// services/clanSyncService.js
const { db } = require('../config/db');

const ClanRedisService = require('./clanRedisService');
const playerRedisService = require('./playerRedisService')
const Utils = require('./utils');   

// ==================== Состояние ====================
let isSynced = false;
let syncPromise = null;
let lastSyncTime = null;

// ==================== Синхронизация всех кланов ====================
async function syncClansFromDB() {
    if (isSynced) {
        console.log('Clans already synced from DB');
        return true;
    }

    if (syncPromise) {
        return await syncPromise;
    }

    syncPromise = (async () => {
        try {
            console.log('Starting clan synchronization from DB to Redis...');

            const client = await db.connect();
            try {
                const clansResult = await client.query(`
                    SELECT 
                        c.clan_id, c.clan_name, c.leader_id, c.leader_name,
                        c.need_rating, c.is_open, c.clan_level, c.max_players
                    FROM clans c
                    ORDER BY c.clan_id
                `);

                const membersResult = await client.query(`
                    SELECT 
                        cm.clan_id, cm.player_id, cm.player_name, cm.is_leader
                    FROM clan_members cm
                    ORDER BY cm.clan_id, cm.is_leader DESC
                `);

                const membersByClan = {};
                membersResult.rows.forEach(member => {
                    if (!membersByClan[member.clan_id]) membersByClan[member.clan_id] = [];
                    membersByClan[member.clan_id].push({
                        player_id: member.player_id,
                        player_name: member.player_name,
                        is_leader: member.is_leader
                    });
                });

                let syncedCount = 0;

                for (const clan of clansResult.rows) {
                    try {
                        const members = membersByClan[clan.clan_id] || [];

                        let totalPoints = 0;
                        for (const member of members) {
                            const playerData = await playerRedisService.getPlayerFromRedis(member.player_id);
                            totalPoints += playerData?.clan_points || 0;
                        }

                        const clanData = {
                            clan_id: clan.clan_id,
                            clan_name: clan.clan_name,
                            leader_id: clan.leader_id,
                            leader_name: clan.leader_name,
                            need_rating: clan.need_rating,
                            is_open: clan.is_open,
                            clan_points: totalPoints,
                            clan_level: clan.clan_level,
                            max_players: clan.max_players,
                            player_count: members.length
                        };

                        await ClanRedisService.saveClanToRedis(clan.clan_id, clanData);
                        await ClanRedisService.saveClanMembersToRedis(clan.clan_id, members);

                        for (const member of members) {
                            const playerData = await playerRedisService.getPlayerFromRedis(member.player_id);
                            if (playerData) {
                                await Utils.updatePlayerInRedis(member.player_id, {
                                    clan_name: clan.clan_name,
                                    clan_points: playerData.clan_points || 0
                                });
                            }
                            await Utils.setPlayerClanInRedis(member.player_id, clan.clan_id);
                        }

                        syncedCount++;
                    } catch (error) {
                        console.error(`Error syncing clan ${clan.clan_id}:`, error);
                    }
                }

                lastSyncTime = new Date();
                isSynced = true;
                console.log(`Successfully synced ${syncedCount} clans from DB to Redis`);
                return true;

            } finally {
                client.release();
            }

        } catch (error) {
            console.error('Clan synchronization failed:', error);
            isSynced = false;
            throw error;
        } finally {
            syncPromise = null;
        }
    })();

    return await syncPromise;
}

// ==================== Принудительная ресинхронизация ====================
async function forceResync() {
    isSynced = false;
    return await syncClansFromDB();
}

// ==================== Статус ====================
function getSyncStatus() {
    return {
        isSynced,
        isSyncing: !!syncPromise,
        lastSyncTime
    };
}

// ==================== Очистка всех кланов в Redis ====================
async function clearAllClansFromRedis() {
    const keys = await global.redisClient.keys('clan:*:info');
    for (const key of keys) await global.redisClient.del(key);

    const memberKeys = await global.redisClient.keys('clan:*:members');
    for (const key of memberKeys) await global.redisClient.del(key);

    isSynced = false;
    console.log(`Cleared ${keys.length} clans from Redis`);
}

