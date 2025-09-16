// playerInGameController.js
const {Constants,GameConstants} = require('../config/constants');

const Utils = require('../services/utils');
const playerRedisService = require('../services/playerRedisService')

class PlayerInGameController {
    constructor() {
        this.matchTTL = 30 * 30; // 15 минут для данных матча
    }

    // 🔹 Маппинг данных из Redis → структура как в Unity
    async mapTransformToPlayerData(playerId, raw) {
        if (!raw || !raw.p_x) return null;

        const playerInfo = await this.getPlayerBasicInfo(playerId);

        return {
            player_id: playerId,
            username: playerInfo.player_name || "Unknown",
            hero_id: parseInt(playerInfo.hero_id || 0),

            position: {
                x: parseFloat(raw.p_x || 0),
                y: parseFloat(raw.p_y || 0),
                z: parseFloat(raw.p_z || 0)
            },
            rotation: {
                x: parseFloat(raw.r_x || 0),
                y: parseFloat(raw.r_y || 0),
                z: parseFloat(raw.r_z || 0),
                w: parseFloat(raw.r_w || 1)
            },

            bools_state: {
                isMoving: raw.isMoving === "true",
                isShooting: raw.isShooting === "true",
                isReloading: raw.isReloading === "true",
                isHealing: raw.isHealing === "true",
                isReviving: raw.isReviving === "true",
                isPickingUp: raw.isPickingUp === "true",
                isDead: raw.isDead === "true",
            },

            is_alive: raw.is_alive === "true",
            noize_volume: parseFloat(raw.noize_volume || 0),
            current_weapon: raw.current_weapon || "secondary",
            timestamp: parseInt(raw.timestamp || Date.now())
        };
    }

    async handleUpdatePlayerTransform(ws, data) {
        const requiredFields = ['player_id', 'room_id', 'p_x', 'p_y', 'p_z', 'r_x', 'r_y', 'r_z', 'r_w'];
        if (!Utils.isValidMessage(data, requiredFields)) {
            return Utils.sendError(ws, 'Missing transform data fields');
        }

        try {
            const { player_id, room_id } = data;

            // Сохраняем трансформ
            await this.savePlayerTransform(player_id, room_id, data);

            // Получаем свои данные
            const myRaw = await global.redisClient.hGetAll(`${Constants.playerTransformKey}${player_id}`);
            const myTransform = await this.mapTransformToPlayerData(player_id, myRaw);

            // Получаем трансформы других игроков
            const otherTransforms = await this.getOtherPlayerTransforms(room_id, player_id);

            // Отправляем уже структурированный JSON
            ws.send(JSON.stringify({
                action: 'update_player_transform_response',
                success: true,
                timestamp: Date.now(),
                my_transform: myTransform,
                other_transforms: otherTransforms
            }));

        } catch (error) {
            console.error('Transform update error:', error);
            Utils.sendError(ws, 'Failed to update transform');
        }
    }

    async getOtherPlayerTransforms(roomId, excludePlayerId) {
        const playerIds = await global.redisClient.sMembers(`${Constants.roomPlayersKey}${roomId}`);
        const otherIds = playerIds.filter(id => id !== excludePlayerId);

        const otherTransforms = [];
        for (const pid of otherIds) {
            const raw = await global.redisClient.hGetAll(`${Constants.playerTransformKey}${pid}`);
            const mapped = await this.mapTransformToPlayerData(pid, raw);
            if (mapped) {
                otherTransforms.push(mapped);
            }
        }
        return otherTransforms;
    }

    async savePlayerTransform(playerId, roomId, data) {
        const transformKey = `${Constants.playerTransformKey}${playerId}`;
        const matchKey = `${Constants.matchKey}${roomId}:${playerId}`;

        const transformData = {
            // Позиция
            'p_x': data.p_x?.toString() || '0',
            'p_y': data.p_y?.toString() || '0',
            'p_z': data.p_z?.toString() || '0',

            // Ротация
            'r_x': data.r_x?.toString() || '0',
            'r_y': data.r_y?.toString() || '0',
            'r_z': data.r_z?.toString() || '0',
            'r_w': data.r_w?.toString() || '1',

            // Булевые состояния
            'isMoving': data.isMoving ? 'true' : 'false',
            'isShooting': data.isShooting ? 'true' : 'false',
            'isReloading': data.isReloading ? 'true' : 'false',
            'isHealing': data.isHealing ? 'true' : 'false',
            'isReviving': data.isReviving ? 'true' : 'false',
            'isPickingUp': data.isPickingUp ? 'true' : 'false',
            'isDead': data.isDead ? 'true' : 'false',

            // Прочее
            'noize_volume': data.noize_volume?.toString() || '0',
            'current_weapon': data.current_weapon?.toString() || 'secondary',
            'timestamp': Date.now().toString(),
            'room_id': roomId
        };

        const pipeline = global.redisClient.multi();

        // Основной трансформ
        pipeline.hSet(transformKey, transformData);
        pipeline.expire(transformKey, 300); // 5 минут TTL

        // Данные матча
        pipeline.hSet(matchKey, 'transform', JSON.stringify(transformData));
        pipeline.expire(matchKey, this.matchTTL);

        await pipeline.exec();
    }

