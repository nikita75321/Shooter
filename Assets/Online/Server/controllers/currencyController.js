// controllers/currencyController.js
const {
    isValidMessage,
    sendError
} = require('../services/utils');

const {
    getPlayerFromRedis,
    savePlayerProfileToRedis,
    updatePlayerInRedis,
    setPlayerClanInRedis
} = require('../services/playerRedisService');

async function handleClaimRewards(ws, data) {
    const requiredFields = ['player_id', 'money', 'donat_money', 'hero_cards', 'open_characters'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing required fields');
    }

    try {
        // Получаем текущие данные игрока
        const playerData = await getPlayerFromRedis(data.player_id) || {};

        // Обновляем валюту
        playerData.money = data.money || 0;
        playerData.donat_money = data.donat_money || 0;

        // Обновляем карточки героев
        if (data.hero_cards && typeof data.hero_cards === 'object') {
            playerData.hero_card = playerData.hero_card || {};
            for (const [heroName, count] of Object.entries(data.hero_cards)) {
                playerData.hero_card[heroName] = (playerData.hero_card[heroName] || 0) + (count || 0);
            }
        }

        // Обновляем открытых персонажей
        if (data.open_characters && typeof data.open_characters === 'object') {
            playerData.open_characters = {
                ...(playerData.open_characters || {}),
                ...data.open_characters
            };
        }

        // Сохраняем данные обратно в Redis
        await savePlayerProfileToRedis(data.player_id, playerData);

        // Отправляем ответ клиенту
        ws.send(JSON.stringify({
            action: 'claim_rewards_response',
            success: true,
            money: playerData.money,
            donat_money: playerData.donat_money,
            hero_cards: playerData.hero_card,
            open_characters: playerData.open_characters
        }));

    } catch (error) {
        console.error('Claim rewards error:', error);
        sendError(ws, 'Failed to claim rewards: ' + error.message);
    }
}

async function handleAddCurrency(ws, data) {
    const requiredFields = ['player_id', 'money', 'donat_money'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing required fields: player_id, money, donat_money');
    }

    try {
        const playerData = await getPlayerFromRedis(data.player_id);
        if (!playerData) return sendError(ws, 'Player not found');

        // Обновляем валюту
        playerData.money = (playerData.money || 0) + (data.money || 0);
        playerData.donat_money = (playerData.donat_money || 0) + (data.donat_money || 0);

        // Сохраняем обновленные данные
        await savePlayerProfileToRedis(data.player_id, playerData);

        ws.send(JSON.stringify({
            action: 'add_currency_response',
            success: true,
            money: playerData.money,
            donat_money: playerData.donat_money
        }));

    } catch (error) {
        console.error('Add currency error:', error);
        sendError(ws, error.message);
    }
}

async function handleSpendHeroCards(ws, data) {
    if (!isValidMessage(data, ['player_id', 'cards_to_spend'])) {
        return sendError(ws, 'Missing player_id or cards_to_spend');
    }

    try {
        const playerData = await getPlayerFromRedis(data.player_id);
        if (!playerData) return sendError(ws, 'Player not found');

        const currentCards = playerData.hero_card || {};
        const cardsToSpend = data.cards_to_spend;

        // Проверяем наличие карточек
        for (const [heroId, count] of Object.entries(cardsToSpend)) {
            if ((currentCards[heroId] || 0) < count) {
                return sendError(ws, `Not enough cards for hero ${heroId}`);
            }
        }

        // Вычитаем потраченные карточки
        const updatedCards = { ...currentCards };
        for (const [heroId, count] of Object.entries(cardsToSpend)) {
            updatedCards[heroId] -= count;
            if (updatedCards[heroId] <= 0) delete updatedCards[heroId];
        }

        playerData.hero_card = updatedCards;

        // Сохраняем обратно в Redis
        await savePlayerProfileToRedis(data.player_id, playerData);

        ws.send(JSON.stringify({
            action: 'spend_hero_cards_response',
            success: true,
            updated_cards: updatedCards
        }));

    } catch (error) {
        console.error('Spend hero cards error:', error);
        sendError(ws, error.message);
    }
}

module.exports = {
    handleClaimRewards,
    handleAddCurrency,
    handleSpendHeroCards
};

// const { db } = require('../config/db');
// const shooterPool = db;

// const { isValidMessage,
//         sendError,
//         getPlayerFromRedis,
//         savePlayerProfileToRedis
// } = require('../services/utils');

// async function handleClaimRewards(ws, data) {
//     console.log('handleClaimRewards called with data:', data);

//     const requiredFields = ['player_id', 'money', 'donat_money', 'hero_cards', 'open_characters'];

//     if (!isValidMessage(data, requiredFields)) {
//         console.log('Missing required fields');
//         return sendError(ws, 'Missing required fields');
//     }

//     try {
//         console.log(`[CLAIM] Starting for player: ${data.player_id}`);

//         // 1. Получаем текущие данные игрока из Redis
//         const playerData = await getPlayerFromRedis(data.player_id) || {};
//         console.log(`[${data.player_id}] Current Redis data:`, playerData);
        
