// services/clanRedisService.js
const playerRedisService = require('./playerRedisService')

let redisClient;

function init(redis) {
    if (!redis) throw new Error("Redis client is required");
    redisClient = redis;
}

// ==================== Вспомогательные ====================
function serializeClanData(clanData) {
    return {
        clan_id: String(clanData.clan_id),
        clan_name: String(clanData.clan_name),
        leader_id: String(clanData.leader_id),
        leader_name: String(clanData.leader_name),
        need_rating: String(clanData.need_rating),
        is_open: String(clanData.is_open),
        clan_points: String(clanData.clan_points),
        clan_level: String(clanData.clan_level),
        max_players: String(clanData.max_players),
        player_count: String(clanData.player_count)
    };
}

function parseClanData(data) {
    return {
        clan_id: Number(data.clan_id) || 0,
        clan_name: data.clan_name || '',
        leader_id: data.leader_id || '',
        leader_name: data.leader_name || '',
        need_rating: Number(data.need_rating) || 0,
        is_open: data.is_open === 'true',
        clan_points: Number(data.clan_points) || 0,
        clan_level: Number(data.clan_level) || 1,
        max_players: Number(data.max_players) || 25,
        player_count: Number(data.player_count) || 0,
        place: Number(data.place) || null // добавляем поле place
    };
}

// ==================== Кланы ====================
async function saveClanToRedis(clanId, clanData) {
    try {
        if (!clanId || !clanData) return false;
        await redisClient.hSet(`clan:${clanId}:info`, serializeClanData(clanData));
        
        // Обновляем рейтинг (по clan_points)
        if (clanData.clan_points !== undefined) {
            await redisClient.zAdd("clans:ranking", {
                score: Number(clanData.clan_points ?? -1),
                value: clanId.toString()
            });
        }

        return true;
    } catch (err) {
        console.error(`Error saving clan ${clanId} to Redis:`, err);
        return false;
    }
}

async function getClanFromRedis(clanId) {
    if (!clanId) return null;

    // Приводим к строке, если это число
    const clanIdStr = String(clanId);

    const data = await redisClient.hGetAll(`clan:${clanIdStr}:info`);
    if (!data || Object.keys(data).length === 0) return null;

    const clan = parseClanData(data);

    // Пересчёт очков по участникам
    const members = await getClanMembersFromRedis(clanIdStr);
    const newPoints = members.reduce((sum, m) => sum + (m.clan_points || 0), 0);
    clan.clan_points = newPoints;

    // ⬅️ Пересчёт уровня по очкам
    clan.clan_level = getClanLevelByPoints(newPoints);

    // Синхронизируем рейтинг по актуальным очкам
    await redisClient.zAdd("clans:ranking", {
        score: Number(newPoints),
        value: clanIdStr
    });

    // Место в рейтинге
    const rank = await redisClient.zRevRank("clans:ranking", clanIdStr);
    clan.place = rank !== null ? rank + 1 : null;

    return clan;
}

async function updateClanInRedis(clanId, updates) {
    try {
        // Получаем текущие данные клана
        const existing = await getClanFromRedis(clanId) || {};
        
        // Объединяем с обновлениями
        const updated = { ...existing, ...updates };

        // Сохраняем обновлённый клан в хэш
        await saveClanToRedis(clanId, updated);

        // Обновляем место в рейтинге
        const rank = await redisClient.zRevRank('clans:ranking', String(clanId));
        updated.place = rank !== null ? rank + 1 : null;

        return updated;
    } catch (err) {
        console.error(`Error updating clan ${clanId} in Redis:`, err);
        return false;
    }
}

async function updateClanMemberInRedis(clanId, playerId, updates = {}) {
    try {
        if (!clanId || !playerId || !updates || Object.keys(updates).length === 0) return false;

        const memberKey = `clan:${clanId}:members`;
        const rawMember = await redisClient.hGet(memberKey, playerId.toString());
        if (!rawMember) return false;

        let parsedMember;
        try {
            parsedMember = JSON.parse(rawMember);
        } catch (parseErr) {
            console.error(`Error parsing member ${playerId} data from clan ${clanId}:`, parseErr);
            return false;
        }

        const updatedMember = { ...parsedMember, ...updates };
        await redisClient.hSet(memberKey, playerId.toString(), JSON.stringify(updatedMember));
        return true;
    } catch (err) {
        console.error(`Error updating member ${playerId} in clan ${clanId}:`, err);
        return false;
    }
}

