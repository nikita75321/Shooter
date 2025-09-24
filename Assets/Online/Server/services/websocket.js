// //websocket.js
const WebSocket = require('ws');
const { v4: uuidv4 } = require('uuid');
const { GameConstants } = require('../config/constants');
const ConnectionController = require('../controllers/connectionController');

const playerInGameController = require('../controllers/playerInGameController');
const damageController = require('../controllers/damageController');
const boostController = require('../controllers/BoostController');
const upgradeController = require('../controllers/UpgradeController')

const { flushPlayerData } = require('./servicesflushService');
const playerController = require('../controllers/playerController');
const ClanController = require('../controllers/clanController');
const currencyController = require('../controllers/currencyController');
const statsController = require('../controllers/statsController');
const systemController = require('../controllers/systemController');
const heroController = require('../controllers/heroController');
const clanSync = require('../services/clanSyncService');
const playerRedisService = require('../services/playerRedisService')

const { roomManager, initializeRoomManager } = require('../controllers/roomManager');
const { formatDateTime, isValidMessage, sendError, handleBinaryMessage } = require('../services/utils');

const connectedPlayers = new Map();

const { Constants } = require('../config/constants');
const wsKey = Constants.wsKey;

// Инициализация RoomManager после подключения Redis
async function InitRoomManager() {
    try {
        const success = await initializeRoomManager();
        if (success) console.log('RoomManager initialized successfully in WebSocket');
    } catch (error) {
        console.error('Failed to initialize RoomManager:', error);
    }
}

