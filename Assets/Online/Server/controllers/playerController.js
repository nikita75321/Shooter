const { db } = require('../config/db');
const shooterPool = db;

const { isValidMessage,
        sendError,
        updatePlayerInRedis } = require('../services/utils');

const playerRedisService = require('../services/playerRedisService')

//Регистрация нового игрока
async function handleRegisterPlayer(ws, data) {
    const requiredFields = ['player_name'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing required field: player_name');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');

        // 1. Проверяем, не занято ли имя
        const nameCheck = await client.query(
            'SELECT 1 FROM players WHERE player_name = $1',
            [data.player_name]
        );
        if (nameCheck.rows.length > 0) {
            throw new Error('Player name already taken');
        }

        // 2. Регистрируем нового игрока (id не передаём — он генерируется автоматически)
        const insertResult = await client.query(
            `INSERT INTO players
            (player_name, platform, open_characters, love_hero, rating, best_rating,
             money, donat_money, overral_kill, match_count, win_count, revive_count,
             max_damage, shoot_count, friends_reward, hero_card, hero_match, hero_levels)
            VALUES
            ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14,$15,$16,$17,$18)
            RETURNING id`,
            [
                data.player_name,
                data.platform || 'unknown',
                data.open_characters || '{"Kayel":[1,0,0,0,0,0,0,0,0]}',
                data.love_hero || '0',
                0, // rating
                0, // best_rating
                0, // money
                0, // donat_money
                0, // overral_kill
                0, // match_count
                0, // win_count
                0, // revive_count
                0, // max_damage
                0, // shoot_count
                '', // friends_reward
                '{}', // hero_card
                [0,0,0,0,0,0,0,0], // hero_match
                JSON.stringify(Array(8).fill({rank:1, level:1})) // hero_levels
            ]
        );

        // 3. Генерируем player_id на основе нового id
        const playerId = `${insertResult.rows[0].id}-${Math.floor(10000 + Math.random() * 90000)}`;

        // 4. Обновляем запись с player_id
        await client.query(
            'UPDATE players SET player_id = $1 WHERE id = $2',
            [playerId, insertResult.rows[0].id]
        );

        // 5. Получаем полные данные игрока
        const result = await client.query(
            `SELECT id, player_name, player_id, rating, money, donat_money,
                    platform, open_characters, love_hero
             FROM players WHERE id = $1`,
            [insertResult.rows[0].id]
        );

        await client.query('COMMIT');

        // 6. Сохраняем данные в Redis
        const playerData = {
            player_id: playerId,
            player_name: data.player_name,
            rating: 0,
            best_rating: 0,
            money: 0,
            donat_money: 0,
            platform: data.platform || 'unknown',
            open_characters: data.open_characters || {"Kayel":[1,0,0,0,0,0,0,0,0]},
            love_hero: data.love_hero || '0',
            overral_kill: 0,
            match_count: 0,
            win_count: 0,
            revive_count: 0,
            max_damage: 0,
            shoot_count: 0,
            friends_reward: '',
            hero_card: {},
            hero_match: [0,0,0,0,0,0,0,0],
            hero_levels: Array(8).fill({rank:1, level:1})
        };
        await playerRedisService.savePlayerProfileToRedis(playerId, playerData);

        // 7. Подключаем игрока
        ws.playerId = playerId;
        ws.playerName = data.player_name;
        global.connectedPlayers.set(playerId, ws);

        // 8. Отправляем ответ клиенту
        ws.send(JSON.stringify({
            action: 'register_player_response',
            success: true,
            id: result.rows[0].id,
            player_name: result.rows[0].player_name,
            player_id: result.rows[0].player_id,
            rating: result.rows[0].rating,
            money: result.rows[0].money,
            donat_money: result.rows[0].donat_money,
            platform: result.rows[0].platform,
            open_characters: typeof result.rows[0].open_characters === 'string'
                ? JSON.parse(result.rows[0].open_characters)
                : result.rows[0].open_characters,
            favorite_hero: result.rows[0].love_hero,
            connected: true
        }));

        console.log(`Player ${data.player_name} (${playerId}) registered and connected successfully`);
    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Register player error:', error);
        if (error.message.includes('unique constraint') || error.message.includes('already taken')) {
            sendError(ws, 'Player name already exists');
        } else {
            sendError(ws, error.message);
        }
    } finally {
        client.release();
    }
}

