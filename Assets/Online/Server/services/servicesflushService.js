const { db } = require('../config/db');
const shooterPool = db;
// const { getPlayerFromRedis,
//         deletePlayerFromRedis } = require('./utils');
const playerRedisService = require('./playerRedisService')

async function flushPlayerData(playerId) {
    try {
        console.log(`[FLUSH] Starting flush for player: ${playerId}`);
        
        // 1. Получаем данные из Redis
        const redisData = await playerRedisService.getPlayerFromRedis(playerId);
        console.log(`[${playerId}] Redis data:`, redisData);
        
        if (!redisData) {
            console.log(`[${playerId}] No data in Redis to save`);
            return;
        }

        // 2. Обновляем данные в PostgreSQL (используем данные из Redis как есть)
        await updatePlayerInPostgreSQL(playerId, redisData);
        console.log(`[${playerId}] Data updated in PostgreSQL`);

        // 3. Очищаем Redis
        // await deletePlayerFromRedis(playerId);
        // console.log(`[${playerId}] Redis cleared`);
        
    } catch (error) {
        console.error(`[${playerId}] Flush error:`, error);
    }
}

async function updatePlayerInPostgreSQL(playerId, updateData) {
    const client = await shooterPool.connect();
    try {
        // console.log(`[UPDATE] Updating player ${playerId} with:`, updateData);
        
        const result = await client.query(`
            UPDATE players 
            SET 
                player_name = COALESCE($1, player_name),
                rating = COALESCE($2, rating),
                best_rating = COALESCE($3, best_rating),
                money = COALESCE($4, money),
                donat_money = COALESCE($5, donat_money),
                clan_name = COALESCE($6, clan_name),
                clan_points = COALESCE($7, clan_points),
                overral_kill = COALESCE($8, overral_kill),
                match_count = COALESCE($9, match_count),
                win_count = COALESCE($10, win_count),
                revive_count = COALESCE($11, revive_count),
                max_damage = COALESCE($12, max_damage),
                shoot_count = COALESCE($13, shoot_count),
                open_characters = COALESCE($14::jsonb, open_characters),
                love_hero = COALESCE($15, love_hero),
                hero_card = COALESCE($16::jsonb, hero_card),
                hero_match = COALESCE($17::integer[], hero_match),
                hero_levels = COALESCE($18::jsonb, hero_levels),
                last_online = NOW()
            WHERE player_id = $19
            RETURNING *
        `, [
            updateData.player_name,
            updateData.rating,
            updateData.best_rating,
            updateData.money,
            updateData.donat_money,
            updateData.clan_name,
            updateData.clan_points,
            updateData.overral_kill,
            updateData.match_count,
            updateData.win_count,
            updateData.revive_count,
            updateData.max_damage,
            updateData.shoot_count,
            JSON.stringify(updateData.open_characters || {}),
            updateData.love_hero,
            JSON.stringify(updateData.hero_card || {}),
            updateData.hero_match || [0, 0, 0, 0, 0, 0, 0, 0],
            JSON.stringify(updateData.hero_levels || Array(8).fill({rank: 1, level: 1})),
            playerId
        ]);
        
        console.log(`[UPDATE] Rows affected: ${result.rowCount}`);
        return result.rowCount > 0;
        
    } catch (error) {
        console.error('Error updating player in PostgreSQL:', error);
        throw error;
    } finally {
        client.release();
    }
}

async function createPlayerInPostgreSQL(client, playerId, data) {
    const insertData = {
        player_name: data.player_name || data.username || 'Unknown',
        rating: data.rating || 0,
        best_rating: data.best_rating || 0,
        money: data.money || 0,
        donat_money: data.donat_money || 0,
        platform: data.platform || 'unknown',
        open_characters: JSON.stringify(data.open_characters || {}),
        love_hero: data.love_hero || data.favorite_hero || '0',
        overral_kill: data.overral_kill || data.kills || 0,
        match_count: data.match_count || 0,
        win_count: data.win_count || 0,
        revive_count: data.revive_count || 0,
        max_damage: data.max_damage || 0,
        shoot_count: data.shoot_count || 0,
        friends_reward: data.friends_reward || '',
        hero_card: JSON.stringify(data.hero_card || {}),
        hero_match: Array.isArray(data.hero_match) ? data.hero_match : [0, 0, 0, 0, 0, 0, 0, 0],
        hero_levels: JSON.stringify(data.hero_levels || Array(8).fill({rank: 1, level: 1}))
    };

    await client.query(`
        INSERT INTO players(
            player_id, player_name, rating, best_rating, money, donat_money,
            platform, open_characters, love_hero, overral_kill, match_count,
            win_count, revive_count, max_damage, shoot_count, friends_reward,
            hero_card, hero_match, hero_levels, last_online
        )
        VALUES($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16, $17, $18, $19, NOW())
    `, [
        playerId,
        insertData.player_name,
        insertData.rating,
        insertData.best_rating,
        insertData.money,
        insertData.donat_money,
        insertData.platform,
        insertData.open_characters,
        insertData.love_hero,
        insertData.overral_kill,
        insertData.match_count,
        insertData.win_count,
        insertData.revive_count,
        insertData.max_damage,
        insertData.shoot_count,
        insertData.friends_reward,
        insertData.hero_card,
        insertData.hero_match,
        insertData.hero_levels
    ]);
}

async function getPlayerFromPostgreSQL(playerId) {
    try {
        // Ваш код запроса к PostgreSQL
        const result = await pool.query(
            'SELECT player_name, rating, love_hero FROM players WHERE player_id = $1',
            [playerId]
        );
        
        if (result.rows.length > 0) {
            return {
                player_name: result.rows[0].player_name,
                rating: result.rows[0].rating,
                love_hero: result.rows[0].love_hero
            };
        }
        
        return null;
    } catch (error) {
        console.error(`Error getting player ${playerId} from PostgreSQL:`, error);
        return null;
    }
}

module.exports = { 
    flushPlayerData,
    getPlayerFromPostgreSQL
};