function setupWebSocketServer(server) {
    const wss = new WebSocket.Server({ noServer: true });
    
    let isInitialized = false;
    const initPromise = (async () => {
        await clearRedisData();
        isInitialized = true;
        console.log('WebSocket server initialization complete');
    })();
    
    server.on('upgrade', async (request, socket, head) => {
        try {
            if (!isInitialized) await initPromise;

            if (socket.wsHandled) { socket.destroy(); return; }
            socket.wsHandled = true;

            wss.handleUpgrade(request, socket, head, (ws) => wss.emit('connection', ws, request));
        } catch (err) {
            console.error('Upgrade error:', err);
            socket.destroy();
        }
    });

    wss.on('connection', (ws) => {
        ws.id = uuidv4();
        ws.isAlive = true;
        console.log('New WebSocket connection, total clients:', wss.clients.size);

        (async () => {
            try { await global.redisClient.set(`${wsKey}${ws.id}`, 'pending'); }
            catch (error) { console.error('Redis set error:', error); }
        })();

        ws.on('pong', () => { ws.isAlive = true; });

        ws.send(JSON.stringify({ action: 'server_time_response', server_time: formatDateTime(new Date()) }));

        ws.on('error', (error) => { console.error('WebSocket error:', error); sendError(ws, 'WebSocket error'); });

        ws.on('message', async (message, isBinary) => {
            let data;
            try {
                data = isBinary ? handleBinaryMessage(ws, message) : JSON.parse(message);
                if (!data) throw new Error('Invalid message format');
            } catch (err) {
                console.error('Message parsing error:', err);
                return sendError(ws, 'Invalid message format');
            }

            switch (data.action) {
                // =================== System ===================
                case 'ping':
                    ws.send(JSON.stringify({ action: 'pong', timestamp: data.timestamp }));
                    break;
                case 'player_login': await playerController.handlePlayerLogin(ws, data); break;
                case 'demo_request': await systemController.handleDemoRequest(ws, data); break;
                case 'get_time_until_month_end': await systemController.handleGetTimeUntilMonthEnd(ws); break;
                case 'get_game_modes_status': await systemController.handleGetGameModesStatus(ws); break;

                // =================== Player ===================
                case 'register_player': await playerController.handleRegisterPlayer(ws, data); break;
                case 'register_player_response':
                    if (data.player_id) {
                        ws.playerId = data.player_id;
                        await global.redisClient.setEx(`ws:${ws.id}:player`, 36000, data.player_id);
                    }
                    break;
                case 'player_connect':
                    await playerController.handlePlayerConnect(ws, data);
                    // if (data.player_id) {
                    //  ws.playerId = data.player_id;
                    //  roomManager.registerPlayerConnection(ws.playerId, ws); 
                    // }
                    if (data.player_id) {
                        ws.playerId = data.player_id;
                        roomManager.registerPlayerConnection(ws.playerId, ws);
                        await cleanupReconnectData(ws.playerId);
                    }
                    break;
                case 'update_player_rating': await playerController.handleUpdatePlayerRating(ws, data); break;
                case 'check_name': await playerController.handleCheckName(ws, data); break;
                case 'update_name': await playerController.handleUpdatePlayerName(ws, data); break;
                case 'update_rating': await playerController.handleUpdateRating(ws, data); break;
                case 'get_player_info': await playerController.handleGetPlayerInfo(ws, data); break;
                case 'get_player_data': await playerController.handleGetPlayerData(ws, data); break;

                // =================== Clan ===================
                case 'create_clan': await ClanController.handleCreateClan(ws, data); break;
                case 'join_clan': await ClanController.handleJoinClan(ws, data); break;
                case 'leave_clan': await ClanController.handleLeaveClan(ws, data); break;
                case 'get_all_clans': await ClanController.handleGetAllClans(ws); break;
                case 'search_clans': await ClanController.handleSearchClans(ws, data); break;
                case 'get_clan_info': await ClanController.handleGetClanInfo(ws, data); break;
                case 'get_clan_top_with_current': await ClanController.handleGetClanInfoWithCurrent(ws, data); break;

                // =================== Stats ===================
                case 'update_player_stats': await statsController.handleUpdatePlayerStats(ws, data); break;
                case 'get_rating_leaderboard': await statsController.handleGetRatingLeaderboard(ws, data); break;
                case 'get_kills_leaderboard': await statsController.handleGetKillsLeaderboard(ws, data); break;

                // =================== Hero ===================
                case 'update_hero_stats': await heroController.handleUpdateHeroStats(ws, data); break;
                case 'update_favorite_hero': await heroController.handleUpdateFavoriteHero(ws, data); break;
                case 'get_hero_levels': await heroController.handleGetHeroLevels(ws, data); break;
                case 'update_hero_levels': await heroController.handleUpdateHeroLevels(ws, data); break;

                // =================== Currency ===================
                case 'claim_rewards': await currencyController.handleClaimRewards(ws, data); break;
                case 'add_currency': await currencyController.handleAddCurrency(ws, data); break;
                case 'spend_hero_cards': await currencyController.handleSpendHeroCards(ws, data); break;

                // =================== Room ===================
                case 'join_matchmaking': await handleJoinMatchmaking(ws, data); break;
                case 'leave_room': await handleLeaveRoom(ws, data); break;
                case 'force_close_room': await handleForceCloseRoom(ws, data); break;
                case 'get_room_info': await handleGetRoomInfo(ws, data); break;
                case 'get_matchmaking_stats': await handleGetMatchmakingStats(ws, data); break;
                case 'update_player_ready': await handleUpdatePlayerReady(ws, data); break;

                // =================== Match overral ===================
                case 'update_player_stats_after_battle': await statsController.handleUpdatePlayerStats(ws, data); break;
                case 'get_match_stats': await playerInGameController.handleGetMatchStats(ws, data); break;

                // =================== Movement ===================
                case 'update_player_transform': await playerInGameController.handleUpdatePlayerTransform(ws, data); break;

                // =================== Combat ===================
                case 'deal_damage': await damageController.handleDealDamage(ws, data); break;
                case 'player_death': await damageController.handlePlayerDeath(ws, data); break;
                case 'player_respawn': await playerInGameController.handlePlayerRespawn(ws, data); break;

                // =================== Boost ===================
                case 'spawn_room_boosts': await boostController.spawnBoosts(ws, data); break;
                case 'boost_pickup': await boostController.handleBoostPickup(ws, data); break;

                // ===========================================
                case 'heal_player': await boostController.healPlayer(ws, data); break;
                
                // =================== Upgrade ===================
                case 'spawn_room_upgrades': await upgradeController.spawnUpgrades(ws, data); break;
                case 'upgrade_pickup': await upgradeController.handleUpgradePickup(ws, data); break;
                case 'upgrade_drop': await upgradeController.handleUpgradeDrop(ws, data); break;

                default:
                    sendError(ws, `Unknown action: ${data.action}`);
            }
        });

        ws.on('close', async (code, reason) => {
            try {
                const playerId = ws.playerId || (await global.redisClient.get(`ws:${ws.id}:player`));
                if (playerId) {
                    // Проверяем, нужно ли готовить реконнект (не при нормальном закрытии)
                    if (shouldPrepareReconnect(code, reason)) {
                        await prepareReconnect(playerId, ws);
                    } else {
                        // Немедленное отключение - очищаем реконнект данные
                        await cleanupReconnectData(playerId);
                        await handleFullDisconnect(playerId);
                    }   
                    // await handleFullDisconnect(playerId);
                }
                
                // Всегда чистим ключи Redis соединения
                // console.log('Deleting keys:', `${wsKey}${ws.id}`, `ws:${ws.id}:player`);
                await global.redisClient.del(`${wsKey}${ws.id}`);
                await global.redisClient.del(`ws:connection:${ws.id}:player`);

            } catch (error) {
                console.error('Error during disconnect cleanup:', error);
            }
        });
    });

    const interval = setInterval(() => {
        wss.clients.forEach((ws) => {
            if (!ws.isAlive) return ws.terminate();
            ws.isAlive = false;
            ws.ping();
        });
    }, 30000);

    wss.on('close', () => clearInterval(interval));

    scheduleDailyRestart(wss);

    return wss;
}

