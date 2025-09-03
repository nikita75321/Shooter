// services/playerRedisService.js
let redisClient;

function init(redis) {
    if (!redis) throw new Error("Redis client is required");
    redisClient = redis;
}

// Преобразование чисел при чтении из Redis
// function parsePlayerData(raw) {
//     const data = {};
//     for (const [k, v] of Object.entries(raw)) {
//         // Попробуем преобразовать в число, если возможно
//         const n = Number(v);
//         data[k] = isNaN(n) ? v : n;
//     }
//     return data;
// }
function parsePlayerData(raw) {
    const data = {};
    for (const [k, v] of Object.entries(raw)) {
        let parsed = v;

        // Попробуем распарсить JSON
        if (typeof v === 'string') {
            try {
                parsed = JSON.parse(v);
            } catch {
                // не JSON, оставляем как строку или число
                const n = Number(v);
                parsed = isNaN(n) ? v : n;
            }
        }

        data[k] = parsed;
    }
    return data;
}

// Получить профиль игрока
async function getPlayerFromRedis(playerId) {
    try {
        const key = `player:${playerId}:profile`;
        const raw = await redisClient.hGetAll(key);
        if (!raw || Object.keys(raw).length === 0) return null;

        const data = parsePlayerData(raw);

        // Преобразуем сложные объекты из JSON, если они хранятся как строки
        ['open_characters', 'hero_levels', 'hero_card', 'hero_match'].forEach(field => {
            if (data[field] && typeof data[field] === 'string') {
                try { data[field] = JSON.parse(data[field]); } catch { data[field] = {}; }
            }
        });

        return data;
    } catch (err) {
        console.error(`Error getting player ${playerId} from Redis:`, err);
        return null;
    }
}

// Сохранить профиль игрока (HSET)
// async function savePlayerProfileToRedis(playerId, playerData) {
//     try {
//         const key = `player:${playerId}:profile`;

//         // сериализуем сложные поля
//         const dataToStore = { ...playerData };
//         ['open_characters', 'hero_levels', 'hero_card', 'hero_match'].forEach(field => {
//             if (dataToStore[field] && typeof dataToStore[field] !== 'string') {
//                 dataToStore[field] = JSON.stringify(dataToStore[field]);
//             }
//         });
//         dataToStore.last_updated = new Date().toISOString();

//         // сохраняем как хэш
//         await redisClient.hSet(key, dataToStore);
//         await redisClient.expire(key, 86400);

//         return true;
//     } catch (err) {
//         console.error(`Error saving player ${playerId} profile:`, err);
//         return false;
//     }
// }
async function savePlayerProfileToRedis(playerId, playerData) {
    try {
        const key = `player:${playerId}:profile`;

        const dataToStore = { ...playerData, last_updated: new Date().toISOString() };

        // Сериализуем ВСЕ объекты и массивы
        for (const [k, v] of Object.entries(dataToStore)) {
            if (typeof v === 'object' && v !== null) {
                dataToStore[k] = JSON.stringify(v);
            } else {
                dataToStore[k] = String(v);
            }
        }

        await redisClient.hSet(key, dataToStore);
        await redisClient.expire(key, 86400);

        return true;
    } catch (err) {
        console.error(`Error saving player ${playerId} profile:`, err);
        return false;
    }
}

// Обновить часть профиля игрока
// async function updatePlayerInRedis(playerId, updates) {
//     try {
//         const existing = await getPlayerFromRedis(playerId);
//         if (!existing) return false;

//         const oldClanPoints = Number(existing.clan_points || 0);
//         const newClanPoints = updates.clan_points !== undefined ? Number(updates.clan_points) : oldClanPoints;
//         const delta = newClanPoints - oldClanPoints;

//         // Объединяем обновления
//         const updated = { ...existing, ...updates, clan_points: newClanPoints };

//         await savePlayerProfileToRedis(playerId, updated);

//         // Обновление клана, если есть изменение очков
//         if (existing.clan_id && delta !== 0) {
//             const clanKey = `clan:${existing.clan_id}:info`;
//             await redisClient.hIncrBy(clanKey, 'clan_points', delta);
//         }

//         return true;
//     } catch (err) {
//         console.error(`Error updating player ${playerId}:`, err);
//         return false;
//     }
// }
async function updatePlayerInRedis(playerId, updates) {
    try {
        const existing = await getPlayerFromRedis(playerId);
        if (!existing) return false;

        // Обновляем профиль игрока
        const updated = { ...existing, ...updates };

        // Сохраняем профиль игрока
        await savePlayerProfileToRedis(playerId, updated);

        // Если есть клан — пересчитывать очки при запросе, а не обновлять хэш клана
        // То есть здесь больше ничего не делаем с clan:<id>:info

        return true;
    } catch (err) {
        console.error(`Error updating player ${playerId}:`, err);
        return false;
    }
}

// Установить клан игрока
async function setPlayerClanInRedis(playerId, clanId) {
    try {
        const key = `player:${playerId}:profile`;
        const exists = await redisClient.exists(key);
        if (!exists) return false;

        if (clanId) {
            await redisClient.hSet(key, 'clan_id', clanId);
        } else {
            await redisClient.hDel(key, 'clan_id');
        }
        return true;
    } catch (err) {
        console.error(`Error setting player clan for ${playerId}:`, err);
        return false;
    }
}

module.exports = {
    init,
    getPlayerFromRedis,
    savePlayerProfileToRedis,
    updatePlayerInRedis,
    setPlayerClanInRedis
};