// ==================== Создание клана ====================
async function createClan(data) {
    // Проверка обязательных полей
    if (!data.clan_name || !data.leader_id || !data.leader_name) {
        throw new Error('Missing required fields for creating clan');
    }

    // 1️⃣ Проверяем, существует ли клан с таким именем
    const allClans = await ClanRedisService.getAllClansFromRedis();
    if (allClans.some(c => c.clan_name.toLowerCase() === data.clan_name.toLowerCase())) {
        throw new Error('Clan name already exists');
    }

    // 2️⃣ Проверяем, что игрок не состоит в другом клане
    const existingPlayerClan = await ClanRedisService.getPlayerClanFromRedis(data.player_id);
    if (existingPlayerClan) {
        throw new Error('Leader already in another clan');
    }

    // 3️⃣ Получаем данные игрока из Redis
    const leaderData = await playerRedisService.getPlayerFromRedis(data.leader_id);
    if (!leaderData) {
        throw new Error('Leader not found in Redis');
    }

    // 4️⃣ Создаём клан в БД с транзакцией
    const client = await db.connect();
    let clanId;
    try {
        await client.query('BEGIN');

        // Вставляем клан (auto-increment ID)
        const res = await client.query(`
            INSERT INTO clans (
                clan_name, leader_id, leader_name,
                need_rating, is_open, clan_points, clan_level,
                max_players, player_count
            ) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9)
            RETURNING clan_id
        `, [
            data.clan_name,
            data.leader_id, // оставляем строкой
            data.leader_name,
            data.need_rating || 0,
            data.is_open !== undefined ? data.is_open : true,
            0,      // clan_points
            1,      // clan_level
            25,     // max_players
            1       // player_count
        ]);

        clanId = res.rows[0].clan_id;

        // Добавляем лидера в участников
        await client.query(`
            INSERT INTO clan_members (clan_id, player_id, player_name, is_leader)
            VALUES ($1,$2,$3,$4)
        `, [clanId, data.leader_id, data.leader_name, true]);

        // Обновляем игрока в БД
        await client.query(`
            UPDATE players
            SET clan_name=$1
            WHERE player_id=$2
        `, [data.clan_name, data.leader_id]);

        await client.query('COMMIT');
    } catch (err) {
        await client.query('ROLLBACK');
        console.error('Error creating clan in DB:', err);
        throw err;
    } finally {
        client.release();
    }

    // 5️⃣ Сохраняем клан и участников в Redis (соблюдаем формат ClanController)
    const clanData = {
        clan_id: clanId,
        clan_name: data.clan_name,
        leader_id: data.leader_id.toString(),
        leader_name: data.leader_name,
        need_rating: data.need_rating || 0,
        is_open: data.is_open !== undefined ? data.is_open : true,
        clan_points: 0,
        clan_level: 1,
        max_players: 25,
        player_count: 1
    };

    // Сохраняем клан в Redis
    await ClanRedisService.saveClanToRedis(clanId, clanData);

    // Добавляем клан в рейтинг (zset)
    await redisClient.zAdd('clans:ranking', { score: 0, value: String(clanId) });

    // Получаем место клана в рейтинге
    const rank = await redisClient.zRevRank('clans:ranking', String(clanId));
    clanData.place = rank !== null ? rank + 1 : null;

    // Сохраняем участников в Redis
    await ClanRedisService.saveClanMembersToRedis(clanId, [{
        player_id: data.leader_id.toString(),
        player_name: data.leader_name,
        is_leader: true,
        joined_at: new Date().toISOString()
    }]);

    // Обновляем игрока
    await Utils.setPlayerClanInRedis(data.leader_id, clanId);
    await Utils.updatePlayerInRedis(data.leader_id, {
        clan_name: data.clan_name,
        clan_points: 0
    });

    // 6️⃣ Добавляем клан в рейтинг (по очкам)
    await redisClient.zAdd("clans:ranking", {
        score: clanData.clan_points,
        value: clanId.toString()
    });

    return { clanId, clanData };
}