async function removeClanFromRedis(clanId) {
    try {
        const members = await getClanMembersFromRedis(clanId);
        for (const member of members) {
            await setPlayerClanInRedis(member.player_id, null);
        }
        await redisClient.del(`clan:${clanId}:info`);
        await redisClient.del(`clan:${clanId}:members`);
        return true;
    } catch (err) {
        console.error(`Error removing clan ${clanId} from Redis:`, err);
        return false;
    }
}

async function saveClanMembersToRedis(clanId, members = []) {
    try {
        if (!clanId || !Array.isArray(members)) return false;
        await redisClient.del(`clan:${clanId}:members`);
        for (const member of members) {
            await redisClient.hSet(
                `clan:${clanId}:members`,
                member.player_id.toString(),
                JSON.stringify(member)
            );
        }
        return true;
    } catch (err) {
        console.error(`Error saving clan members for clan ${clanId}:`, err);
        return false;
    }
}

async function getClanMembersFromRedis(clanId) {
    const membersKey = `clan:${clanId}:members`;
    const raw = await redisClient.hGetAll(membersKey);

    const detailed = [];
    for (const [playerId, json] of Object.entries(raw)) {
        const member = JSON.parse(json);
        const profile = await playerRedisService.getPlayerFromRedis(playerId);

        detailed.push({
            player_id: playerId,
            player_name: member.player_name,
            is_leader: member.is_leader,
            rating: profile?.rating ? Number(profile.rating) : 0,
            clan_points: profile?.clan_points ? Number(profile.clan_points) : 0,
        });
    }

    return detailed;
}

async function addClanMemberToRedis(clanId, memberData) {
    try {
        if (!clanId || !memberData || !memberData.player_id) return false;
        await redisClient.hSet(
            `clan:${clanId}:members`,
            memberData.player_id.toString(),
            JSON.stringify(memberData)
        );
        return true;
    } catch (err) {
        console.error(`Error adding member ${memberData.player_id} to clan ${clanId}:`, err);
        return false;
    }
}

async function removeClanMemberFromRedis(clanId, playerId) {
    try {
        if (!clanId || !playerId) return false;
        await redisClient.hDel(`clan:${clanId}:members`, playerId.toString());
        return true;
    } catch (err) {
        console.error(`Error removing member ${playerId} from clan ${clanId}:`, err);
        return false;
    }
}

async function updateClanMemberLeaderFlag(clanId, playerId, isLeader = true) {
    try {
        const members = await getClanMembersFromRedis(clanId);
        const updatedMembers = members.map(member => ({
            ...member,
            is_leader: member.player_id === playerId ? isLeader : false
        }));
        await saveClanMembersToRedis(clanId, updatedMembers);
        return true;
    } catch (err) {
        console.error(`Error updating leader flag in clan ${clanId}:`, err);
        return false;
    }
}

// ==================== Игроки ====================
async function setPlayerClanInRedis(playerId, clanId) {
    try {
        if (!playerId) return false;
        if (clanId) {
            await redisClient.set(`player:${playerId}:clan`, clanId.toString());
        } else {
            await redisClient.del(`player:${playerId}:clan`);
        }
        return true;
    } catch (err) {
        console.error(`Error setting player ${playerId} clan:`, err);
        return false;
    }
}

async function getPlayerClanFromRedis(playerId) {
    try {
        if (!playerId) return null;
        const clanId = await redisClient.hGetAll(`player:${playerId}:clan`);
        return clanId ? { clan_id: clanId } : null;
    } catch (err) {
        console.error(`Error getting clan for player ${playerId}:`, err);
        return null;
    }
}

// ==================== Список кланов ====================
async function getAllClansFromRedis() {
    try {
        const keys = await redisClient.keys('clan:*:info');
        const clans = [];
        for (const key of keys) {
            const clanData = await getClanFromRedis(key.split(':')[1]);
            if (clanData) clans.push(clanData);
        }
        return clans;
    } catch (err) {
        console.error('Error getting all clans from Redis:', err);
        return [];
    }
}

function getClanLevelByPoints(points) {
  const thresholds = [150, 500, 2000, 5000, 10000, 20000, 35000, 60000, 100000, 150000];
  let level = 0;
  for (let i = 0; i < thresholds.length; i++) {
    if (points >= thresholds[i]) level = i + 1;
    else break;
  }
  return level;
}

module.exports = {
    init,
    saveClanToRedis,
    getClanFromRedis,
    updateClanInRedis,
    removeClanFromRedis,
    saveClanMembersToRedis,
    getClanMembersFromRedis,
    addClanMemberToRedis,
    removeClanMemberFromRedis,
    updateClanMemberLeaderFlag,
    setPlayerClanInRedis,
    getPlayerClanFromRedis,
    getAllClansFromRedis,
    updateClanMemberInRedis
};