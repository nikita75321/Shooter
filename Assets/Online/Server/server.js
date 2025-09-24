// //server.js
const https = require('https');
const fs = require('fs');
const path = require('path');
const express = require('express');
const WebSocket = require('ws');

const { createClient } = require('redis');
const Utils = require('./services/utils');
const clanRedisService = require('./services/clanRedisService');
const ClanSyncService = require('./services/clanSyncService');
const statsController = require('./controllers/statsController');
const playerRedisService = require('./services/playerRedisService')
const { initializeDatabase, db } = require('./config/db');

const { setupWebSocketServer, InitRoomManager } = require('./services/websocket');
const heapdump = require('heapdump');

const sslOptions = {
    key: fs.readFileSync('/etc/letsencrypt/live/game.webempire.store/privkey.pem'),
    cert: fs.readFileSync('/etc/letsencrypt/live/game.webempire.store/fullchain.pem')
};

const app = express();
const port = process.env.PORT || 3000;
const server = https.createServer(sslOptions, app);

global.connectedPlayers = new Map();
global.pendingReconnections = new Map(); // Новый Map для ожидающих реконнекта

// Константы для реконнекта
const RECONNECT_TIMEOUT = 10000; // 10 секунд
const RECONNECT_TOKEN_EXPIRY = 30; // 30 секунд для токена в Redis

(async () => {
    try {
        // ================== Redis ==================
        const redisClient = createClient({
            url: 'redis://localhost:6379',
            socket: { reconnectStrategy: retries => Math.min(retries * 100, 5000) }
        });

        redisClient.on('error', (err) => console.error('Redis Client Error', err));
        await redisClient.connect();
        console.log('Redis connected successfully');

        global.redisClient = redisClient;
        Utils.init(redisClient);
        clanRedisService.init(redisClient);
        playerRedisService.init(redisClient);

        // ================== RoomManager ==================
        await InitRoomManager();
        console.log('RoomManager initialized successfully in server.js');

        // ================== Pub/Sub ==================
        await initializeRedisPubSub();
        console.log('Redis Pub/Sub initialized');

        // ================== Database ==================
        await initializeDatabase();
        console.log('Database initialized successfully');

        try {
            const pg = db;
            const res = await statsController.ensureLeaderboardsFromDB(pg, { batchSize: 2000 });

            if (res.skipped) {
                console.log(`[LB warmup] skipped (already have data). ZCARD rating=${res.rc}, kills=${res.kc}`);
            } else {
                console.log(`[LB warmup] rebuilt from DB. players=${res.total}, ZCARD rating=${res.rc}, kills=${res.kc}`);
            }
        } catch (e) {
            console.error('Leaderboards warmup failed:', e);
        }

        // ================== Clan sync ==================
        console.log('Starting clan synchronization from DB to Redis...');
        await ClanSyncService.clearAllClansFromRedis();
        await ClanSyncService.syncClansFromDB();
        console.log('Clan synchronization completed');

        // ================== WebSocket ==================
        const wss = setupWebSocketServer(server);
        global.wss = wss;
        
        // ================== Periodic clan sync ==================
        const clanSyncInterval = setInterval(async () => {
            try {
                console.log('Periodic clan sync start...');
                await ClanSyncService.syncAllClansToDatabase();
                console.log('Periodic clan sync completed');
            } catch (err) {
                console.error('Periodic clan sync failed:', err);
            }
        }, 60 * 60 * 60 * 24);

        // ================== Очистка старых реконнектов ==================
        setInterval(() => {
            cleanupExpiredReconnections();
        }, 60 * 60 * 2); // Каждые 2 минуты

        // ================== Start server ==================
        server.listen(port, () => console.log(`Server running on port ${port}`));

    } catch (err) {
        console.error('Server initialization failed:', err);
        process.exit(1);
    }
})();

// ================== Функции для реконнекта ==================

/**
 * Генерирует токен для реконнекта
 */
function generateReconnectToken() {
    return Math.random().toString(36).substring(2) + Date.now().toString(36);
}

/**
 * Сохраняет данные игрока для возможного реконнекта
 */
async function preparePlayerReconnect(playerId, playerData) {
    const reconnectToken = generateReconnectToken();
    const reconnectKey = `reconnect:${playerId}`;
    
    try {
        // Сохраняем данные игрока в Redis на 30 секунд
        await global.redisClient.setex(
            reconnectKey, 
            RECONNECT_TOKEN_EXPIRY, 
            JSON.stringify({
                token: reconnectToken,
                playerData: playerData,
                timestamp: Date.now()
            })
        );
        
        // Сохраняем в памяти для быстрого доступа
        global.pendingReconnections.set(playerId, {
            token: reconnectToken,
            playerData: playerData,
            timestamp: Date.now(),
            timeout: setTimeout(() => {
                cleanupPlayerReconnect(playerId);
            }, RECONNECT_TIMEOUT)
        });
        
        console.log(`Reconnect prepared for player ${playerId}, token: ${reconnectToken}`);
        return reconnectToken;
    } catch (error) {
        console.error(`Error preparing reconnect for player ${playerId}:`, error);
        return null;
    }
}