// ================== Функции реконнекта ==================

/**
 * Определяет, нужно ли готовить реконнект
 */
function shouldPrepareReconnect(code, reason) {
    // Не готовим реконнект при нормальном закрытии или ошибках аутентификации
    const noReconnectCodes = [1000, 1001, 1008, 4000];
    return !noReconnectCodes.includes(code) && 
           !reason?.includes('normal') && 
           !reason?.includes('auth');
}

/**
 * Генерирует токен реконнекта
 */
function generateReconnectToken() {
    return Math.random().toString(36).substring(2) + Date.now().toString(36);
}

/**
 * Сохраняет токен реконнекта для игрока
 */
async function saveReconnectToken(playerId, ws) {
    if (!playerId) return null;
    
    const reconnectToken = generateReconnectToken();
    const reconnectKey = `reconnect:${playerId}`;
    const playerData = await playerRedisService.getPlayerFromRedis(playerId);
    
    try {
        // Сохраняем в Redis на 30 секунд
        await global.redisClient.setEx(
            reconnectKey, 
            30, // 30 секунд
            JSON.stringify({
                token: reconnectToken,
                playerData: playerData,
                timestamp: Date.now(),
                roomId: ws.roomId // сохраняем ID комнаты если есть
            })
        );
        
        // Отправляем токен клиенту
        if (ws.readyState === WebSocket.OPEN) {
            ws.send(JSON.stringify({
                action: 'reconnect_token',
                token: reconnectToken,
                expiresIn: 30
            }));
        }
        
        console.log(`Reconnect token saved for player ${playerId}`);
        return reconnectToken;
    } catch (error) {
        console.error(`Error saving reconnect token for player ${playerId}:`, error);
        return null;
    }
}

/**
 * Подготавливает реконнект при отключении
 */
