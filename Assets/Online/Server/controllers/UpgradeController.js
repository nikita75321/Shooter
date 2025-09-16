const { Constants } = require('../config/constants');
const Utils = require('../services/utils');
const { roomManager } = require('./roomManager');

class UpgradeController {
    constructor() {
        this.upgradeTTL = 60 * 15; // 15 минут
    }

    getUpgradeKey(roomId, upgradeId) {
        return `${Constants.upgradeKey}${roomId}:${upgradeId}`;
    }

    getRoomUpgradesKey(roomId) {
        return `${Constants.roomUpgradesKey}${roomId}`;
    }

    getPlayerUpgradesKey(roomId, playerId) {
        return `${Constants.playerUpgradesKey}${roomId}:${playerId}`;
    }

    async spawnUpgrades(ws, data) {
        if (!Utils.isValidMessage(data, ['room_id', 'upgrades'])) {
            return Utils.sendError(ws, 'Missing spawn upgrade fields');
        }

        const { room_id, upgrades } = data;
        if (!Array.isArray(upgrades) || upgrades.length === 0) {
            return Utils.sendError(ws, 'Upgrade list is empty');
        }

        try {
            const pipeline = global.redisClient.multi();

            for (const upgrade of upgrades) {
                const upgradeKey = this.getUpgradeKey(room_id, upgrade.upgrade_id);
                const roomUpgradesKey = this.getRoomUpgradesKey(room_id);

                const data = {
                    upgrade_id: upgrade.upgrade_id.toString(),
                    type: upgrade.type,
                    p_x: upgrade.p_x.toString(),
                    p_y: upgrade.p_y.toString(),
                    p_z: upgrade.p_z.toString(),
                    is_taken: "false",
                    owner_id: "",
                    timestamp: Date.now().toString()
                };

                pipeline.hSet(upgradeKey, data);
                pipeline.expire(upgradeKey, this.upgradeTTL);
                pipeline.sAdd(roomUpgradesKey, upgradeKey);
                pipeline.expire(roomUpgradesKey, this.upgradeTTL);
            }

            await pipeline.exec();

            ws.send(JSON.stringify({
                action: 'spawn_room_upgrades_response',
                success: true,
                room_id,
                upgrades
            }));

            console.log(`[Upgrades] Spawned ${upgrades.length} upgrades in room ${room_id}`);
        } catch (err) {
            console.error('Error spawning upgrades:', err);
            Utils.sendError(ws, 'Failed to spawn upgrades');
        }
    }

    async getUpgrade(roomId, upgradeId) {
        const key = this.getUpgradeKey(roomId, upgradeId);
        return await global.redisClient.hGetAll(key);
    }

    async handleUpgradePickup(ws, data) {
        if (!Utils.isValidMessage(data, ['player_id', 'room_id', 'upgrade_id'])) {
            return Utils.sendError(ws, 'Missing upgrade pickup fields');
        }

        const { player_id, room_id, upgrade_id } = data;

        try {
            const upgrade = await this.getUpgrade(room_id, upgrade_id);
            if (!upgrade || upgrade.is_taken === "true") {
                return Utils.sendError(ws, 'Upgrade not found or already taken');
            }

            const upgradeKey = this.getUpgradeKey(room_id, upgrade_id);
            const playerKey = this.getPlayerUpgradesKey(room_id, player_id);

            const pipeline = global.redisClient.multi();
            pipeline.hSet(upgradeKey, { is_taken: "true", owner_id: player_id });
            pipeline.sAdd(playerKey, upgradeKey);
            await pipeline.exec();

            const room = await roomManager.getRoomInfo(room_id);
            if (room) {
                roomManager.notifyRoomPlayers(room, {
                    action: 'upgrade_taken',
                    player_id,
                    upgrade_id,
                    upgrade_type: upgrade.type
                });
            }

            ws.send(JSON.stringify({
                action: 'upgrade_pickup_response',
                success: true,
                player_id,
                upgrade_id,
                upgrade_type: upgrade.type,
            }));

        } catch (err) {
            console.error('Error handling upgrade pickup:', err);
            Utils.sendError(ws, 'Failed to pickup upgrade');
        }
    }

