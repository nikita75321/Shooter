// playerInGameController.js
const {Constants,GameConstants} = require('../config/constants');

const Utils = require('../services/utils');
const playerRedisService = require('../services/playerRedisService')

class PlayerInGameController {
    constructor() {
        this.matchTTL = 30 * 30; // 15 минут для данных матча
    }

    async handleUpdatePlayerTransform(ws, data) {
        const requiredFields = ['player_id', 'room_id', 'p_x', 'p_y', 'p_z', 'r_x', 'r_y', 'r_z', 'r_w', 'anim'];
        if (!Utils.isValidMessage(data, requiredFields)) {
            return Utils.sendError(ws, 'Missing transform data fields');
        }

        try {
            const { player_id, room_id } = data;
            
            // Сохраняем трансформ в Redis
            await this.savePlayerTransform(player_id, room_id, data);
            
            // Получаем свои собственные данные
            const myTransform = await this.getPlayerTransform(player_id);
            // Получаем трансформы других игроков
            const otherTransforms = await this.getOtherPlayerTransforms(room_id, player_id);

            // Отправляем ответ с трансформами других игроков
            ws.send(JSON.stringify({
                action: 'player_transform_update_response',
                success: true,
                timestamp: Date.now(),
                other_transforms: otherTransforms,
                my_transform: myTransform
            }));

        } catch (error) {
            console.error('Transform update error:', error);
            Utils.sendError(ws, 'Failed to update transform');
        }
    }

    async savePlayerTransform(playerId, roomId, data) {
        const transformKey = `${Constants.playerTransformKey}${playerId}`;
        const matchKey = `${Constants.matchKey}${roomId}:${playerId}`;

        const transformData = {
            'p_x': data.p_x?.toString() || '0',
            'p_y': data.p_y?.toString() || '0',
            'p_z': data.p_z?.toString() || '0',
            'r_x': data.r_x?.toString() || '0',
            'r_y': data.r_y?.toString() || '0',
            'r_z': data.r_z?.toString() || '0',
            'r_w': data.r_w?.toString() || '1',
            'anim': data.anim?.toString() || 'idle',
            'timestamp': Date.now().toString(),
            'room_id': roomId
        };

        // Сохраняем в двух местах: отдельно для трансформа и в данных матча
        const pipeline = global.redisClient.multi();
        
        // Основной трансформ
        pipeline.hSet(transformKey, transformData);
        pipeline.expire(transformKey, 300); // 5 минут TTL
        
        // Данные матча
        pipeline.hSet(matchKey, 'transform', JSON.stringify(transformData));
        pipeline.expire(matchKey, this.matchTTL);
        
        await pipeline.exec();
    }

    async getOtherPlayerTransforms(roomId, excludePlayerId) {
        try {
            // Получаем всех игроков в комнате
            const roomPlayersKey = `${Constants.roomPlayersKey}${roomId}`;
            const playerIds = await global.redisClient.sMembers(roomPlayersKey);
            
            const otherPlayerIds = playerIds.filter(id => id !== excludePlayerId);
            if (otherPlayerIds.length === 0) return [];

            // Получаем трансформы параллельно
            const transformPromises = otherPlayerIds.map(playerId => 
                this.getPlayerTransform(playerId)
            );

            const transforms = await Promise.all(transformPromises);
            return transforms.filter(transform => transform !== null);

        } catch (error) {
            console.error(`Error getting other player transforms for room ${roomId}:`, error);
            return [];
        }
    }

    async getPlayerTransform(playerId) {
        try {
            const transformKey = `${Constants.playerTransformKey}${playerId}`;
            const transformData = await global.redisClient.hGetAll(transformKey);
            
            if (!transformData || !transformData.p_x) return null;

            // Получаем информацию об игроке
            const playerInfo = await this.getPlayerBasicInfo(playerId);

            return {
                player_id: playerId,
                player_name: playerInfo.player_name,
                position: {
                    x: parseFloat(transformData.p_x || '0'),
                    y: parseFloat(transformData.p_y || '0'),
                    z: parseFloat(transformData.p_z || '0')
                },
                rotation: {
                    x: parseFloat(transformData.r_x || '0'),
                    y: parseFloat(transformData.r_y || '0'),
                    z: parseFloat(transformData.r_z || '0'),
                    w: parseFloat(transformData.r_w || '1')
                },
                animation: transformData.anim || 'idle',
                timestamp: parseInt(transformData.timestamp || '0'),
                is_alive: await this.isPlayerAlive(playerId, transformData.room_id)
            };
        } catch (error) {
            console.error(`Error getting transform for player ${playerId}:`, error);
            return null;
        }
    }

    async getPlayerBasicInfo(playerId) {
        try {
            const playerInfo = await playerRedisService.getPlayerFromRedis(playerId);
            if (playerInfo) {
                
                return {
                    player_name: playerInfo.player_name || 'Unknown',
                    rating: playerInfo.rating || 1000,
                    hero_id: playerInfo.hero_id || 0
                };
            }

            // Если нет в Redis, возвращаем дефолт
            return { username: 'Unknown', rating: 1000, hero_id: 0 };
        } catch (error) {
            console.error(`Error getting basic info for player ${playerId}:`, error);
            return { username: 'Unknown', rating: 1000, hero_id: 0 };
        }
    }