    async getPlayerBasicInfo(playerId) {
        try {
            const playerInfo = await playerRedisService.getPlayerFromRedis(playerId);
            if (playerInfo) {
                return {
                    player_name: playerInfo.player_name || "Unknown",
                    rating: playerInfo.rating || 1000,
                    hero_id: playerInfo.hero_id || 0
                };
            }
            return { player_name: "Unknown", rating: 1000, hero_id: 0 };
        } catch (error) {
            console.error(`Error getting basic info for player ${playerId}:`, error);
            return { player_name: "Unknown", rating: 1000, hero_id: 0 };
        }
    }

    async getPlayerTransform(playerId) {
        try {
            const transformKey = `${Constants.playerTransformKey}${playerId}`;
            const transformData = await global.redisClient.hGetAll(transformKey);

            if (!transformData || !transformData.p_x) return null;

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

                // Булевые состояния
                isMoving: transformData.isMoving === 'true',
                isShooting: transformData.isShooting === 'true',
                isReloading: transformData.isReloading === 'true',
                isHealing: transformData.isHealing === 'true',
                isReviving: transformData.isReviving === 'true',
                isPickingUp: transformData.isPickingUp === 'true',
                isDead: transformData.isDead === 'true',

                // Доп
                noizeVolume: parseFloat(transformData.noize_volume || '0'),
                current_weapon: transformData.current_weapon || 'secondary',
                timestamp: parseInt(transformData.timestamp || '0'),
                is_alive: !(transformData.isDead === 'true')
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
            return { player_name: 'Unknown', rating: 1000, hero_id: 0 };
        } catch (error) {
            console.error(`Error getting basic info for player ${playerId}:`, error);
            return { player_name: 'Unknown', rating: 1000, hero_id: 0 };
        }
    }

    async updatePlayerStats(playerId, roomId, data) {
        const matchKey = `${Constants.matchKey}${roomId}:${playerId}`;
        const statsKey = `${Constants.playerStats}${roomId}:${playerId}`;

        const currentStats = await this.getPlayerStats(playerId, roomId) || {};

        // helpers
        const toFloat = (v, fallback) => {
            const n = parseFloat(v);
            return Number.isFinite(n) ? n : fallback;
        };
        const toInt = (v, fallback) => {
            const n = parseInt(v);
            return Number.isFinite(n) ? n : fallback;
        };
        const toBool = (v, fallback) => {
            if (typeof v === 'boolean') return v;
            if (typeof v === 'string') return v === 'true';
            return (typeof fallback === 'boolean') ? fallback : true;
        };

        // текущие значения из Redis (как числа)
        const cur_hp        = toFloat(currentStats.hp,         0);
        const cur_max_hp    = toFloat(currentStats.max_hp,     cur_hp);
        const cur_armor     = toFloat(currentStats.armor,      0);
        const cur_max_armor = toFloat(currentStats.max_armor,  cur_armor);
        const cur_damage    = toFloat(currentStats.damage,     0);
        const cur_kills     = toInt  (currentStats.kills,      0);
        const cur_deaths    = toInt  (currentStats.deaths,     0);
        const cur_vision    = toFloat(currentStats.vision,     0);
        const cur_score     = toFloat(currentStats.score,      0);
        const cur_alive     = toBool (currentStats.is_alive,   true);

        // входящие данные (опциональны)
        const kills     = toInt  (data.kills,     cur_kills);
        const deaths    = toInt  (data.deaths,    cur_deaths);
        const damage    = toFloat(data.damage,    cur_damage);
        const max_hp_in = ('max_hp'    in data) ? toFloat(data.max_hp,    cur_max_hp)    : cur_max_hp;
        const max_ar_in = ('max_armor' in data) ? toFloat(data.max_armor, cur_max_armor) : cur_max_armor;
        const vision    = ('vision'    in data) ? toFloat(data.vision,    cur_vision)    : cur_vision;
        const score     = ('score'     in data) ? toFloat(data.score,     cur_score)     : cur_score;

        // сначала определим новые max-значения
        const max_hp    = Math.max(0, max_hp_in);
        const max_armor = Math.max(0, max_ar_in);

        // затем — hp/armor с учётом возможных максимумов
        const new_hp_in    = ('new_hp'    in data) ? toFloat(data.new_hp,    cur_hp)    : cur_hp;
        const new_armor_in = ('new_armor' in data) ? toFloat(data.new_armor, cur_armor) : cur_armor;

        const hp    = Math.min(Math.max(0, new_hp_in),    max_hp    || new_hp_in);    // если max_hp=0, не clamp'им
        const armor = Math.min(Math.max(0, new_armor_in), max_armor || new_armor_in); // если max_armor=0, не clamp'им

        const is_alive = toBool(data.is_alive, cur_alive);

        const updatedStats = {
            hp: hp.toString(),
            max_hp: max_hp.toString(),               // <-- теперь сохраняем и отдаём
            armor: armor.toString(),
            max_armor: max_armor.toString(),         // <-- теперь сохраняем и отдаём
            kills: kills.toString(),
            deaths: deaths.toString(),
            damage: damage.toString(),
            vision: vision.toString(),               // поле на будущее
            is_alive: is_alive.toString(),
            score: Number.isFinite(score) ? score : 0,
            last_update: Date.now().toString()
        };

        const pipeline = global.redisClient.multi();
        pipeline.hSet(matchKey, updatedStats);
        pipeline.expire(matchKey, this.matchTTL);
        pipeline.hSet(statsKey, updatedStats);
        pipeline.expire(statsKey, this.matchTTL);
        await pipeline.exec();

        return updatedStats;
    }

    async getPlayerStats(playerId, roomId) {
        try {
            const matchKey = `${Constants.playerStats}${roomId}:${playerId}`;
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