async function prepareReconnect(playerId, ws) {
    try {
        const playerData = await playerRedisService.getPlayerFromRedis(playerId);
        if (!playerData) {
            await handleFullDisconnect(playerId);
            return;
        }

        // Сохраняем данные для реконнекта
        const reconnectToken = await saveReconnectToken(playerId, ws);
        if (reconnectToken) {
            console.log(`Reconnect prepared for player ${playerId}, token: ${reconnectToken}`);
            
            // Устанавливаем таймаут для полного отключения
            setTimeout(async () => {
                const stillPending = await global.redisClient.get(`reconnect:${playerId}`);
                if (stillPending) {
                    console.log(`Reconnect timeout for player ${playerId}, performing full disconnect`);
                    await cleanupReconnectData(playerId);
                    await handleFullDisconnect(playerId);
                }
            }, RECONNECT_TIMEOUT);
        }
    } catch (error) {
        console.error(`Error preparing reconnect for player ${playerId}:`, error);
        await handleFullDisconnect(playerId);
    }
}

/**
 * Обрабатывает попытку реконнекта
 */
async function handlePlayerReconnect(ws, data) {
    const { playerId, token } = data;
    
    if (!playerId || !token) {
        sendError(ws, 'Missing playerId or token');
        return;
    }

    try {
        const reconnectKey = `reconnect:${playerId}`;
        const reconnectData = await global.redisClient.get(reconnectKey);
        
        if (!reconnectData) {
            sendError(ws, 'Reconnect token expired or invalid');
            ws.close(1008, 'Reconnect failed');
            return;
        }

        const reconnectInfo = JSON.parse(reconnectData);
        
        if (reconnectInfo.token !== token) {
            sendError(ws, 'Invalid reconnect token');
            ws.close(1008, 'Reconnect failed');
            return;
        }

        // Успешный реконнект
        await global.redisClient.del(reconnectKey);
        
        // Восстанавливаем данные игрока
        ws.playerId = playerId;
        ws.playerData = reconnectInfo.playerData;
        
        // Регистрируем соединение
        await global.redisClient.setEx(`ws:${ws.id}:player`, 36000, playerId);
        roomManager.registerPlayerConnection(playerId, ws);
        
        // Восстанавливаем игрока в комнату если нужно
        if (reconnectInfo.roomId) {
            await restorePlayerToRoom(playerId, reconnectInfo.roomId, ws);
        }

        // Отправляем подтверждение
        ws.send(JSON.stringify({
            action: 'reconnect_success',
            playerData: reconnectInfo.playerData,
            roomId: reconnectInfo.roomId
        }));

        console.log(`Player ${playerId} reconnected successfully`);

    } catch (error) {
        console.error(`Error handling reconnect for player ${playerId}:`, error);
        sendError(ws, 'Reconnect failed');
        ws.close(1008, 'Reconnect error');
    }
}

/**
 * Восстанавливает игрока в комнату
 */
async function restorePlayerToRoom(playerId, roomId, ws) {
    try {
        // Здесь должна быть логика восстановления игрока в конкретной комнате
        // Это зависит от вашей реализации roomManager
        const room = roomManager.getRoom(roomId);
        if (room && room.hasPlayer(playerId)) {
            room.reconnectPlayer(playerId, ws);
            ws.roomId = roomId;
            console.log(`Player ${playerId} restored to room ${roomId}`);
        }
    } catch (error) {
        console.error(`Error restoring player ${playerId} to room ${roomId}:`, error);
    }
}

/**
 * Очищает данные реконнекта
 */
async function cleanupReconnectData(playerId) {
    try {
        await global.redisClient.del(`reconnect:${playerId}`);
    } catch (error) {
        console.error(`Error cleaning reconnect data for player ${playerId}:`, error);
    }
}

/**
 * Обрабатывает полное отключение игрока
 */
async function handleFullDisconnect(playerId) {
    try {
        // 1. Удаляем игрока из комнат
        await ConnectionController.handlePlayerDisconnect(playerId);
        await roomManager.handlePlayerDisconnect(playerId);

        // 2. Сохраняем данные игрока в БД
        const playerData = await playerRedisService.getPlayerFromRedis(playerId);
        if (playerData) {
            await flushPlayerData(playerId);
        }
        
        // 3. Обновляем данные о клане в БД
        if (playerData?.clan_id) {
            await clanSync.syncClanFromRedisToDB(playerData.clan_id);
        }
        
        console.log(`Player ${playerId} fully disconnected, cleanup done`);
    } catch (error) {
        console.error(`Error during full disconnect for player ${playerId}:`, error);
    }
}