// ==================== Синхронизация всех кланов ====================
async function syncAllClansToDatabase() {
    const client = await db.connect();

    try {
        const allClans = await ClanRedisService.getAllClansFromRedis();

        for (const clan of allClans) {
            // Вставка или обновление клана
            await client.query(`
                INSERT INTO clans (
                    clan_id, clan_name, leader_id, leader_name,
                    need_rating, is_open, clan_points, clan_level,
                    max_players, player_count
                ) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10)
                ON CONFLICT (clan_id) DO UPDATE SET
                    clan_name = EXCLUDED.clan_name,
                    leader_id = EXCLUDED.leader_id,
                    leader_name = EXCLUDED.leader_name,
                    need_rating = EXCLUDED.need_rating,
                    is_open = EXCLUDED.is_open,
                    clan_points = EXCLUDED.clan_points,
                    clan_level = EXCLUDED.clan_level,
                    max_players = EXCLUDED.max_players,
                    player_count = EXCLUDED.player_count
            `, [
                clan.clan_id,
                clan.clan_name,
                clan.leader_id,
                clan.leader_name,
                clan.need_rating,
                clan.is_open,
                clan.clanPoints || 0,
                clan.clan_level,
                clan.max_players,
                clan.player_count
            ]);

            // Обновляем участников клана
            for (const member of clan.members || []) {
                await client.query(`
                    INSERT INTO clan_members (clan_id, player_id, player_name, is_leader)
                    VALUES ($1,$2,$3,$4)
                    ON CONFLICT (clan_id, player_id) DO UPDATE SET
                        player_name = EXCLUDED.player_name,
                        is_leader = EXCLUDED.is_leader
                `, [clan.clan_id, member.player_id, member.player_name, member.is_leader]);

                await client.query(`
                    UPDATE players
                    SET clan_id=$1, clan_name=$2
                    WHERE player_id=$3
                `, [clan.clan_id, clan.clan_name, member.player_id]);
            }
        }
    } catch (err) {
        console.error('Failed to sync clans during shutdown:', err);
    } finally {
        client.release();
    }
}

// ==================== Синхронизация одного клана ====================
// async function syncClanFromRedisToDB(clanId) {
//     const client = await db.connect();
//     try {
//         const clanData = await ClanRedisService.getClanFromRedis(clanId);
//         if (!clanData) {
//             console.warn(`Clan ${clanId} not found in Redis`);
//             return false;
//         }

//         await client.query('BEGIN');

//         // Проверяем, существует ли клан в БД
//         const existingClan = await client.query(
//             'SELECT clan_id FROM clans WHERE clan_id = $1',
//             [clanId]
//         );

//         if (existingClan.rows.length === 0) {
//             // Вставка нового клана
//             await client.query(`
//                 INSERT INTO clans (
//                     clan_id, clan_name, leader_id, leader_name,
//                     need_rating, is_open, clan_points, clan_level,
//                     max_players, player_count
//                 ) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10)
//             `, [
//                 clanId,
//                 clanData.clan_name,
//                 clanData.leader_id, // строка
//                 clanData.leader_name,
//                 clanData.need_rating,
//                 clanData.is_open,
//                 clanData.clan_points,
//                 clanData.clan_level,
//                 clanData.max_players,
//                 clanData.player_count
//             ]);
//         } else {
//             // Обновление существующего клана
//             await client.query(`
//                 UPDATE clans
//                 SET clan_name=$1,
//                     leader_id=$2,
//                     leader_name=$3,
//                     need_rating=$4,
//                     is_open=$5,
//                     clan_points=$6,
//                     clan_level=$7,
//                     max_players=$8,
//                     player_count=$9
//                 WHERE clan_id=$10
//             `, [
//                 clanData.clan_name,
//                 clanData.leader_id, // строка
//                 clanData.leader_name,
//                 clanData.need_rating,
//                 clanData.is_open,
//                 clanData.clan_points,
//                 clanData.clan_level,
//                 clanData.max_players,
//                 clanData.player_count,
//                 clanId
//             ]);
//         }

//         // Синхронизация участников
//         const members = await ClanRedisService.getClanMembersFromRedis(clanId);
//         await client.query(`DELETE FROM clan_members WHERE clan_id = $1`, [clanId]);

//         for (const m of members) {
//             await client.query(`
//                 INSERT INTO clan_members (clan_id, player_id, player_name, is_leader)
//                 VALUES ($1,$2,$3,$4)
//             `, [clanId, m.player_id, m.player_name, m.is_leader]);

//             // Обновляем игрока
//             await client.query(`
//                 UPDATE players
//                 SET clan_name = $1
//                 WHERE player_id = $2
//             `, [clanData.clan_name, m.player_id]);
//         }

//         await client.query('COMMIT');
//         return true;

//     } catch (err) {
//         await client.query('ROLLBACK');
//         console.error(`Error syncing clan ${clanId} to DB:`, err);
//         return false;
//     } finally {
//         client.release();
//     }
// }
// async function syncClanFromRedisToDB(clanId) {
//     const client = await db.connect();
//     try {
//         const clanData = await ClanRedisService.getClanFromRedis(clanId);
//         if (!clanData) {
//             console.warn(`Clan ${clanId} not found in Redis`);
//             return false;
//         }

