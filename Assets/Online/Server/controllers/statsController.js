const { db } = require('../config/db');
const shooterPool = db;

const Utils = require('../services/utils');
const playerRedisService = require('../services/playerRedisService');

async function handleUpdatePlayerStatsAfterBattle(ws, data) {
    const requiredFields = ['player_id'];
    if (!Utils.isValidMessage(data, requiredFields)) {
        return Utils.sendError(ws, 'Missing required field: player_id');
    }

    try {
        const playerId = data.player_id;

        // 1. Получаем текущие данные игрока из Redis
        let currentData = await playerRedisService.getPlayerFromRedis(playerId);

        if (!currentData) {
            currentData = {
                rating: 0,
                best_rating: 0,
                money: 0,
                donat_money: 0,
                overral_kill: 0,
                match_count: 0,
                win_count: 0,
                revive_count: 0,
                max_damage: 0,
                shoot_count: 0,
                love_hero: null,
                clan_points: 0,
                clan_name: null,
                open_characters: {},
                hero_levels: [],
                hero_card: {},
                hero_lvl: [0,0,0],
                hero_match: Array(8).fill(0)
            };
        }

        // 2. Десериализация JSON-полей
        const openCharacters = typeof currentData.open_characters === 'string'
            ? JSON.parse(currentData.open_characters)
            : currentData.open_characters || {};
        const heroLevels = typeof currentData.hero_levels === 'string'
            ? JSON.parse(currentData.hero_levels)
            : currentData.hero_levels || [];
        const heroCard = typeof currentData.hero_card === 'string'
            ? JSON.parse(currentData.hero_card)
            : currentData.hero_card || {};
        const heroLvl = typeof currentData.hero_lvl === 'string'
            ? JSON.parse(currentData.hero_lvl)
            : currentData.hero_lvl || [0,0,0];
        const heroMatch = typeof currentData.hero_match === 'string'
            ? JSON.parse(currentData.hero_match)
            : currentData.hero_match || Array(8).fill(0);

        // 3. Вычисляем новые значения
        const ratingChange = data.rating_change || 0;
        const newRating = Math.max(0, (parseInt(currentData.rating) || 0) + ratingChange);
        const newMaxDamage = Math.max(parseInt(currentData.max_damage) || 0, data.damage_dealt || 0);
        const winIncrement = data.is_win ? 1 : 0;

        // 4. Подготавливаем обновления
        const redisUpdates = {
            rating: newRating,
            best_rating: Math.max(parseInt(currentData.best_rating) || 0, newRating),
            money: (parseInt(currentData.money) || 0) + (data.money_change || 0),
            donat_money: (parseInt(currentData.donat_money) || 0) + (data.donat_money_change || 0),
            overral_kill: (parseInt(currentData.overral_kill) || 0) + (data.kills || 0),
            match_count: (parseInt(currentData.match_count) || 0) + 1,
            win_count: (parseInt(currentData.win_count) || 0) + winIncrement,
            revive_count: (parseInt(currentData.revive_count) || 0) + (data.revives || 0),
            max_damage: newMaxDamage,
            shoot_count: (parseInt(currentData.shoot_count) || 0) + (data.shots_fired || 0)
        };

        if (data.favorite_hero) {
            redisUpdates.love_hero = data.favorite_hero;
        }

        // 5. Клановые очки остаются только у игрока, без Redis-инкремента
        if (data.clan_points_change !== undefined && currentData.clan_name) {
            redisUpdates.clan_points = data.clan_points_change;
        }

        // 6. Обновляем JSON-поля
        if (data.open_characters) Object.assign(openCharacters, data.open_characters);
        redisUpdates.open_characters = openCharacters;
        redisUpdates.hero_levels = data.hero_levels || heroLevels;
        redisUpdates.hero_card = data.hero_card || heroCard;
        redisUpdates.hero_lvl = data.hero_lvl || heroLvl;
        redisUpdates.hero_match = data.hero_match || heroMatch;

        // 7. Сохраняем игрока в Redis
        await playerRedisService.savePlayerProfileToRedis(playerId, { ...currentData, ...redisUpdates });

        // 8. Формируем ответ
        const updatedData = { ...currentData, ...redisUpdates };
        ws.send(JSON.stringify({
            action: 'player_stats_updated_after_battle',
            stats: {
                rating: updatedData.rating,
                best_rating: updatedData.best_rating,
                money: updatedData.money,
                donat_money: updatedData.donat_money,
                overral_kill: updatedData.overral_kill,
                match_count: updatedData.match_count,
                win_count: updatedData.win_count,
                revive_count: updatedData.revive_count,
                max_damage: updatedData.max_damage,
                shoot_count: updatedData.shoot_count,
                love_hero: updatedData.love_hero || null,
                clan_points: updatedData.clan_points || 0,
                open_characters: updatedData.open_characters,
                hero_levels: updatedData.hero_levels,
                hero_card: updatedData.hero_card,
                hero_lvl: updatedData.hero_lvl,
                hero_match: updatedData.hero_match
            }
        }));

    } catch (error) {
        console.error('Update player stats error:', error);
        Utils.sendError(ws, error.message);
    }
}