// ================= Helper Functions =================
async function clearRedisData() {
    const patterns = ['connection:*'];
    for (const pattern of patterns) {
        const keys = await global.redisClient.keys(pattern);
        if (keys.length) await global.redisClient.del(keys);
    }
}

// ================= Room/Matchmaking Functions =================
async function handleGetRoomInfo(ws, data) {
    if (!ws.playerId) {
        return sendError(ws, 'Player not connected');
    }
 
    const room = roomManager.getRoomByPlayerId(ws.playerId);
    if (room) {
        ws.send(JSON.stringify({
            action: 'room_info',
            room: roomManager.getRoomInfo(room.id)
        }));
    } else {
        sendError(ws, 'Player not in any room');
    }
}
async function handleGetMatchmakingStats(ws, data) {
    const stats = roomManager.getAllModesStatus();
    ws.send(JSON.stringify({
        action: 'matchmaking_stats',
        stats: stats
    }));
}
async function handleLeaveRoom(ws, data) {
    if (!ws.playerId) {
        return sendError(ws, 'Player not connected');
    }
 
    try {
        const room = await roomManager.handlePlayerDisconnect(ws.playerId);
     
        if (room) {
            ws.send(JSON.stringify({
                action: 'room_left',
                room_id: room.id,
                player_id: ws.playerId
            }));
         
        console.log(`Player ${ws.playerId} left room ${room.id}`);
        } else {
            sendError(ws, 'Player not in any room');
        }
    } catch (error) {
        console.error('Error leaving room:', error);
        sendError(ws, 'Failed to leave room');
    }
}
async function handleForceCloseRoom(ws, data) {
    if (!ws.playerId) {
        return sendError(ws, 'Player not connected');
    }
 
    // Optional: Add admin permission check here
    // if (!isAdmin(ws.playerId)) {
    //     return Utils.sendError(ws, 'Insufficient permissions');
    // }
 
    if (!data.room_id) {
        return sendError(ws, 'Room ID required');
    }
 
    try {
        const success = roomManager.forceResetRoom(data.room_id);
     
        if (success) {
            ws.send(JSON.stringify({
                action: 'room_force_closed',
                room_id: data.room_id,
                success: true
            }));
        } else {
            sendError(ws, 'Room not found or already closed');
        }
    } catch (error) {
        console.error('Error force closing room:', error);
        sendError(ws, 'Failed to force close room');
    }
}
async function handleJoinMatchmaking(ws, data) { 
    console.log(`Join matchmaking request:`, data);

    const requiredFields = ['player_id', 'mode', 'hero_id', 'hero_skin'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing player_id, mode, hero_id or hero_skin');
    }

    if (ws.readyState !== WebSocket.OPEN) {
        console.log(`WebSocket is not open for player ${data.player_id}, state: ${ws.readyState}`);
        return;
    }

    try {
        const { player_id, mode, hero_id, hero_skin } = data;

        // Находим доступную комнату
        const room = await roomManager.findAvailableRoom(mode, player_id, hero_id);
        if (!room) {
            ws.send(JSON.stringify({
                action: 'matchmaking_full',
                message: 'All rooms are currently full'
            }));
            return;
        }

        // Берём профиль игрока из Redis
        const playerProfile = await playerRedisService.getPlayerFromRedis(player_id);

        let heroLevel = 1;
        let heroRank = 1;
        if (playerProfile?.hero_levels && Array.isArray(playerProfile.hero_levels)) {
            const heroData = playerProfile.hero_levels[hero_id];
            if (heroData) {
                heroLevel = heroData.level || 1;
                heroRank = heroData.rank || 1;
            }
        }

        // Сохраняем временные данные героя для этого матча
        const heroKey = `player:hero:${room.id}:${player_id}`;
        await global.redisClient.hSet(heroKey, {
            hero_id,
            skin_id: hero_skin,
            level: heroLevel,
            rank: heroRank
        });
        await global.redisClient.expire(heroKey, 60 * 5); // TTL 5 минут

        // Добавляем игрока в комнату с передачей всех параметров героя
        const updatedRoom = await roomManager.addPlayerToRoom(
            room,
            player_id,
            hero_id,
            hero_skin,
            heroLevel,
            heroRank
        );

        if (ws.readyState !== WebSocket.OPEN) {
            console.log(`WebSocket closed during matchmaking for ${player_id}`);
            await roomManager.removePlayerFromRoom(player_id);
            return;
        }

        ws.currentRoom = updatedRoom.id;
        ws.playerId = player_id;

        // Ответ игроку
        ws.send(JSON.stringify({
            action: 'join_matchmaking_response',
            room_id: updatedRoom.id,
            hero_id,
            hero_skin,
            hero_level: heroLevel,
            hero_rank: heroRank,
            players_in_room: updatedRoom.playerCount,
            max_players: updatedRoom.maxPlayers,
            estimated_wait: updatedRoom.state === GameConstants.ROOM_STATES.COUNTDOWN
                ? (30000 - (Date.now() - updatedRoom.startTime))
                : 30000
        }));

    } catch (error) {
        console.error('Matchmaking error:', error);
        sendError(ws, 'Failed to join matchmaking');
    }
}