    async updatePlayerStats(playerId, roomId, data) {
        const matchKey = `${Constants.matchKey}${roomId}:${playerId}`;
        const statsKey = `${Constants.playerStats}${roomId}:stats`;
        
        const currentStats = await this.getPlayerStats(playerId, roomId);

        const kills  = Number.isFinite(parseInt(data.kills))  ? parseInt(data.kills)  : (currentStats.kills  ?? 0);
        const deaths = Number.isFinite(parseInt(data.deaths)) ? parseInt(data.deaths) : (currentStats.deaths ?? 0);
        const damage = Number.isFinite(parseFloat(data.damage)) ? parseFloat(data.damage) : (currentStats.damage ?? 0);
        const new_hp = parseFloat(data.new_hp);
        const new_armor = parseFloat(data.new_armor);

        const updatedStats = {
            new_hp: new_hp.toString(),
            new_armor: new_armor.toString(),
            kills: kills.toString(),
            deaths: deaths.toString(),
            damage: damage.toString(),
            is_alive: ((typeof data.is_alive === "boolean") ? data.is_alive : (currentStats.is_alive ?? true)).toString(),
            last_update: Date.now().toString()
        };

        // Подстраховка от NaN
        if (!Number.isFinite(updatedStats.score)) {
            updatedStats.score = 0;
        }

        const pipeline = global.redisClient.multi();
        pipeline.hSet(matchKey, updatedStats);
        pipeline.expire(matchKey, this.matchTTL);

        // console.log("ZADD debug:", statsKey, updatedStats.score, playerId.toString());
        
        // pipeline.zAdd(statsKey, [{ score: updatedStats.score, value: playerId.toString() }]);
        pipeline.hSet(statsKey, updatedStats);
        pipeline.expire(statsKey, this.matchTTL);

        await pipeline.exec();

        return updatedStats;
    }

    // async updatePlayerStats(playerId, roomId, data) {
    //     const matchKey = `${Constants.matchKey}${roomId}:${playerId}`;
    //     const statsKey = `${Constants.matchKey}${roomId}:stats`;

    //     const currentStats = await this.getPlayerStats(playerId, roomId);
        
    //     const updatedStats = {
    //         kills: parseInt(data.kills) || currentStats.kills,
    //         deaths: parseInt(data.deaths) || currentStats.deaths,
    //         damage: parseInt(data.damage) || currentStats.damage,
    //         is_alive: Boolean(data.is_alive),
    //         score: (parseInt(data.kills) * 100) + (parseInt(data.damage) || 0),
    //         last_update: Date.now()
    //     };

    //     const pipeline = global.redisClient.multi();
        
    //     // Сохраняем индивидуальную статистику
    //     pipeline.hSet(matchKey, 'stats', JSON.stringify(updatedStats));
    //     pipeline.expire(matchKey, this.matchTTL);
        
    //     // Обновляем общую статистику комнаты
    //     pipeline.zAdd(statsKey, { score: updatedStats.score, value: playerId });
    //     pipeline.expire(statsKey, this.matchTTL);
        
    //     await pipeline.exec();

    //     return updatedStats;
    // }

    //======================Рабочий вариавнт======================
    // async getPlayerStats(playerId, roomId) {
    //     try {
    //         const matchKey = `${Constants.matchKey}${roomId}:${playerId}`;
    //         const statsJson = await global.redisClient.hGet(matchKey, 'stats');

    //         return statsJson ? JSON.parse(statsJson) : {
    //             kills: 0,
    //             deaths: 0,
    //             damage: 0,
    //             is_alive: true,
    //             score: 0,
    //             last_update: Date.now()
    //         };
    //     } catch (error) {
    //         console.error(`Error getting stats for player ${playerId}:`, error);
    //         return { kills: 0, deaths: 0, damage: 0, is_alive: true, score: 0, last_update: Date.now() };
    //     }
    // }
    //======================Рабочий вариавнт======================

    async getPlayerStats(playerId, roomId) {
        try {
            const matchKey = `player_stats:${roomId}:${playerId}`;
            const stats = await global.redisClient.hGetAll(matchKey);

            // console.log(`[getPlayerStats] matchKey - ${matchKey}`, stats);

            if (!stats || Object.keys(stats).length === 0) {
                return {
                    hp: 0,
                    max_hp: 0,
                    armor: 0,
                    max_armor: 0,
                    vision: 0,
                    kills: 0,
                    deaths: 0,
                    damage: 0,
                    respawn_time: 0,
                    last_update: Date.now()
                };
            }

            return {
                hp: parseFloat(stats.hp ?? 0),
                max_hp: parseFloat(stats.max_hp ?? 0),
                armor: parseFloat(stats.armor ?? 0),
                max_armor: parseFloat(stats.max_armor ?? 0),
                vision: parseInt(stats.vision ?? 0, 10),
                kills: parseInt(stats.kills ?? 0, 10),
                deaths: parseInt(stats.deaths ?? 0, 10),
                damage: parseFloat(stats.damage ?? 0),
                respawn_time: parseInt(stats.respawn_time ?? 0, 10),
                last_update: Date.now()
            };
        } catch (error) {
            console.error(`Error getting stats for player ${playerId}:`, error);
            return {
                hp: 0,
                max_hp: 0,
                armor: 0,
                max_armor: 0,
                vision: 0,
                kills: 0,
                deaths: 0,
                damage: 0,
                respawn_time: 0,
                last_update: Date.now()
            };
        }
    }