/**
 * Очищает данные реконнекта для игрока
 */
function cleanupPlayerReconnect(playerId) {
    const pendingReconnect = global.pendingReconnections.get(playerId);
    if (pendingReconnect) {
        clearTimeout(pendingReconnect.timeout);
        global.pendingReconnections.delete(playerId);
        
        // Асинхронно очищаем Redis
        const reconnectKey = `reconnect:${playerId}`;
        global.redisClient.del(reconnectKey).catch(console.error);
        
        console.log(`Reconnect data cleaned up for player ${playerId}`);
        
        // Здесь можно добавить логику полного сохранения данных игрока
        // если реконнект не произошел в течение таймаута
        const { flushPlayerData } = require('./services/servicesflushService');
        flushPlayerData(playerId).catch(err => 
            console.error(`Failed to save player ${playerId} after reconnect timeout:`, err)
        );
    }
}

/**
 * Очищает просроченные реконнекты
 */
function cleanupExpiredReconnections() {
    const now = Date.now();
    for (const [playerId, reconnect] of global.pendingReconnections.entries()) {
        if (now - reconnect.timestamp > RECONNECT_TIMEOUT) {
            cleanupPlayerReconnect(playerId);
        }
    }
}

// ================== Работа с кластерами ==================
async function initializeRedisPubSub() {
    const subscriber = global.redisClient.duplicate();
    await subscriber.connect();

    await subscriber.subscribe('websocket_messages', async (message) => {
        try {
            const { playerId, data, roomId, heroId } = JSON.parse(message);

            global.wss.clients.forEach(client => {
                if (client.playerId === playerId && client.readyState === WebSocket.OPEN) {
                    const fullMessage = {
                        ...data,
                        meta: { roomId, ...(heroId !== undefined ? { heroId } : {}) }
                    };
                    client.send(JSON.stringify(fullMessage));
                    const logHero = heroId !== undefined ? `, heroId: ${heroId}` : '';
                    console.log(`Delivered ${data.action} to player ${playerId}${logHero}`);
                }
            });
        } catch (err) {
            console.error('Error processing Redis Pub/Sub message:', err);
        }
    });

    console.log('Redis Pub/Sub for WebSocket initialized');
}

// ================== Express ==================
app.get('/', (req, res) => res.sendFile(path.join(__dirname, 'public', 'index.html')));

// ================== Heapdump ==================
setInterval(() => {
    const heapUsed = process.memoryUsage().heapUsed;
    if (heapUsed > 100 * 1024 * 1024) {
        const filename = `/tmp/heapdump-${Date.now()}.heapsnapshot`;
        heapdump.writeSnapshot(filename, (err) => {
            if (err) console.error('Failed to write heapdump:', err);
            else console.log(`Heapdump written to ${filename}`);
        });
    }
}, 60000);

// ================== Graceful shutdown ==================
const gracefulShutdown = async () => {
    console.log('Shutting down server...');
    const { flushPlayerData } = require('./services/servicesflushService');

    // Очищаем все ожидающие реконнекты
    for (const playerId of global.pendingReconnections.keys()) {
        cleanupPlayerReconnect(playerId);
    }

    // 1. Сохраняем всех игроков
    for (const client of global.wss.clients) {
        if (client.playerId) {
            try { await flushPlayerData(client.playerId); }
            catch (err) { console.error(`Failed to save player ${client.playerId}:`, err); }
        }
    }

    // 2. Синхронизация всех кланов из Redis в БД
    try {
        if (ClanSyncService) {
            console.log('Syncing all clans to database...');
            await ClanSyncService.syncAllClansToDatabase();
            console.log('All clans synced successfully');
        }
    } catch (err) {
        console.error('Failed to sync clans during shutdown:', err);
    }

    // 3. Закрываем все соединения WebSocket
    global.wss.clients.forEach(client => {
        if (client.readyState === WebSocket.OPEN) client.close(1001, 'Server restarting');
    });

    // 4. Закрываем сервер после небольшой задержки
    setTimeout(() => {
        global.wss.close(() => server.close(() => process.exit(0)));
    }, 5000);
};

process.on('SIGINT', gracefulShutdown);
process.on('SIGTERM', gracefulShutdown);

process.on('uncaughtException', err => {
    console.error('Uncaught Exception:', err);
    process.exit(1);
});

process.on('unhandledRejection', (reason, promise) => {
    console.error('Unhandled Rejection at:', promise, 'reason:', reason);
    process.exit(1);
});

// Экспортируем функции для использования в websocket.js
module.exports = {
    preparePlayerReconnect,
    cleanupPlayerReconnect
};