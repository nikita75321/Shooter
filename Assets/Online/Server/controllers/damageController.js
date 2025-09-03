// damageController.js
const { Constants } = require('../config/constants');
const Utils = require('../services/utils');
const playerInGameController = require('./playerInGameController');
const { roomManager } = require('./roomManager');

class DamageController {
    constructor() {
        this.matchTTL = 60 * 60 * 2;
        this.maxShotDistance = 50;   // дальность стрельбы
        this.playerHitRadius = 0.8;  // радиус попадания
    }

    async handleDealDamage(ws, data) {
        try {
            // Проверяем все нужные поля
            const requiredFields = [
                'attacker_id', 'room_id',
                'shot_origin_x', 'shot_origin_y', 'shot_origin_z',
                'shot_dir_x', 'shot_dir_y', 'shot_dir_z',
                'damage'
            ];

            for (const field of requiredFields) {
                if (data[field] === undefined || data[field] === null) {
                    return Utils.sendError(ws, `Missing damage data field: ${field}`);
                }
            }

            const { attacker_id, room_id, damage } = data;

            // Собираем объекты из плоских полей
            const shot_origin = {
                x: data.shot_origin_x,
                y: data.shot_origin_y,
                z: data.shot_origin_z
            };

            const shot_direction = {
                x: data.shot_dir_x,
                y: data.shot_dir_y,
                z: data.shot_dir_z
            };

            // Нормализуем направление выстрела
            const dir = this.normalize(shot_direction);

            const room = await roomManager.getRoomInfo(room_id);
            if (!room) return Utils.sendError(ws, 'Room not found');

            // Перебор всех игроков комнаты
            for (const player of room.players) {
                if (player.player_id === attacker_id) continue; // не стрелять в себя

                const targetTransform = await playerInGameController.getPlayerTransform(player.player_id);
                if (!targetTransform || !targetTransform.is_alive) continue;

                // Проверка попадания
                const hit = this.checkHit(shot_origin, dir, targetTransform.position);
                if (!hit) continue;

                // --- обновляем HP ---
                const hpKey = `${Constants.matchKey}${room_id}:${player.player_id}:hp`;
                let currentHp = await global.redisClient.get(hpKey);
                currentHp = currentHp ? parseInt(currentHp) : 100;

                const newHp = Math.max(0, currentHp - damage);
                await global.redisClient.setEx(hpKey, this.matchTTL, newHp.toString());

                // Обновляем статистику стрелка
                const attackerStats = await playerInGameController.getPlayerStats(attacker_id, room_id);
                await playerInGameController.updatePlayerStats(attacker_id, room_id, {
                    damage: attackerStats.damage + damage
                });

                // Смерть игрока
                if (newHp <= 0) {
                    await playerInGameController.handlePlayerDeath(ws, {
                        player_id: player.player_id,
                        room_id,
                        killer_id: attacker_id
                    });
                }

                // Рассылаем всем игрокам в комнате
                await roomManager.notifyRoomPlayers(room, {
                    action: 'player_damaged',
                    attacker_id,
                    target_id: player.player_id,
                    amount: damage,
                    new_hp: newHp,
                    timestamp: Date.now()
                });
            }

            // Ответ стрелку
            ws.send(JSON.stringify({
                action: 'damage_dealt_response',
                success: true,
                attacker_id,
                room_id
            }));

        } catch (error) {
            console.error('Damage handling error:', error);
            Utils.sendError(ws, 'Failed to deal damage');
        }
    }

    // Проверка попадания (луч → сфера)
    checkHit(origin, dir, targetPos) {
        const toTarget = {
            x: targetPos.x - origin.x,
            y: targetPos.y - origin.y,
            z: targetPos.z - origin.z
        };

        const proj = this.dot(toTarget, dir);
        if (proj < 0 || proj > this.maxShotDistance) return false;

        const closestPoint = {
            x: origin.x + dir.x * proj,
            y: origin.y + dir.y * proj,
            z: origin.z + dir.z * proj
        };

        const distSq = this.distanceSquared(closestPoint, targetPos);
        return distSq <= this.playerHitRadius * this.playerHitRadius;
    }

    normalize(v) {
        const len = Math.sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        return len > 0 ? { x: v.x / len, y: v.y / len, z: v.z / len } : { x: 0, y: 0, z: 0 };
    }

    dot(a, b) {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    distanceSquared(a, b) {
        const dx = a.x - b.x;
        const dy = a.y - b.y;
        const dz = a.z - b.z;
        return dx * dx + dy * dy + dz * dz;
    }
}

module.exports = new DamageController();