async function handleGetRatingLeaderboard(ws, data) {
    if (!Utils.isValidMessage(data, ['player_id'])) {
        return Utils.sendError(ws, 'Missing player_id');
    }

    const client = await shooterPool.connect();
    try {
        // 1. Get top 10 players by rating (with unique places)
        const topPlayers = await client.query(`
            WITH ranked_players AS (
                SELECT 
                    player_id,
                    player_name as name,
                    rating as value,
                    DENSE_RANK() OVER (ORDER BY rating DESC) as dense_rank,
                    ROW_NUMBER() OVER (ORDER BY rating DESC, id ASC) as unique_place
                FROM players
            )
            SELECT 
                player_id,
                name,
                value,
                unique_place as place
            FROM ranked_players
            ORDER BY unique_place
            LIMIT 10
        `);

        // 2. Get current player's position
        const myStats = await client.query(`
            WITH ranked_players AS (
                SELECT 
                    player_id,
                    player_name as name,
                    rating as value,
                    DENSE_RANK() OVER (ORDER BY rating DESC) as dense_rank,
                    ROW_NUMBER() OVER (ORDER BY rating DESC, id ASC) as unique_place
                FROM players
            )
            SELECT 
                name,
                value,
                unique_place as place
            FROM ranked_players
            WHERE player_id = $1
        `, [data.player_id]);

        ws.send(JSON.stringify({
            action: 'rating_leaderboard_response',
            top_players: topPlayers.rows,
            my_stats: myStats.rows[0] || null
        }));

    } catch (error) {
        console.error('Error in handleGetRatingLeaderboard:', error);
        Utils.sendError(ws, 'Failed to get rating leaderboard');
    } finally {
        client.release();
    }
}
async function handleGetKillsLeaderboard(ws, data) {
    if (!Utils.isValidMessage(data, ['player_id'])) {
        return Utils.sendError(ws, 'Missing player_id');
    }

    const client = await shooterPool.connect();
    try {
        // 1. Get top 10 players by kills (with unique places)
        const topPlayers = await client.query(`
            WITH ranked_players AS (
                SELECT 
                    player_id,
                    player_name as name,
                    overral_kill as value,
                    DENSE_RANK() OVER (ORDER BY overral_kill DESC) as dense_rank,
                    ROW_NUMBER() OVER (ORDER BY overral_kill DESC, id ASC) as unique_place
                FROM players
            )
            SELECT 
                player_id,
                name,
                value,
                unique_place as place
            FROM ranked_players
            ORDER BY unique_place
            LIMIT 10
        `);

        // 2. Get current player's position
        const myStats = await client.query(`
            WITH ranked_players AS (
                SELECT 
                    player_id,
                    player_name as name,
                    overral_kill as value,
                    DENSE_RANK() OVER (ORDER BY overral_kill DESC) as dense_rank,
                    ROW_NUMBER() OVER (ORDER BY overral_kill DESC, id ASC) as unique_place
                FROM players
            )
            SELECT 
                name,
                value,
                unique_place as place
            FROM ranked_players
            WHERE player_id = $1
        `, [data.player_id]);

        ws.send(JSON.stringify({
            action: 'kills_leaderboard_response',
            top_players: topPlayers.rows,
            my_stats: myStats.rows[0] || null
        }));

    } catch (error) {
        console.error('Error in handleGetKillsLeaderboard:', error);
        Utils.sendError(ws, 'Failed to get kills leaderboard');
    } finally {
        client.release();
    }
}

module.exports = {
    handleUpdatePlayerStats: handleUpdatePlayerStatsAfterBattle,
    handleGetRatingLeaderboard,
    handleGetKillsLeaderboard
};