async function handleUpdatePlayerReady(ws, data) {
    if (!isValidMessage(data, ['player_id', 'is_ready', 'room_id'])) {
        return sendError(ws, 'Missing required fields');
    }
    try {
        // Теперь метод асинхронный!
        const room = await roomManager.getRoomByPlayerId(data.player_id);
        if (room && room.id === data.room_id) {
            // Здесь можно обновить статус готовности игрока
            // и отправить обновление всем в комнате
         
            // Теперь метод асинхронный!
            await roomManager.notifyRoomPlayers(room, {
                action: 'player_ready_update',
                player_id: data.player_id,
                hero_id: data.hero_id,
                is_ready: Boolean(data.is_ready)
            });
            ws.send(JSON.stringify({
                action: 'player_ready_response',
                success: true,
                is_ready: data.is_ready
            }));
        } else {
            sendError(ws, 'Player not in specified room');
        }
    } catch (error) {
        console.error('Player ready update error:', error);
        sendError(ws, 'Failed to update ready status');
    }
}

// ================= Daily Restart =================
function scheduleDailyRestart(wss) {
    const now = new Date();
    const restartTime = new Date(
        now.getFullYear(),
        now.getMonth(),
        now.getDate(),
        0, 0, 0, 0
    );
    // Если время уже прошло сегодня, планируем на завтра
    if (now > restartTime) {
        restartTime.setDate(restartTime.getDate() + 1);
    }
    const timeUntilRestart = restartTime.getTime() - now.getTime();
    // Таймер для основного перезапуска
    setTimeout(() => {
        performRestartSequence(wss);
    }, timeUntilRestart);
    // Таймеры для предупреждений
    scheduleWarning(wss, timeUntilRestart - 60 * 60 * 1000, "in 1 hour");
    scheduleWarning(wss, timeUntilRestart - 30 * 60 * 1000, "in 30 minutes");
    scheduleWarning(wss, timeUntilRestart - 15 * 60 * 1000, "in 15 minutes");
    scheduleWarning(wss, timeUntilRestart - 5 * 60 * 1000, "in 5 minutes");
}

// Функция для планирования предупреждений
function scheduleWarning(wss, timeUntilWarning, message) {
    if (timeUntilWarning <= 0) return;

    setTimeout(() => {
        sendRestartNotification(wss, message);
    }, timeUntilWarning);
}

module.exports = { setupWebSocketServer, InitRoomManager, roomManager,
                setupWebSocketServer,
                InitRoomManager,
                prepareReconnect,
                handlePlayerReconnect,
                cleanupReconnectData
 };