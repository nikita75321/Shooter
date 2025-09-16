const { Constants } = require('../config/constants');
const globalRedis = require('../services/redisClient'); // твой redis клиент

class UpgradeRedisService {
    constructor() {
        this.upgradeTTL = 60 * 60 * 2; // 2 часа, можно менять
    }

    getUpgradeKey(roomId, upgradeId) {
        return `${Constants.upgradeKey}${roomId}:${upgradeId}`;
    }

    getRoomUpgradesKey(roomId) {
        return `${Constants.roomUpgradesKey}${roomId}`;
    }

    /**
     * Добавляем апгрейд в комнату
     */
    async addUpgrade(roomId, upgradeData) {
        const key = this.getUpgradeKey(roomId, upgradeData.upgrade_id);

        const data = {
            upgrade_id: upgradeData.upgrade_id,
            type: upgradeData.type,
            p_x: upgradeData.p_x,
            p_y: upgradeData.p_y,
            p_z: upgradeData.p_z,
            is_taken: "false",
            owner_id: null
        };

        // Сохраняем в Redis
        await globalRedis.hmset(key, data);
        await globalRedis.expire(key, this.upgradeTTL);

        // Добавляем в список апгрейдов комнаты
        await globalRedis.sadd(this.getRoomUpgradesKey(roomId), upgradeData.upgrade_id);
    }

    /**
     * Получаем апгрейд
     */
    async getUpgrade(roomId, upgradeId) {
        const key = this.getUpgradeKey(roomId, upgradeId);
        const data = await globalRedis.hgetall(key);
        return data && Object.keys(data).length > 0 ? data : null;
    }

    /**
     * Отмечаем апгрейд как подобранный
     */
    async markUpgradeAsTaken(roomId, upgradeId, playerId) {
        const key = this.getUpgradeKey(roomId, upgradeId);
        await globalRedis.hmset(key, {
            is_taken: "true",
            owner_id: playerId
        });
    }

    /**
     * Отмечаем апгрейд как "выпавший" (после смерти игрока)
     */
    async markUpgradeAsDropped(roomId, upgradeId) {
        const key = this.getUpgradeKey(roomId, upgradeId);
        await globalRedis.hmset(key, {
            is_taken: "false",
            owner_id: null
        });
    }

    /**
     * Получаем все апгрейды комнаты
     */
    async getRoomUpgrades(roomId) {
        const ids = await globalRedis.smembers(this.getRoomUpgradesKey(roomId));
        const upgrades = [];
        for (const id of ids) {
            const upgrade = await this.getUpgrade(roomId, id);
            if (upgrade) upgrades.push(upgrade);
        }
        return upgrades;
    }

    /**
     * Удаляем апгрейд
     */
    async removeUpgrade(roomId, upgradeId) {
        const key = this.getUpgradeKey(roomId, upgradeId);
        await globalRedis.del(key);
        await globalRedis.srem(this.getRoomUpgradesKey(roomId), upgradeId);
    }

    /**
     * Чистим все апгрейды комнаты
     */
    async clearRoomUpgrades(roomId) {
        const ids = await globalRedis.smembers(this.getRoomUpgradesKey(roomId));
        for (const id of ids) {
            await this.removeUpgrade(roomId, id);
        }
        await globalRedis.del(this.getRoomUpgradesKey(roomId));
    }
}

module.exports = new UpgradeRedisService();