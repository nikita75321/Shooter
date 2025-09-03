let redisClient;

const playerRedisService = require('../services/playerRedisService')

function init(redis) {
    if (!redis) throw new Error("Redis client is required");
    redisClient = redis;
}

// ==================== Игроки ====================
// async function getPlayerFromRedis(playerId) {
//     try {
//         const key = `player:${playerId}:profile`;
//         const rawProfile = await redisClient.get(key);
//         if (!rawProfile) return null;

//         let data = {};
//         try {
//             data = JSON.parse(rawProfile);
//         } catch (err) {
//             console.error('Failed to parse player JSON from Redis:', err);
//             return null;
//         }

//         // Преобразуем числа
//         data.rating = Number(data.rating || 0);
//         data.best_rating = Number(data.best_rating || 0);
//         data.money = Number(data.money || 0);
//         data.donat_money = Number(data.donat_money || 0);
//         data.clan_points = Number(data.clan_points || 0);
//         data.overral_kill = Number(data.overral_kill || 0);
//         data.match_count = Number(data.match_count || 0);
//         data.win_count = Number(data.win_count || 0);
//         data.revive_count = Number(data.revive_count || 0);
//         data.max_damage = Number(data.max_damage || 0);
//         data.shoot_count = Number(data.shoot_count || 0);

//         // Парсим вложенные объекты
//         if (typeof data.open_characters === 'string') {
//             try { data.open_characters = JSON.parse(data.open_characters); } catch { data.open_characters = {}; }
//         }
//         if (typeof data.hero_levels === 'string') {
//             try { data.hero_levels = JSON.parse(data.hero_levels); } catch { data.hero_levels = Array(8).fill({ rank: 1, level: 1 }); }
//         }
//         if (typeof data.hero_card === 'string') {
//             try { data.hero_card = JSON.parse(data.hero_card); } catch { data.hero_card = {}; }
//         }
//         if (typeof data.hero_match === 'string') {
//             try { data.hero_match = JSON.parse(data.hero_match); } catch { data.hero_match = Array(8).fill(0); }
//         }

//         return data;
//     } catch (err) {
//         console.error(`Error getting player ${playerId} from Redis:`, err);
//         return null;
//     }
// }

// async function savePlayerProfileToRedis(playerId, playerData) {
//     try {
//         // Сохраняем весь объект как JSON
//         await redisClient.set(
//             `player:${playerId}:profile`,
//             JSON.stringify({
//                 ...playerData,
//                 last_updated: new Date().toISOString()
//             }),
//             'EX', 86400 // 24 часа
//         );
//         return true;
//     } catch (err) {
//         console.error(`Error saving player ${playerId} profile:`, err);
//         return false;
//     }
// }

// Обновление профиля игрока в Redis
async function updatePlayerInRedis(playerId, updates) {
    try {
        const existing = await playerRedisService.getPlayerFromRedis(playerId);
        if (!existing) return false;

        const oldClanPoints = Number(existing.clan_points || 0);
        const newClanPoints = updates.clan_points !== undefined ? Number(updates.clan_points) : oldClanPoints;
        const delta = newClanPoints - oldClanPoints;

        // Объединяем обновления
        const updated = { ...existing, ...updates, clan_points: newClanPoints };
        await playerRedisService.savePlayerProfileToRedis(playerId, updated);

        return true;
    } catch (err) {
        console.error(`Error updating player ${playerId}:`, err);
        return false;
    }
}

async function setPlayerClanInRedis(playerId, clanId) {
    try {
        const player = await playerRedisService.getPlayerFromRedis(playerId);
        if (!player) return false;
        player.clan_id = clanId;
        await playerRedisService.savePlayerProfileToRedis(playerId, player);
        return true;
    } catch (err) {
        console.error('Error setting player clan:', err);
        return false;
    }
}

// ==================== WebSocket ====================
const WebSocket = require('ws');

function sendError(ws, message) {
    if (ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify({ action: 'error', message: message || 'Internal server error' }));
    }
}

function handleBinaryMessage(ws, binaryData) {
    try {
        const buffer = Buffer.from(binaryData);
        let offset = 0;
        const actionLength = buffer.readUInt8(offset);
        offset += 1;

        const action = buffer.toString('utf8', offset, offset + actionLength);
        offset += actionLength;

        const jsonData = buffer.toString('utf8', offset);
        const data = JSON.parse(jsonData);
        data.action = action;
        return data;
    } catch (err) {
        console.error('Error processing binary message:', err);
        sendError(ws, 'Failed to process binary message');
        return null;
    }
}

function formatDateTime(date) {
    const d = new Date(date);
    const pad = (n) => String(n).padStart(2,'0');
    return `${pad(d.getDate())}.${pad(d.getMonth()+1)}.${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

function isValidMessage(data, requiredFields = []) {
    if (!data || typeof data !== 'object') return false;
    return requiredFields.every(f => data[f] !== undefined && data[f] !== null);
}

module.exports = {
    init,
    // getPlayerFromRedis,
    // savePlayerProfileToRedis,
    updatePlayerInRedis,
    setPlayerClanInRedis,
    sendError,
    handleBinaryMessage,
    formatDateTime,
    isValidMessage
};