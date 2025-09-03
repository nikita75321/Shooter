const Utils = require('../services/utils');

async function fixAllCorruptedData() {
    try {
        // Получаем все ключи игроков
        const keys = await global.redisClient.keys('player:*:profile');
        
        console.log(`Found ${keys.length} player keys to check`);
        
        for (const key of keys) {
            const playerId = key.split(':')[1];
            await Utils.fixCorruptedPlayerData(playerId);
        }
        
        console.log('Finished fixing all corrupted data');
    } catch (error) {
        console.error('Error in mass fix:', error);
    }
}

module.exports = {
    fixAllCorruptedData
};