//         await client.query('BEGIN');

//         // Проверяем, существует ли клан в БД
//         const existingClan = await client.query(
//             'SELECT clan_id FROM clans WHERE clan_id = $1',
//             [clanId]
//         );

//         if (existingClan.rows.length === 0) {
//             // Вставка нового клана
//             await client.query(`
//                 INSERT INTO clans (
//                     clan_id, clan_name, leader_id, leader_name,
//                     need_rating, is_open, clan_points, clan_level,
//                     max_players, player_count
//                 ) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10)
//             `, [
//                 clanId,
//                 clanData.clan_name,
//                 clanData.leader_id,
//                 clanData.leader_name,
//                 clanData.need_rating,
//                 clanData.is_open,
//                 clanData.clan_points,
//                 clanData.clan_level,
//                 clanData.max_players,
//                 clanData.player_count
//             ]);
//         } else {
//             // Обновление существующего клана
//             await client.query(`
//                 UPDATE clans
//                 SET clan_name=$1,
//                     leader_id=$2,
//                     leader_name=$3,
//                     need_rating=$4,
//                     is_open=$5,
//                     clan_points=$6,
//                     clan_level=$7,
//                     max_players=$8,
//                     player_count=$9
//                 WHERE clan_id=$10
//             `, [
//                 clanData.clan_name,
//                 clanData.leader_id,
//                 clanData.leader_name,
//                 clanData.need_rating,
//                 clanData.is_open,
//                 clanData.clan_points,
//                 clanData.clan_level,
//                 clanData.max_players,
//                 clanData.player_count,
//                 clanId
//             ]);
//         }

//         // Получаем участников из Redis
//         const members = await ClanRedisService.getClanMembersFromRedis(clanId);

//         // Синхронизация участников с UPSERT (чтобы избежать duplicate key)
//         for (const m of members) {
//             await client.query(`
//                 INSERT INTO clan_members (clan_id, player_id, player_name, is_leader)
//                 VALUES ($1,$2,$3,$4)
//                 ON CONFLICT (clan_id, player_id) DO UPDATE
//                 SET player_name = EXCLUDED.player_name,
//                     is_leader = EXCLUDED.is_leader
//             `, [clanId, m.player_id, m.player_name, m.is_leader]);

//             // Обновляем игрока
//             await client.query(`
//                 UPDATE players
//                 SET clan_name = $1
//                 WHERE player_id = $2
//             `, [clanData.clan_name, m.player_id]);
//         }

//         await client.query('COMMIT');
//         return true;

//     } catch (err) {
//         await client.query('ROLLBACK');
//         console.error(`Error syncing clan ${clanId} to DB:`, err);
//         return false;
//     } finally {
//         client.release();
//     }
// }
// async function syncClanFromRedisToDB(clanId) {
//     const client = await db.connect();
//     try {
//         const clanData = await ClanRedisService.getClanFromRedis(clanId);
//         if (!clanData) {
//             console.warn(`Clan ${clanId} not found in Redis`);
//             return false;
//         }

//         const members = await ClanRedisService.getClanMembersFromRedis(clanId);

//         // Если участников нет — удаляем клан из Redis и БД
//         if (!members || members.length === 0) {
//             console.log(`Deleting empty clan ${clanId} from Redis and DB`);
//             await ClanRedisService.removeClanFromRedis(clanId);
//             await client.query('DELETE FROM clans WHERE clan_id=$1', [clanId]);
//             await client.query('DELETE FROM clan_members WHERE clan_id=$1', [clanId]);
//             return true;
//         }

//         await client.query('BEGIN');

//         // Вставка или обновление клана
//         const existingClan = await client.query('SELECT clan_id FROM clans WHERE clan_id = $1', [clanId]);

//         if (existingClan.rows.length === 0) {
//             await client.query(`
//                 INSERT INTO clans (
//                     clan_id, clan_name, leader_id, leader_name,
//                     need_rating, is_open, clan_points, clan_level,
//                     max_players, player_count
//                 ) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10)
//             `, [
//                 clanId,
//                 clanData.clan_name,
//                 clanData.leader_id,
//                 clanData.leader_name,
//                 clanData.need_rating,
//                 clanData.is_open,
//                 clanData.clan_points,
//                 clanData.clan_level,
//                 clanData.max_players,
//                 clanData.player_count
//             ]);
//         } else {
//             await client.query(`
//                 UPDATE clans
//                 SET clan_name=$1,
//                     leader_id=$2,
//                     leader_name=$3,
//                     need_rating=$4,
//                     is_open=$5,
//                     clan_points=$6,
//                     clan_level=$7,
//                     max_players=$8,
//                     player_count=$9
//                 WHERE clan_id=$10
//             `, [
//                 clanData.clan_name,
//                 clanData.leader_id,
//                 clanData.leader_name,
//                 clanData.need_rating,
//                 clanData.is_open,
//                 clanData.clan_points,
//                 clanData.clan_level,
//                 clanData.max_players,
//                 clanData.player_count,
//                 clanId
//             ]);
//         }

