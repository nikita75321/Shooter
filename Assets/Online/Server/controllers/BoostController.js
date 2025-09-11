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

            // 3. Обработка разных типов бустов
            let updatedStats = null;

            switch (boost.type) {
                case "armor":
                    updatedStats = await playerInGameController.updatePlayerStats(player_id, room_id, {
                        new_hp: playerStats.hp,
                        new_armor: playerStats.max_armor,
                        kills: playerStats.kills,
                        deaths: playerStats.deaths,
                        damage: playerStats.damage,
                        is_alive: true
                    });
                    console.log(`new armor = ${playerStats.max_armor}`);
                    
                    break;

                case "aidkit":
                    // ничего не обновляем, просто успех
                    updatedStats = playerStats;
                    break;

                case "ammo":
                    // ничего не обновляем, просто успех
                    updatedStats = playerStats;
                    break;

                default:
                    return Utils.sendError(ws, `Unknown boost type: ${boost.type}`);
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
                action: 'boost_pickup_response',
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