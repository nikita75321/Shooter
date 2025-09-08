// damageController.js
const { Constants } = require('../config/constants');
const Utils = require('../services/utils');
const playerInGameController = require('./playerInGameController');
const { roomManager } = require('./roomManager');

class DamageController {
    constructor() {
        this.matchTTL = 60 * 60 * 2;
        this.maxShotDistance = 50;   // дальность стрельбы
        this.playerHitRadius = 1.8;  // радиус попадания
    }

    // console.log('handleDealDamage called', data);
    // console.log('Target player:', playerId, 'transform:', targetTransform, 'is_alive:', targetTransform?.is_alive);
    // console.log('Check hit result:', hit);
    // console.log('Saving to Redis:', statsKey, 'hp:', hp, 'armor:', armor);
   async handleDealDamage(ws, data) {
        try {
            const requiredFields = [
                'attacker_id', 'room_id',
                'shot_origin_x', 'shot_origin_y', 'shot_origin_z',
                'shot_dir_x', 'shot_dir_y', 'shot_dir_z',
                'damage'
            ];

            for (const field of requiredFields) {
                if (data[field] === undefined || data[field] === null) {
                    console.warn(`Missing damage data field: ${field}`);
                    return Utils.sendError(ws, `Missing damage data field: ${field}`);
                }
            }

            const { attacker_id, room_id, damage } = data;
            console.log('handleDealDamage called', data);

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

            const dir = this.normalize(shot_direction);

            const room = await roomManager.getRoomInfo(room_id);
            if (!room) {
                console.warn(`Room not found: ${room_id}`);
                return Utils.sendError(ws, 'Room not found');
            }
            
            const pipeline = global.redisClient.multi();
            console.log(`Room loaded: ${room_id}, players: ${room.players.join(', ')}`);

            for (const playerId of room.players) {
                if (!playerId) continue; // защита от пустых значений
                if (playerId === attacker_id) continue;

                console.log(`Processing target player: ${playerId}`);

                const targetTransform = await playerInGameController.getPlayerTransform(playerId);
                if (!targetTransform) {
                    console.log(`No transform for player: ${playerId}`);
                    continue;
                }
                // if (!targetTransform.is_alive) {
                //     console.log(`Player ${playerId} is dead, skipping`);
                //     continue;
                // }

                const hit = this.checkHit(shot_origin, dir, targetTransform.position);
                // const hit = true;
                console.log(`Check hit for player ${playerId}: ${hit}`);
                if (!hit) continue;

                const statsKey = `player_stats:${room_id}:${playerId}`;
                const targetStats = await global.redisClient.hGetAll(statsKey);

                if (!targetStats || !targetStats.hp) {
                    console.warn(`Stats not found for player: ${playerId}`);
                    continue;
                }

                let hp = parseFloat(targetStats.hp);
                let armor = parseFloat(targetStats.armor);

                console.log(`Before damage: player=${playerId}, hp=${hp}, armor=${armor}`);

                let damageToHp = 0;
                if (armor > 0) {
                    const damageToArmor = Math.min(damage, armor);
                    armor -= damageToArmor;
                    damageToHp = damage - damageToArmor;
                    hp -= damageToHp;
                } else {
                    hp -= damage;
                    damageToHp = damage;
                }

                console.log(`After damage: player=${playerId}, hp=${hp}, armor=${armor}`);

                // Запись в Redis
                // await global.redisClient.hSet(statsKey,
                //     'hp', hp.toString(),
                //     'armor', armor.toString()
                // );

                // --- запись в Redis через pipeline ---
                console.log("Saving to redis:", { hp, armor });
                pipeline.hSet(statsKey, 'hp', hp);
                pipeline.hSet(statsKey, 'armor', armor);

                console.log(`Updated Redis for player ${playerId}: hp=${hp}, armor=${armor}`);

                // Обновляем статистику стрелка
                const attackerStats = await playerInGameController.getPlayerStats(attacker_id, room_id);
                console.log(`[attackerStats] ` + attackerStats);
                
                await playerInGameController.updatePlayerStats(attacker_id, room_id, {
                    damage: attackerStats.damage + damage
                });
                console.log(`Updated attacker ${attacker_id} total damage: ${attackerStats.damage + damage}`);

                // Проверка смерти
                if (hp <= 0) {
                    const deaths = parseInt(targetStats.deaths || "0", 10) + 1;
                    const respawnTime = Date.now() + 5000; // респаун через 5 секунд

                    await global.redisClient.hSet(statsKey, {
                        deaths,
                        respawn_time: respawnTime,
                        hp: 0,
                        armor: 0
                    });
                    console.log(`Player ${playerId} died. Respawn at ${new Date(respawnTime).toLocaleTimeString()}`);

                    if (attacker_id !== playerId) {
                        const attackerStatsKey = `player_stats:${room_id}:${attacker_id}`;
                        const attackerKills = parseInt(attackerStats.kills || "0", 10) + 1;
                        await global.redisClient.hSet(attackerStatsKey, 'kills', attackerKills);
                        console.log(`Attacker ${attacker_id} kills incremented: ${attackerKills}`);
                    }

                    await playerInGameController.handlePlayerDeath(ws, {
                        player_id: playerId,
                        room_id,
                        killer_id: attacker_id
                    });
                }

                // Уведомляем всех игроков
                await roomManager.notifyRoomPlayers(room, {
                    action: 'player_damaged',
                    attacker_id,
                    target_id: playerId,
                    amount: damage,
                    new_hp: Math.max(hp, 0),
                    new_armor: Math.max(armor, 0),
                    timestamp: Date.now()
                });
                console.log(`Notified room players about damage to ${playerId}`);
                
                ws.send(JSON.stringify({
                    action: 'deal_damage_response',
                    success: true,
                    attacker_id,
                    target_id: playerId,
                    damage,
                    new_hp: hp,
                    new_armor: armor,
                    room_id
                }));
                console.log(`Damage response sent to attacker ${attacker_id}`);
            }

            // вот тут выполняем все команды разом
            await pipeline.exec();

        } catch (error) {
            console.error('Damage handling error:', error);
            Utils.sendError(ws, 'Failed to deal damage');
        }
    }

    // Проверка попадания (луч → сфера)
    checkHit(origin, dir, targetPos) {
        // console.log('CheckHit:', { origin, dir, targetPos, proj, distSq, maxShotDistance: this.maxShotDistance, playerHitRadius: this.playerHitRadius });

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

        console.log('CheckHit:', { origin, dir, targetPos, proj, distSq });

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