//         // 2. Обновляем деньги
//         const updatedData = {
//             ...playerData,
//             money: (playerData.money || 0) + (data.money || 0),
//             donat_money: (playerData.donat_money || 0) + (data.donat_money || 0)
//         };
//         console.log(`[${data.player_id}] Updated data:`, updatedData);

//         // 3. Обновляем карточки героев
//         if (data.hero_cards && typeof data.hero_cards === 'object') {
//             updatedData.hero_card = updatedData.hero_card || {};
            
//             for (const [heroName, count] of Object.entries(data.hero_cards)) {
//                 const currentCount = updatedData.hero_card[heroName] || 0;
//                 updatedData.hero_card[heroName] = currentCount + (count || 0);
//             }
//         }

//         // 4. Обновляем открытых персонажей
//         if (data.open_characters && typeof data.open_characters === 'object') {
//             updatedData.open_characters = {
//                 ...(updatedData.open_characters || {}),
//                 ...data.open_characters
//             };
//         }

//         // 5. Сохраняем обновленные данные в Redis
//         await savePlayerProfileToRedis(data.player_id, updatedData);
//         console.log(`[${data.player_id}] Data saved to Redis`);

//         // 6. Отправляем ответ клиенту
//         ws.send(JSON.stringify({
//             action: 'claim_rewards_response',
//             success: true,
//             money: updatedData.money,
//             donat_money: updatedData.donat_money,
//             hero_cards: updatedData.hero_card,
//             open_characters: updatedData.open_characters
//         }));

//         // console.log(`Rewards claimed for player ${data.player_id}: +${data.money} money, +${data.donat_money} donat`);

//     } catch (error) {
//         console.error('Claim rewards error:', error);
//         sendError(ws, 'Failed to claim rewards: ' + error.message);
//     }
// }

// async function handleAddCurrency(ws, data) {
//     const requiredFields = ['player_id', 'money', 'donat_money'];
//     if (!isValidMessage(data, requiredFields)) {
//         return sendError(ws, 'Missing required fields: player_id, money, donat_money');
//     }

//     const client = await shooterPool.connect();
//     try {
//         await client.query('BEGIN');

//         // 1. Обновляем валюту игрока
//         const updateResult = await client.query(
//             `UPDATE players SET 
//                 money = money + $1,
//                 donat_money = donat_money + $2
//              WHERE player_id = $3
//              RETURNING money, donat_money`,
//             [data.money, data.donat_money, data.player_id]
//         );

//         if (updateResult.rowCount === 0) {
//             throw new Error('Player not found');
//         }

//         await client.query('COMMIT');

//         // 2. Отправляем обновленные данные
//         ws.send(JSON.stringify({
//             action: 'add_currency_response',
//             success: true,
//             money: updateResult.rows[0].money,
//             donat_money: updateResult.rows[0].donat_money
//         }));

//     } catch (error) {
//         await client.query('ROLLBACK');
//         console.error('Add currency error:', error);
//         sendError(ws, error.message);
//     } finally {
//         client.release();
//     }
// }
// async function handleSpendHeroCards(ws, data) {
//     if (!isValidMessage(data, ['player_id', 'cards_to_spend'])) {
//         return sendError(ws, 'Missing player_id or cards_to_spend');
//     }

//     const client = await shooterPool.connect();
//     try {
//         await client.query('BEGIN');
        
//         // 1. Check if player has enough cards
//         const checkResult = await client.query(
//             'SELECT hero_card FROM players WHERE player_id = $1 FOR UPDATE',
//             [data.player_id]
//         );
        
//         if (checkResult.rows.length === 0) {
//             throw new Error('Player not found');
//         }
        
//         const currentCards = checkResult.rows[0].hero_card || {};
//         const cardsToSpend = data.cards_to_spend;
        
//         // 2. Validate card counts
//         for (const [heroId, count] of Object.entries(cardsToSpend)) {
//             const currentCount = currentCards[heroId] || 0;
//             if (currentCount < count) {
//                 throw new Error(`Not enough cards for hero ${heroId}`);
//             }
//         }
        
//         // 3. Update card counts
//         const updatedCards = {...currentCards};
//         for (const [heroId, count] of Object.entries(cardsToSpend)) {
//             updatedCards[heroId] = (updatedCards[heroId] || 0) - count;
//             if (updatedCards[heroId] <= 0) {
//                 delete updatedCards[heroId];
//             }
//         }
        
//         // 4. Save updated cards
//         await client.query(
//             'UPDATE players SET hero_card = $1 WHERE player_id = $2',
//             [updatedCards, data.player_id]
//         );
        
//         await client.query('COMMIT');
        
//         ws.send(JSON.stringify({
//             action: 'spend_hero_cards_response',
//             success: true,
//             updated_cards: updatedCards
//         }));
        
//     } catch (error) {
//         await client.query('ROLLBACK');
//         console.error('Error spending hero cards:', error);
//         sendError(ws, error.message);
//     } finally {
//         client.release();
//     }
// }

// module.exports = {
//     handleClaimRewards,
//     handleAddCurrency,
//     handleSpendHeroCards
// };