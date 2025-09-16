//BoostRedisService.js
const { GameConstants, Constants } = require('../config/constants');

class BoostRedisService {
    constructor() {
        this.boostTTL = 60 * 15; // 15 минут на всякий случай
    }

    getBoostKey(roomId, boostId) {
        return `${Constants.boostKey}${roomId}:${boostId}`;
    }

    getRoomBoostsKey(roomId) {
        return `${Constants.roomBoostsKey}${roomId}`;
    }

    async addBoost(roomId, boostData) {
        const boostKey = this.getBoostKey(roomId, boostData.boost_id);
        const roomBoostsKey = this.getRoomBoostsKey(roomId);

        const data = {
            boost_id: boostData.boost_id.toString(),
            type: boostData.type,
            p_x: boostData.p_x.toString(),
            p_y: boostData.p_y.toString(),
            p_z: boostData.p_z.toString(),
            is_taken: "false",
            timestamp: Date.now().toString()
        };

        const pipeline = global.redisClient.multi();
        pipeline.hSet(boostKey, data);
        pipeline.expire(boostKey, this.boostTTL);
        pipeline.sAdd(roomBoostsKey, boostKey);
        pipeline.expire(roomBoostsKey, this.boostTTL);
        await pipeline.exec();

        return data;
    }

    async getBoost(roomId, boostId) {
        const boostKey = this.getBoostKey(roomId, boostId);
        return await global.redisClient.hGetAll(boostKey);
    }

    async getAllBoosts(roomId) {
        const roomBoostsKey = this.getRoomBoostsKey(roomId);
        const boostKeys = await global.redisClient.sMembers(roomBoostsKey);

        if (!boostKeys.length) return [];

        const pipeline = global.redisClient.multi();
        boostKeys.forEach(key => pipeline.hGetAll(key));
        const results = await pipeline.exec();

        return results.map(r => r[1]);
    }

    async markBoostAsTaken(roomId, boostId) {
        const boostKey = this.getBoostKey(roomId, boostId);
        await global.redisClient.hSet(boostKey, "is_taken", "true");
    }

    async cleanupRoomBoosts(roomId) {
        const roomBoostsKey = this.getRoomBoostsKey(roomId);
        const boostKeys = await global.redisClient.sMembers(roomBoostsKey);

        const pipeline = global.redisClient.multi();
        boostKeys.forEach(key => pipeline.del(key));
        pipeline.del(roomBoostsKey);
        await pipeline.exec();
    }
}

module.exports = new BoostRedisService();