    async isPlayerAlive(playerId, roomId) {
        try {
            const stats = await this.getPlayerStats(playerId, roomId);
            return stats.is_alive !== false;
        } catch (error) {
            return true; // По умолчанию считаем живым
        }
    }

    async notifyRoomAboutStatChange(roomId, playerId, stats) {
        try {
            const roomManager = require('./roomManager');
            const room = await roomManager.getRoomInfo(roomId);
            
            if (room) {
                roomManager.notifyRoomPlayers(room, {
                    action: 'player_stats_updated_after_battle',
                    player_id: playerId,
                    stats: stats
                });
            }
        } catch (error) {
            console.error('Error notifying room about stat change:', error);
        }
    }

    async handleGetMatchStats(ws, data) {
        if (!Utils.isValidMessage(data, ['player_id', 'room_id'])) {
            return Utils.sendError(ws, 'Missing player_id or room_id');
        }

        try {
            const { room_id } = data;
            const stats = await this.getRoomStats(room_id);
            
            ws.send(JSON.stringify({
                action: 'match_stats_response',
                room_id: room_id,
                stats: stats,
                timestamp: Date.now()
            }));

        } catch (error) {
            console.error('Get match stats error:', error);
            Utils.sendError(ws, 'Failed to get match stats');
        }
    }

    async getRoomStats(roomId) {
        try {
            const statsKey = `${Constants.matchKey}${roomId}:stats`;
            const roomPlayersKey = `${Constants.roomPlayersKey}${roomId}`;
            
            const playerIds = await global.redisClient.sMembers(roomPlayersKey);
            const allStats = [];
            
            for (const playerId of playerIds) {
                const stats = await this.getPlayerStats(playerId, roomId);
                const info = await this.getPlayerBasicInfo(playerId);
                
                allStats.push({
                    player_id: playerId,
                    username: info.username,
                    ...stats
                });
            }
            
            // Сортируем по score
            return allStats.sort((a, b) => b.score - a.score);
            
        } catch (error) {
            console.error(`Error getting room stats for ${roomId}:`, error);
            return [];
        }
    }

    async handlePlayerDeath(ws, data) {
        if (!Utils.isValidMessage(data, ['player_id', 'room_id', 'killer_id'])) {
            return Utils.sendError(ws, 'Missing death data fields');
        }

        try {
            const { player_id, room_id, killer_id } = data;
            
            // Обновляем статистику смерти
            await this.updatePlayerStats(player_id, room_id, {
                deaths: (await this.getPlayerStats(player_id, room_id)).deaths + 1,
                is_alive: false
            });
            
            // Если есть убийца, обновляем его статистику
            if (killer_id && killer_id !== player_id) {
                await this.updatePlayerStats(killer_id, room_id, {
                    kills: (await this.getPlayerStats(killer_id, room_id)).kills + 1
                });
            }
            
            // Очищаем трансформ мертвого игрока
            await this.clearPlayerTransform(player_id);
            
            ws.send(JSON.stringify({
                action: 'player_death_response',
                success: true,
                player_id: player_id,
                killer_id: killer_id,
                timestamp: Date.now()
            }));

        } catch (error) {
            console.error('Player death handling error:', error);
            Utils.sendError(ws, 'Failed to process death');
        }
    }

    async clearPlayerTransform(playerId) {
        try {
            await global.redisClient.del(`${Constants.playerTransformKey}${playerId}`);
        } catch (error) {
            console.error(`Error clearing transform for player ${playerId}:`, error);
        }
    }

    async cleanupMatchData(roomId) {
        try {
            const roomPlayersKey = `${Constants.roomPlayersKey}${roomId}`;
            const playerIds = await global.redisClient.sMembers(roomPlayersKey);
            
            const pipeline = global.redisClient.multi();
            
            // Очищаем данные всех игроков
            for (const playerId of playerIds) {
                pipeline.del(`${Constants.playerTransformKey}${playerId}`);
                pipeline.del(`${Constants.matchKey}${roomId}:${playerId}`);
            }
            
            // Очищаем общую статистику
            pipeline.del(`${Constants.matchKey}${roomId}:stats`);
            pipeline.del(roomPlayersKey);
            
            await pipeline.exec();
            
        } catch (error) {
            console.error(`Error cleaning up match data for room ${roomId}:`, error);
        }
    }
}

module.exports = new PlayerInGameController();