// Создание/обновление игрока
async function handlePlayerConnect(ws, data) {
    if (!isValidMessage(data, ['player_id', 'player_name'])) {
        return sendError(ws, 'Missing required fields: player_id or player_name for connection');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');
        
        const updateResult = await client.query(
            'UPDATE players SET player_name = $1, last_online = NOW() WHERE player_id = $2 RETURNING id',
            [data.player_name, data.player_id]
        );
        
        if (updateResult.rowCount === 0) {
            throw new Error('Player not found. Please register first.');
        }
        
        await client.query('COMMIT');
        
        // Сохраняем информацию о подключении
        ws.playerId = data.player_id;
        ws.playerName = data.player_name;
        
        // Добавляем в глобальную мапу подключенных игроков
        global.connectedPlayers.set(data.player_id, ws);
        
        ws.send(JSON.stringify({
            action: 'player_connect_response',
            success: true,
            player_id: data.player_id,
            player_name: data.player_name
        }));

        console.log(`Player ${data.player_name} (${data.player_id}) connected successfully`);

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error in handlePlayerConnect:', error);
        
        ws.send(JSON.stringify({
            action: 'player_connect_response',
            success: false,
            error: error.message
        }));
    } finally {
        client.release();
    }
}

// Получение информации об игроке
async function handleGetPlayerInfo(ws, data) {
    if (!isValidMessage(data, ['player_id'])) {
        throw new Error('Missing player_id');
    }

    const client = await shooterPool.connect();
    try {
        const result = await client.query(
            'SELECT player_id, player_name, rating FROM players WHERE player_id = $1',
            [data.player_id]
        );
        
        if (result.rows.length > 0) {
            ws.send(JSON.stringify({
                action: 'get_player_info_response',
                player: result.rows[0]
            }));
        } else {
            sendError(ws, 'Player not found');
        }
    } catch (error) {
        console.error('Error in handleGetPlayerInfo:', error);
        sendError(ws, 'Failed to get player info');
    } finally {
        client.release();
    }
}
// Обновление рейтинга игрока
async function handleUpdatePlayerRating(ws, data) {
    if (!isValidMessage(data, ['player_id', 'rating_change'])) {
        throw new Error('Missing player_id or rating_change');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');
        
        await client.query(
            'UPDATE players SET rating = rating + $1 WHERE player_id = $2',
            [data.rating_change, data.player_id]
        );
        
        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'update_rating_response',
            success: true
        }));
    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error in handleUpdatePlayerRating:', error);
        sendError(ws, 'Failed to update player rating');
    } finally {
        client.release();
    }
}
// Проверка доступности имени
async function handleCheckName(ws, data) {
    if (!isValidMessage(data, ['player_name'])) {
        throw new Error('Missing player_name');
    }

    const client = await shooterPool.connect();
    try {
        const result = await client.query(
            'SELECT 1 FROM players WHERE player_name = $1',
            [data.player_name]
        );
        
        ws.send(JSON.stringify({
            action: 'check_name_response',
            available: result.rows.length === 0,
            requested_name: data.player_name
        }));
    } catch (error) {
        console.error('Error in handleCheckName:', error);
        sendError(ws, 'Failed to check name availability');
    } finally {
        client.release();
    }
}
// Обновление имени игрока
async function handleUpdatePlayerName(ws, data) {
    if (!isValidMessage(data, ['player_id', 'new_name'])) {
        return sendError(ws, 'Missing player_id or new_name');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');
        
        // 1. Получаем внутренний ID игрока по его player_id (строке)
        const playerRes = await client.query(
            'SELECT id FROM players WHERE player_id = $1',
            [data.player_id]
        );
        
        if (playerRes.rows.length === 0) {
            throw new Error('Player not found');
        }
        const playerId = playerRes.rows[0].id;

        // 2. Проверка на существование имени (исключая текущего игрока)
        const nameCheck = await client.query(
            'SELECT 1 FROM players WHERE player_name = $1 AND player_id != $2',
            [data.new_name, data.player_id]
        );
        
        if (nameCheck.rows.length > 0) {
            throw new Error('Player name already taken');
        }

        // 3. Обновление имени в таблице players (по строковому player_id)
        await client.query(
            'UPDATE players SET player_name = $1 WHERE player_id = $2',
            [data.new_name, data.player_id]
        );
        
        // 4. Обновление имени в clan_members (по внутреннему числовому ID)
        await client.query(
            'UPDATE clan_members SET player_name = $1 WHERE player_id = $2',
            [data.new_name, playerId]
        );
        
        await client.query('COMMIT');
        await updatePlayerInRedis(data.player_id, {
            player_name: data.new_name,
            username: data.new_name
        });
        
        ws.send(JSON.stringify({
            action: 'update_name_response',
            success: true,
            new_name: data.new_name
        }));

    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error updating player name:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}
// Обновление рейтинга
async function handleUpdateRating(ws, data) {
    if (!isValidMessage(data, ['player_name', 'rating_change'])) {
        throw new Error('Missing player_name or rating_change');
    }

    const client = await shooterPool.connect();
    try {
        await client.query('BEGIN');
        
        await client.query(
            'UPDATE players SET rating = rating + $1 WHERE player_name = $2',
            [data.rating_change, data.player_name]
        );
        
        await client.query('COMMIT');
        
        ws.send(JSON.stringify({
            action: 'update_rating_response',
            success: true
        }));
    } catch (error) {
        await client.query('ROLLBACK');
        console.error('Error in handleUpdateRating:', error);
        sendError(ws, error.message);
    } finally {
        client.release();
    }
}
//Получение данных игрока
async function handleGetPlayerData(ws, data) {
    const requiredFields = ['player_id'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing required fields: player_id');
    }

    try {
        let fromRedis = true;
        let playerData = await playerRedisService.getPlayerFromRedis(data.player_id);

        if (!playerData) {
            fromRedis = false;
            const client = await shooterPool.connect();
            try {
                const result = await client.query(
                    `SELECT 
                        p.player_id,
                        p.player_name,
                        p.rating,
                        p.best_rating,
                        p.money,
                        p.donat_money,
                        p.clan_name,
                        p.clan_points,
                        p.platform,
                        p.open_characters,
                        p.love_hero,
                        p.overral_kill,
                        p.match_count,
                        p.win_count,
                        p.revive_count,
                        p.max_damage,
                        p.shoot_count,
                        p.friends_reward,
                        p.hero_card,
                        p.hero_match,
                        p.hero_levels,
                        c.clan_id
                     FROM players p
                     LEFT JOIN clans c ON p.clan_name = c.clan_name
                     WHERE p.player_id = $1`,
                    [data.player_id]
                );

                if (result.rows.length === 0) throw new Error('Player not found');
                playerData = result.rows[0];

                await playerRedisService.savePlayerProfileToRedis(data.player_id, playerData);
            } finally {
                client.release();
            }
        }

        const openCharacters = playerData.open_characters || {};
        const heroCard = playerData.hero_card || {};
        const heroMatch = playerData.hero_match || Array(8).fill(0);
        const heroLevels = playerData.hero_levels || Array(8).fill({ rank: 1, level: 1 });

        const clanData = playerData.clan_name
            ? {
                id: playerData.clan_id || 0,
                name: playerData.clan_name || "",
                points: Number(playerData.clan_points) || 0
            }
            : null;

        const stats = {
            overral_kill: Number(playerData.overral_kill) || 0,
            matches: Number(playerData.match_count) || 0,
            win_count: Number(playerData.win_count) || 0,
            lose_count: (Number(playerData.match_count) || 0) - (Number(playerData.win_count) || 0),
            revive_count: Number(playerData.revive_count) || 0,
            max_damage: Number(playerData.max_damage) || 0,
            shoot_count: Number(playerData.shoot_count) || 0
        };

        ws.send(JSON.stringify({
            action: 'get_player_data_response',
            success: true,
            from_cache: fromRedis,
            player: {
                id: playerData.player_id,
                username: playerData.username || playerData.player_name || "",
                rating: Number(playerData.rating) || 0,
                bestRating: Number(playerData.best_rating) || 0,
                money: Number(playerData.money) || 0,
                donatMoney: Number(playerData.donat_money) || 0,
                clan: clanData,
                stats: stats,
                characters: openCharacters,
                favoriteHero: playerData.love_hero || "0",
                platform: playerData.platform || "unknown",
                friendsReward: playerData.friends_reward || "",
                hero_card: heroCard,
                hero_match: heroMatch,
                hero_levels: heroLevels
            }
        }));
    } catch (error) {
        console.error('Get player data error:', error);
        sendError(ws, error.message);
    }
}

module.exports = {
    handleRegisterPlayer,
    handlePlayerConnect,
    handleGetPlayerInfo,
    handleUpdatePlayerRating,
    handleCheckName,
    handleUpdatePlayerName,
    handleUpdateRating,
    handleGetPlayerData
};