const { db } = require('../config/db');
const shooterPool = db;

const Utils = require('../services/utils');

async function handleUpdateHeroStats(ws, data) {
    const requiredFields = ['player_id', 'hero_id', 'matches_to_add', 'hero_match'];
    if (!Utils.isValidMessage(data, requiredFields)) {
        return Utils.sendError(ws, 'Missing required fields: player_id, hero_id, matches_to_add, hero_match');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');

        await client.query(
            `UPDATE players 
             SET hero_match = $1::integer[]
             WHERE player_id = $2`,
            [data.hero_match, data.player_id]
        );

        if (data.is_favorite) {
            await client.query(
                `UPDATE players 
                 SET love_hero = $1
                 WHERE player_id = $2`,
                [data.hero_id.toString(), data.player_id]
            );
        }

        await client.query('COMMIT');

        // ОБНОВЛЯЕМ REDIS
        const redisUpdates = {
            hero_match: data.hero_match
        };
        
        if (data.is_favorite) {
            redisUpdates.love_hero = data.hero_id.toString();
        }
        
        await Utils.updatePlayerInRedis(data.player_id, redisUpdates);

        const result = await client.query(
            `SELECT hero_match, love_hero 
             FROM players 
             WHERE player_id = $1`,
            [data.player_id]
        );

        ws.send(JSON.stringify({
            action: 'update_hero_stats_response',
            success: true,
            hero_match: result.rows[0].hero_match,
            favorite_hero: result.rows[0].love_hero
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Update hero stats error:', error);
        Utils.sendError(ws, error.message);
    } finally {
        client.release();
    }
}

async function handleUpdateFavoriteHero(ws, data) {
    if (!Utils.isValidMessage(data, ['player_id', 'favorite_hero'])) {
        return Utils.sendError(ws, 'Missing player_id or favorite_hero');
    }

    const client = await shooterPool.connect();
    try {
        await client.query(
            'UPDATE players SET love_hero = $1 WHERE player_id = $2',
            [data.favorite_hero, data.player_id]
        );

        // ОБНОВЛЯЕМ REDIS
        await Utils.updatePlayerInRedis(data.player_id, {
            love_hero: data.favorite_hero
        });

        ws.send(JSON.stringify({
            action: 'update_favorite_hero_response',
            success: true,
            favorite_hero: data.favorite_hero
        }));

    } catch (error) {
        console.error('Favorite hero update error:', error);
        Utils.sendError(ws, 'Failed to update favorite hero');
    } finally {
        client.release();
    }
}

async function handleGetHeroLevels(ws, data) {
    if (!Utils.isValidMessage(data, ['player_id'])) {
        return Utils.sendError(ws, 'Missing player_id');
    }

    try {
        // Пытаемся получить из Redis сначала
        const redisData = await Utils.getPlayerFromRedis(data.player_id);
        
        if (redisData && redisData.hero_levels) {
            ws.send(JSON.stringify({
                action: 'hero_levels_response',
                hero_levels: redisData.hero_levels
            }));
            return;
        }

        // Если в Redis нет, получаем из PostgreSQL
        const client = await shooterPool.connect();
        const result = await client.query(
            'SELECT hero_levels FROM players WHERE player_id = $1',
            [data.player_id]
        );

        const heroLevels = result.rows[0]?.hero_levels || [];
        
        // Сохраняем в Redis для будущих запросов
        await Utils.updatePlayerInRedis(data.player_id, { hero_levels: heroLevels });

        ws.send(JSON.stringify({
            action: 'hero_levels_response',
            hero_levels: heroLevels
        }));

        client.release();

    } catch (error) {
        console.error('Error getting hero levels:', error);
        Utils.sendError(ws, 'Failed to get hero levels');
    }
}

async function handleUpdateHeroLevels(ws, data) {
    const requiredFields = ['player_id', 'hero_id', 'level', 'rank'];
    if (!Utils.isValidMessage(data, requiredFields)) {
        console.log('[ERROR] Missing required fields');
        return Utils.sendError(ws, 'Missing required fields');
    }

    // Validate data
    const heroId = parseInt(data.hero_id);
    const level = parseInt(data.level);
    const rank = parseInt(data.rank);
    
    if (isNaN(heroId) || heroId < 0 || heroId > 7) {
        console.log('[ERROR] Invalid hero_id:', data.hero_id);
        return Utils.sendError(ws, 'Invalid hero_id (must be 0-7)');
    }
    
    if (isNaN(level) || level < 1 || level > 50) {
        console.log('[ERROR] Invalid level:', data.level);
        return Utils.sendError(ws, 'Invalid level (must be 1-50)');
    }
    
    if (isNaN(rank) || rank < 1 || rank > 6) {
        console.log('[ERROR] Invalid rank:', data.rank);
        return Utils.sendError(ws, 'Invalid rank (must be 1-6)');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');

        // Получаем текущие уровни героев
        const currentResult = await client.query(
            'SELECT hero_levels FROM players WHERE player_id = $1',
            [data.player_id]
        );

        if (currentResult.rowCount === 0) {
            throw new Error('Player not found');
        }

        let heroLevels = currentResult.rows[0].hero_levels || [];
        
        // Если hero_levels пустой или неправильного формата, инициализируем
        if (!Array.isArray(heroLevels)) {
            heroLevels = [];
        }
        
        // Убеждаемся, что массив имеет правильную длину
        while (heroLevels.length <= heroId) {
            heroLevels.push({ rank: 1, level: 1 });
        }

        // Обновляем конкретного героя
        heroLevels[heroId] = { rank, level };

        // Convert to JSON string to ensure proper formatting
        const heroLevelsJson = JSON.stringify(heroLevels);

        // Обновляем в PostgreSQL
        const updateResult = await client.query(
            'UPDATE players SET hero_levels = $1::json WHERE player_id = $2 RETURNING hero_levels',
            [heroLevelsJson, data.player_id]
        );

        await client.query('COMMIT');

        // ОБНОВЛЯЕМ REDIS
        await Utils.updatePlayerInRedis(data.player_id, {
            hero_levels: heroLevels
        });
        
        ws.send(JSON.stringify({
            action: 'update_hero_levels_response',
            success: true,
            hero_id: heroId,
            level: level,
            rank: rank,
            hero_levels: heroLevels
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('[ERROR] Update hero levels failed:', error.stack);
        
        let errorMsg = 'Failed to update hero levels';
        if (error.message.includes('Player not found')) {
            errorMsg = 'Player not found';
        }
        
        Utils.sendError(ws, errorMsg);
    } finally {
        client.release();
    }
}

module.exports = {
    handleUpdateHeroStats,
    handleUpdateFavoriteHero,
    handleGetHeroLevels,
    handleUpdateHeroLevels
};