//         // Синхронизация участников
//         for (const m of members) {
//             await client.query(`
//                 INSERT INTO clan_members (clan_id, player_id, player_name, is_leader)
//                 VALUES ($1,$2,$3,$4)
//                 ON CONFLICT (clan_id, player_id) DO UPDATE
//                 SET player_name = EXCLUDED.player_name,
//                     is_leader = EXCLUDED.is_leader
//             `, [clanId, m.player_id, m.player_name, m.is_leader]);

//             await client.query(`
//                 UPDATE players
//                 SET clan_name = $1
//                 WHERE player_id = $2
//             `, [clanData.clan_name, m.player_id]);
//         }

//         await client.query('COMMIT');
//         return true;

//     } catch (err) {
//         await client.query('ROLLBACK');
//         console.error(`Error syncing clan ${clanId} to DB:`, err);
//         return false;
//     } finally {
//         client.release();
//     }
// }
async function syncClanFromRedisToDB(clanId) {
    const client = await db.connect();
    try {
        const clanData = await ClanRedisService.getClanFromRedis(clanId);
        if (!clanData) {
            console.warn(`Clan ${clanId} not found in Redis`);
            return false;
        }

        const members = await ClanRedisService.getClanMembersFromRedis(clanId);

        // Если участников нет — удаляем клан из Redis и БД
        if (!members || members.length === 0) {
            console.log(`Deleting empty clan ${clanId} from Redis and DB`);
            await ClanRedisService.removeClanFromRedis(clanId);
            await client.query('DELETE FROM clans WHERE clan_id=$1', [clanId]);
            await client.query('DELETE FROM clan_members WHERE clan_id=$1', [clanId]);
            return true;
        }

        await client.query('BEGIN');

        // Вставка или обновление клана сразу через ON CONFLICT
        await client.query(`
            INSERT INTO clans (
                clan_id, clan_name, leader_id, leader_name,
                need_rating, is_open, clan_points, clan_level,
                max_players, player_count
            ) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10)
            ON CONFLICT (clan_id) DO UPDATE
            SET clan_name = EXCLUDED.clan_name,
                leader_id = EXCLUDED.leader_id,
                leader_name = EXCLUDED.leader_name,
                need_rating = EXCLUDED.need_rating,
                is_open = EXCLUDED.is_open,
                clan_points = EXCLUDED.clan_points,
                clan_level = EXCLUDED.clan_level,
                max_players = EXCLUDED.max_players,
                player_count = EXCLUDED.player_count
        `, [
            clanId,
            clanData.clan_name,
            clanData.leader_id,
            clanData.leader_name,
            clanData.need_rating,
            clanData.is_open,
            clanData.clan_points,
            clanData.clan_level,
            clanData.max_players,
            clanData.player_count
        ]);

        // Синхронизация участников
        for (const m of members) {
            await client.query(`
                INSERT INTO clan_members (clan_id, player_id, player_name, is_leader)
                VALUES ($1,$2,$3,$4)
                ON CONFLICT (clan_id, player_id) DO UPDATE
                SET player_name = EXCLUDED.player_name,
                    is_leader = EXCLUDED.is_leader
            `, [clanId, m.player_id, m.player_name, m.is_leader]);

            await client.query(`
                UPDATE players
                SET clan_name = $1
                WHERE player_id = $2
            `, [clanData.clan_name, m.player_id]);
        }

        await client.query('COMMIT');
        return true;

    } catch (err) {
        await client.query('ROLLBACK');
        console.error(`Error syncing clan ${clanId} to DB:`, err);
        return false;
    } finally {
        client.release();
    }
}

// ==================== Экспорт ====================
module.exports = {
    syncClansFromDB,
    forceResync,
    getSyncStatus,
    clearAllClansFromRedis,
    createClan,
    syncClanFromRedisToDB,
    syncAllClansToDatabase
};