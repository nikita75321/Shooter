const boostRedisService = require('../services/boostRedisService');
const playerInGameController = require('./playerInGameController');
const { roomManager } = require('./roomManager');
const Utils = require('../services/utils');

class BoostController {
    async spawnBoosts(ws, data) {
        if (!Utils.isValidMessage(data, ['room_id', 'player_id', 'boosts'])) {
            return Utils.sendError(ws, 'Missing spawn boost fields');
        }

        const { room_id, boosts } = data;

        if (!Array.isArray(boosts) || boosts.length === 0) {
            return Utils.sendError(ws, 'Boost list is empty');
        }

        try {
            for (const boost of boosts) {
                const { boost_id, type, p_x, p_y, p_z } = boost;

                // сохраняем буст в Redis
                // await boostRedisService.addBoost(room_id, bo{
                //     boost_id: boost_id,
                //     type: type,
                //     p_x: p_x,
                //     p_y: p_y,
                //     p_z: p_z,
                //     is_taken: false
                // });
                await boostRedisService.addBoost(room_id, boost);
            }

            // отправляем ответ клиенту
            ws.send(JSON.stringify({
                action: 'spawn_room_boosts_response',
                success: true,
                room_id,
                boosts
            }));

            console.log(`[Boosts] Spawned ${boosts.length} boosts in room ${room_id}`);
        } catch (err) {
            console.error('Error spawning boosts:', err);
            Utils.sendError(ws, 'Failed to spawn boosts');
        }
    }

    async handleBoostPickup(ws, data) {
        if (!Utils.isValidMessage(data, ['player_id', 'room_id', 'boost_id'])) {
            return Utils.sendError(ws, 'Missing boost pickup fields');
        }

        const { player_id, room_id, boost_id } = data;

        try {
            // 1. Получаем буст
            const boost = await boostRedisService.getBoost(room_id, boost_id);
            if (!boost || !boost.type) {
                return Utils.sendError(ws, 'Boost not found');
            }

            // Проверяем, не подобран ли
            if (boost.is_taken === "true") {
                return Utils.sendError(ws, 'Boost already taken');
            }

            // 2. Получаем статы игрока
            const playerStats = await playerInGameController.getPlayerStats(player_id, room_id);
            if (!playerStats) {
                return Utils.sendError(ws, 'Player stats not found');
            }

            // 3. Если буст типа "armor" → обновляем броню
            let updatedStats = {};
            if (boost.type === "armor") {
                updatedStats = await playerInGameController.updatePlayerStats(player_id, room_id, {
                    new_hp: playerStats.hp, // оставляем текущее
                    new_armor: playerStats.max_armor, // даём максимум брони
                    kills: playerStats.kills,
                    deaths: playerStats.deaths,
                    damage: playerStats.damage,
                    is_alive: true
                });
            }

            // 4. Помечаем буст как "подобран"
            await boostRedisService.markBoostAsTaken(room_id, boost_id);

            // 5. Оповещаем комнату
            const room = await roomManager.getRoomInfo(room_id);
            if (room) {
                roomManager.notifyRoomPlayers(room, {
                    action: 'boost_taken',
                    player_id: player_id,
                    boost_id: boost_id,
                    boost_type: boost.type,
                    new_stats: updatedStats
                });
            }

            // 6. Отправляем ответ игроку
            ws.send(JSON.stringify({
                success: true,
                player_id: player_id,
                boost_id: boost_id,
                boost_type: boost.type,
            }));

        } catch (err) {
            console.error('Error handling boost pickup:', err);
            Utils.sendError(ws, 'Failed to pickup boost');
        }
    }
}

module.exports = new BoostController();