    async handlePlayerDeathDropUpgrades(roomId, playerId) {
        try {
            const playerKey = this.getPlayerUpgradesKey(roomId, playerId);
            const upgradeKeys = await global.redisClient.sMembers(playerKey);

            if (!upgradeKeys || upgradeKeys.length === 0) return;

            const pipeline = global.redisClient.multi();

            for (const key of upgradeKeys) {
                const upgrade = await global.redisClient.hGetAll(key);
                if (!upgrade) continue;

                pipeline.hSet(key, { is_taken: "false", owner_id: "" });

                const room = await roomManager.getRoomInfo(roomId);
                if (room) {
                    roomManager.notifyRoomPlayers(room, {
                        action: 'upgrade_dropped',
                        player_id: playerId,
                        upgrade_id: upgrade.upgrade_id,
                        upgrade_type: upgrade.type,
                        position: { x: upgrade.p_x, y: upgrade.p_y, z: upgrade.p_z }
                    });
                }
            }

            pipeline.del(playerKey);
            await pipeline.exec();

            console.log(`[Upgrades] Player ${playerId} died and dropped ${upgradeKeys.length} upgrades`);
        } catch (err) {
            console.error('Error handling player death for upgrades:', err);
        }
    }

    async handleUpgradeDrop(ws, data) {
        // ждём: room_id, player_id, upgrade_id, p_x, p_y, p_z
        if (!Utils.isValidMessage(data, ['room_id', 'player_id', 'upgrade_id', 'p_x', 'p_y', 'p_z'])) {
            return Utils.sendError(ws, 'Missing upgrade drop fields');
        }

        const { room_id, player_id, upgrade_id } = data;

        // привести координаты к числам
        const x = Number(data.p_x), y = Number(data.p_y), z = Number(data.p_z);
        if ([x, y, z].some(v => Number.isNaN(v))) {
            return Utils.sendError(ws, 'Invalid upgrade position');
        }

        try {
            const upgradeKey = this.getUpgradeKey(room_id, upgrade_id);
            const playerKey  = this.getPlayerUpgradesKey(room_id, player_id);

            // проверить, что апгрейд есть
            const upgrade = await global.redisClient.hGetAll(upgradeKey);
            if (!upgrade || !upgrade.type) {
                return Utils.sendError(ws, 'Upgrade not found');
            }

            // записать позицию и освободить апгрейд
            const pipeline = global.redisClient.multi();
            pipeline.hSet(upgradeKey, {
                is_taken: "false",
                owner_id: "",
                p_x: x.toString(),
                p_y: y.toString(),
                p_z: z.toString(),
                timestamp: Date.now().toString()
            });
            // убрать привязку к игроку
            pipeline.sRem(playerKey, upgradeKey);
            await pipeline.exec();

            // оповестить комнату тем же событием, что и автодроп при смерти
            const room = await roomManager.getRoomInfo(room_id);
            if (room) {
                roomManager.notifyRoomPlayers(room, {
                    action: 'upgrade_dropped',
                    player_id,
                    upgrade_id,
                    upgrade_type: upgrade.type,
                    position: { x: x.toString(), y: y.toString(), z: z.toString() }
                });
            }

            // ответить инициатору
            ws.send(JSON.stringify({
                action: 'upgrade_drop_response',
                success: true,
                player_id,
                upgrade_id,
                upgrade_type: upgrade.type
            }));

            console.log(`[Upgrades] Player ${player_id} dropped upgrade ${upgrade_id} in room ${room_id} at (${x}, ${y}, ${z})`);
        } catch (err) {
            console.error('Error handling upgrade drop:', err);
            Utils.sendError(ws, 'Failed to drop upgrade');
        }
    }
}

module.exports = new UpgradeController();