// roomManager.js
const { GameConstants, Constants } = require('../config/constants');
const WebSocket = require('ws');
const playerRedisService = require('../services/playerRedisService');
const playerInGameController = require('./playerInGameController');

class RoomManager {
    constructor() {
        this.roomTimers = new Map();
        this.matchTimers = new Map();
        this.connectedPlayers = new Map();
        this.validModes = [1, 2, 3];
        this.isRedisConnected = false;
        this.initializationPromise = null;
        console.log('RoomManager created, waiting for Redis connection...');
    }

    // === Инициализация ===
    async initialize() {
        try {
            if (!global.redisClient) throw new Error('Redis client not available');
            await global.redisClient.ping();
            this.isRedisConnected = true;
            console.log('Redis connected');

            await this.initializeRedisRooms();
            console.log('All rooms initialized');
            return true;
        } catch (err) {
            console.error('RoomManager initialization failed:', err);
            setTimeout(() => this.initialize(), 5000);
            return false;
        }
    }

    async resetAllRooms() {
        console.log('Resetting all rooms...');
        for (const mode of this.validModes) {
            const roomsKey = `rooms:${mode}`;
            const keys = await global.redisClient.hKeys(roomsKey);
            for (const roomId of keys) {
                const roomDataRaw = await global.redisClient.hGet(roomsKey, roomId);
                const roomData = roomDataRaw ? JSON.parse(roomDataRaw) : { maxPlayers: GameConstants.MODE_CAPACITY[mode] };
                await this.resetRoom({
                    id: roomId,
                    mode: mode,
                    maxPlayers: roomData.maxPlayers || GameConstants.MODE_CAPACITY[mode]
                });
            }
        }
        console.log('All rooms reset');
    }

    async initializeRedisRooms() {
        if (!this.isRedisConnected) throw new Error('Redis not connected');
        if (this.initializationPromise) return this.initializationPromise;

        this.initializationPromise = (async () => {
            for (const mode of this.validModes) {
                const roomsKey = `rooms:${mode}`;
                for (let i = 1; i <= GameConstants.MAX_ROOMS_PER_MODE; i++) {
                    const roomId = `${mode}-${i}`;
                    const existing = await global.redisClient.hGet(roomsKey, roomId);
                    if (!existing) {
                        const roomData = {
                            id: roomId,
                            mode: mode.toString(),
                            maxPlayers: GameConstants.MODE_CAPACITY[mode].toString(),
                            state: GameConstants.ROOM_STATES.WAITING,
                            players: '[]',
                            bots: '[]',
                            startTime: '0',
                            matchId: '',
                            matchStartTime: '0',
                            matchEndTime: '0'
                        };
                        await global.redisClient.hSet(roomsKey, roomId, JSON.stringify(roomData));
                    } else {
                        try {
                            const parsed = JSON.parse(existing);
                            let needsUpdate = false;
                            if (!('matchId' in parsed)) {
                                parsed.matchId = '';
                                needsUpdate = true;
                            }
                            if (!('matchStartTime' in parsed)) {
                                parsed.matchStartTime = '0';
                                needsUpdate = true;
                            }
                            if (!('matchEndTime' in parsed)) {
                                parsed.matchEndTime = '0';
                                needsUpdate = true;
                            }
                            if (needsUpdate) {
                                await global.redisClient.hSet(roomsKey, roomId, JSON.stringify(parsed));
                            }
                        } catch (err) {
                            console.error(`Failed to parse room data for ${roomId}:`, err);
                        }
                    }
                }
            }
        })();

        return this.initializationPromise;
    }

    // === Получение информации о комнате ===
    async getRoomInfo(roomId) {
        try {
            const [mode] = roomId.split('-').map(Number);
            const roomsKey = `rooms:${mode}`;
            const roomRaw = await global.redisClient.hGet(roomsKey, roomId);
            // console.log(`roomRaw ${roomRaw}`);
            
            if (!roomRaw) return null;

            const roomData = JSON.parse(roomRaw);
            const players = await this.getRoomPlayers(roomId);
            // const players = await this.getAllPlayersHeroInfo();

            let bots = [];
            return {
                ...roomData,
                mode,
                players,
                bots,
                playerCount: players.length,
                botCount: bots.length,
                startTime: parseInt(roomData.startTime || '0')
            };
        } catch (err) {
            console.error(`Error getting room info for ${roomId}:`, err);
            return null;
        }
    }

    async getRoomPlayers(roomId) {
        try {
            const key = `${Constants.roomPlayersKey}${roomId}`;
            const players = await global.redisClient.sMembers(key);
            return players || [];
        } catch (err) {
            console.error(`Error fetching players for room ${roomId}:`, err);
            return [];
        }
    }

    getRoomByPlayerId(playerId) {
        for (const room of Object.values(this.rooms || {})) {
            if (room.playerIds?.includes(playerId)) {
                return room;
            }
        }
        return null;
    }

    // === Добавление игрока ===
    async addPlayerToRoom(room, playerId, heroId, heroSkin, heroLevel, heroRank) {
        const client = this.getPlayerConnection(playerId);
        if (!client || client.readyState !== WebSocket.OPEN) {
            console.log(`Player ${playerId} is not connected, skipping room join`);
            return room;
        }

        console.log(`Adding player ${playerId} to room ${room.id}`);

        const roomPlayersKey = `${Constants.roomPlayersKey}${room.id}`;
        const roomKey = `${Constants.roomKey}${room.id}`;

        // Проверяем, не в комнате ли уже игрок
        const currentPlayers = await this.getRoomPlayers(room.id);
        if (currentPlayers.includes(playerId)) {
            console.log(`Player ${playerId} already in room ${room.id}`);
            return await this.getRoomInfo(room.id);
        }

        // Добавляем игрока в Redis
        const pipeline = global.redisClient.multi();
        pipeline.sAdd(roomPlayersKey, playerId);
        const updatedPlayers = [...currentPlayers, playerId];
        pipeline.hSet(roomKey, 'players', JSON.stringify(updatedPlayers));

        // Получаем текущие данные комнаты
        const roomDataRaw = await global.redisClient.hGet(`rooms:${room.mode}`, room.id);
        const roomData = roomDataRaw ? JSON.parse(roomDataRaw) : { ...room };

        // Если первый игрок, переводим комнату в COUNTDOWN
        if (roomData.state === GameConstants.ROOM_STATES.WAITING) {
            console.log(`Changing room ${room.id} from WAITING to COUNTDOWN`);
            pipeline.hSet(`rooms:${room.mode}`, room.id, JSON.stringify({
                ...roomData,
                state: GameConstants.ROOM_STATES.COUNTDOWN,
                startTime: Date.now()
            }));
        }

        // Сохраняем героя игрока для матча
        const heroKey = `player:hero:${room.id}:${playerId}`;
        pipeline.hSet(heroKey, {
            hero_id: heroId,
            skin_id: heroSkin,
            level: heroLevel,
            rank: heroRank
        });
        pipeline.expire(heroKey, 60 * 10); // TTL 10 минут

        await pipeline.exec();

        // ⚡️ Инициализация боевых статов игрока
        await this.initPlayerStats(room.id, playerId, heroId, heroLevel, heroRank);

        const updatedRoom = await this.getRoomInfo(room.id);
        console.log(`Room ${room.id} now has ${updatedRoom.playerCount} players, max: ${updatedRoom.maxPlayers}`);

        // Если комната заполнена - начинаем игру сразу
        if (updatedRoom.playerCount === updatedRoom.maxPlayers) {
            console.log(`Room ${room.id} is full, starting game immediately`);
            if (this.roomTimers.has(room.id)) {
                clearTimeout(this.roomTimers.get(room.id));
                this.roomTimers.delete(room.id);
            }
            await this.startGame(updatedRoom);
        } else if (roomData.state === GameConstants.ROOM_STATES.WAITING) {
            // Запускаем таймер COUNTDOWN
            this.startMatchmakingTimer(updatedRoom);
        }

        // Отправка события каждому игроку в комнате
        for (const pId of updatedPlayers) {
            this.publishToPlayer(pId, {
                action: 'room_update',
                room_id: updatedRoom.id,
                players_in_room: updatedRoom.playerCount,
                max_players: updatedRoom.maxPlayers
            });
        }

        return updatedRoom;
    }
    async initPlayerStats(roomId, playerId, heroId, level, rank) {
        const heroKey = `hero:${heroId}`;
        const hero = await global.redisClient.hGetAll(heroKey);

        if (!hero || !hero.max_hp) throw new Error(`Hero data not found: ${heroId}`);

        let maxHp = parseFloat(hero.max_hp);
        let maxArmor = parseFloat(hero.armor);
        let damage = parseFloat(hero.damage);
        let vision = parseFloat(hero.vision);

        // Применяем формулы ранга и уровня
        maxHp *= Math.pow(1.15, rank) * Math.pow(1.05, level);
        maxArmor *= Math.pow(1.15, rank) * Math.pow(1.05, level);
        damage *= Math.pow(1.15, rank) * Math.pow(1.05, level);

        const statsKey = `player_stats:${roomId}:${playerId}`;
        await global.redisClient.hSet(statsKey, {
            hp: maxHp,          // текущие HP
            max_hp: maxHp,      // макс HP
            armor: 0,           // текущая броня
            max_armor: maxArmor,// макс броня
            damage,
            vision,
            deaths: 0,
            kills: 0,
            respawn_time: 0
        });

        console.log(`✅ Stats initialized for player ${playerId} in room ${roomId}`);
    }

    async removePlayerFromRoom(playerId) {
        for (const mode of this.validModes) {
            const roomsKey = `rooms:${mode}`;
            const keys = await global.redisClient.hKeys(roomsKey);
            for (const roomId of keys) {
                const players = await this.getRoomPlayers(roomId);
                if (!players.includes(playerId)) continue;

                const roomPlayersKey = `${Constants.roomPlayersKey}${roomId}`;
                const pipeline = global.redisClient.multi();
                pipeline.sRem(roomPlayersKey, playerId);
                pipeline.del(`${Constants.playerHeroKey}${roomId}:${playerId}`);

                const updatedPlayers = players.filter(id => id !== playerId);
                const roomDataRaw = await global.redisClient.hGet(roomsKey, roomId);
                const roomData = JSON.parse(roomDataRaw);
                roomData.players = updatedPlayers;

                pipeline.hSet(roomsKey, roomId, JSON.stringify(roomData));
                await pipeline.exec();

                return await this.getRoomInfo(roomId);
            }
        }
        return null;
    }

    async findAvailableRoom(mode) {
        const roomsKey = `rooms:${mode}`;
        const keys = await global.redisClient.hKeys(roomsKey);
        for (const state of [GameConstants.ROOM_STATES.COUNTDOWN, GameConstants.ROOM_STATES.WAITING]) {
            for (const roomId of keys) {
                const room = await this.getRoomInfo(roomId);
                if (room.state === state && room.playerCount < Number(room.maxPlayers)) return room;
            }
        }
        return null;
    }

    async findRoomByPlayer(playerId) {
        for (const mode of this.validModes) {
            const roomsKey = `rooms:${mode}`;
            const keys = await global.redisClient.hKeys(roomsKey);
            for (const roomId of keys) {
                const players = await this.getRoomPlayers(roomId);
                if (players.includes(playerId)) return await this.getRoomInfo(roomId);
            }
        }
        return null;
    }

    // === Таймер матчмейкинга ===
    startMatchmakingTimer(room) {
        // if (this.roomTimers.has(room.id)) clearTimeout(this.roomTimers.get(room.id));
        if (this.roomTimers.has(room.id)) return;

        const timer = setTimeout(async () => {
            try {
                const currentPlayers = await this.getRoomPlayers(room.id);
                if (currentPlayers.length === 0) {
                    await this.resetRoom(room);
                    return;
                }

                const botsNeeded = room.maxPlayers - currentPlayers.length;
                const bots = [];
                for (let i = 0; i < botsNeeded; i++) bots.push(`bot_${room.id}_${i}`);

                const roomsKey = `rooms:${room.mode}`;
                const roomDataRaw = await global.redisClient.hGet(roomsKey, room.id);
                const roomData = roomDataRaw ? JSON.parse(roomDataRaw) : { ...room };
                roomData.bots = bots;
                roomData.state = GameConstants.ROOM_STATES.IN_PROGRESS;
                await global.redisClient.hSet(roomsKey, room.id, JSON.stringify(roomData));

                await this.startGame({ ...roomData, players: currentPlayers, bots });
            } catch (err) {
                console.error(`Error in matchmaking timer for room ${room.id}:`, err);
            } finally {
                this.roomTimers.delete(room.id);
            }
        }, GameConstants.MATCHMAKING_TIME);

        this.roomTimers.set(room.id, timer);
    }

    clearMatchmakingTimer(roomId) {
        if (this.roomTimers.has(roomId)) {
            clearTimeout(this.roomTimers.get(roomId));
            this.roomTimers.delete(roomId);
        }
    }

    startMatchTimer(room) {
        if (!room || !room.id) return;

        this.clearMatchTimer(room.id);

        const now = Date.now();
        const parseTime = (value, fallback) => {
            const parsed = parseInt(value, 10);
            return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
        };

        const matchStartTime = parseTime(room.matchStartTime, now);
        const defaultEnd = matchStartTime + GameConstants.MATCH_DURATION_MS;
        const matchEndTime = parseTime(room.matchEndTime, defaultEnd);
        const delay = Math.max(0, matchEndTime - now);

        const timer = setTimeout(async () => {
            try {
                const currentRoom = await this.getRoomInfo(room.id);
                if (!currentRoom) return;
                await this.endGame(currentRoom);
            } catch (err) {
                console.error(`Error ending match for room ${room.id}:`, err);
            } finally {
                this.matchTimers.delete(room.id);
            }
        }, delay);

        this.matchTimers.set(room.id, timer);
    }

    clearMatchTimer(roomId) {
        if (this.matchTimers.has(roomId)) {
            clearTimeout(this.matchTimers.get(roomId));
            this.matchTimers.delete(roomId);
        }
    }

    // === Старт/Завершение игры ===
    async startGame(room) {
        this.clearMatchmakingTimer(room.id);
        this.clearMatchTimer(room.id);

        const roomsKey = `rooms:${room.mode}`;
        const roomDataRaw = await global.redisClient.hGet(roomsKey, room.id);
        const roomData = roomDataRaw ? JSON.parse(roomDataRaw) : { ...room };

        roomData.state = GameConstants.ROOM_STATES.IN_PROGRESS;
        const matchStartTimestamp = Date.now();
        roomData.matchId = `match_${matchStartTimestamp}_${Math.random().toString(36).substr(2,9)}`;
        roomData.matchStartTime = matchStartTimestamp.toString();
        roomData.matchEndTime = (matchStartTimestamp + GameConstants.MATCH_DURATION_MS).toString();
        await global.redisClient.hSet(roomsKey, room.id, JSON.stringify(roomData));

        const realPlayers = room.players || [];
        const allPlayersInfo = await this.getPlayersWithInfo(realPlayers, room.id);
        
        // Формируем payload в старом формате
        const matchStartPayload = {
            action: 'match_start',
            room_id: room.id,
            match_id: roomData.matchId,
            players: allPlayersInfo.map(p => ({
                playerId: p.playerId,
                player_name: p.player_name,
                rating: p.rating,
                hero_id: p.hero_id,
                hero_skin: p.hero_skin,
                hero_level: p.hero_level,
                hero_rank: p.hero_rank,
                isReady: p.isReady || false,
                isAlive: p.isAlive || true,
                kills: p.kills || 0,
                deaths: p.deaths || 0,
                hp: p.hp,
                armor: p.armor,
                max_hp: p.max_hp,
                max_armor: p.max_armor
            })),
            bots: room.bots || []
        };

        for (const player of allPlayersInfo) {
            await this.publishToPlayer(player.playerId, matchStartPayload, room.id);
        }

        console.log(`Game started in room ${room.id} with ${allPlayersInfo.length} players and ${room.bots?.length || 0} bots`);

        this.startMatchTimer({
            ...room,
            matchId: roomData.matchId,
            matchStartTime: roomData.matchStartTime,
            matchEndTime: roomData.matchEndTime
        });
    }

    async endGame(room, options = {}) {
        const { force = false } = options;
        this.clearMatchTimer(room.id);

        const roomsKey = `rooms:${room.mode}`;
        const roomDataRaw = await global.redisClient.hGet(roomsKey, room.id);
        const roomData = roomDataRaw ? JSON.parse(roomDataRaw) : { ...room };

        if (roomData.state === GameConstants.ROOM_STATES.COMPLETED && !force) {
            return;
        }

        if (!force) {
            const matchEndTime = parseInt(roomData.matchEndTime || '0', 10);
            const now = Date.now();
            if (matchEndTime > now) {
                const remaining = matchEndTime - now;
                console.log(`Match for room ${room.id} requested to end early, rescheduling in ${remaining} ms`);
                this.startMatchTimer({
                    ...room,
                    matchId: roomData.matchId,
                    matchStartTime: roomData.matchStartTime,
                    matchEndTime: roomData.matchEndTime
                });
                return;
            }
        }

        roomData.state = GameConstants.ROOM_STATES.COMPLETED;
        roomData.matchStartTime = '0';
        roomData.matchEndTime = '0';
        await global.redisClient.hSet(roomsKey, room.id, JSON.stringify(roomData));

        let matchResults = [];
        try {
            const stats = await playerInGameController.getRoomStats(room.id);
            const toInt = (value) => {
                const parsed = parseInt(value, 10);
                return Number.isFinite(parsed) ? parsed : 0;
            };

            const toFloat = (value) => {
                const parsed = parseFloat(value);
                return Number.isFinite(parsed) ? parsed : 0;
            };

        matchResults = (stats || [])
            .sort((a, b) => {
                const killsA = toInt(a.kills);
                const killsB = toInt(b.kills);
                if (killsA !== killsB) return killsB - killsA; // больше киллов — выше

                const deathsA = toInt(a.deaths);
                const deathsB = toInt(b.deaths);
                if (deathsA !== deathsB) return deathsA - deathsB; // меньше смертей — выше

                const damageA = toFloat(a.damage);
                const damageB = toFloat(b.damage);
                return damageB - damageA; // больше урона — выше
            })
            .map((stat, index) => ({
                player_id: stat.player_id,
                player_name: stat.player_name,
                kills: toInt(stat.kills),
                deaths: toInt(stat.deaths),
                damage: toFloat(stat.damage),
                score: toFloat(stat.score), // можно оставить, но он больше не влияет на место
                place: index + 1,
                is_winner: index < 3
            }));
        } catch (err) {
            console.error(`Failed to build match results for room ${room.id}:`, err);
        }

        const matchEndPayload = {
            action: 'match_end',
            room_id: room.id,
            match_id: roomData.matchId || room.matchId || null,
            results: matchResults
        };

        await this.notifyRoomPlayers(room, matchEndPayload);

        try {
            await playerInGameController.cleanupMatchData(room.id);
        } catch (err) {
            console.error(`Failed to cleanup match data for room ${room.id}:`, err);
        }

        // await this.resetRoom({
        //     id: room.id,
        //     mode: room.mode,
        //     maxPlayers: room.maxPlayers || GameConstants.MODE_CAPACITY[room.mode]
        // });
    }

    async resetRoom(room) {
        this.clearMatchTimer(room.id);
        const roomsKey = `rooms:${room.mode}`;
        if (room.resetting) return;
        room.resetting = true;

        const roomPlayersKey = `${Constants.roomPlayersKey}${room.id}`;
        const roomData = {
            id: room.id,
            mode: room.mode.toString(),
            maxPlayers: room.maxPlayers.toString(),
            state: GameConstants.ROOM_STATES.WAITING,
            players: '[]',
            bots: '[]',
            startTime: '0',
            matchId: '',
            matchStartTime: '0',
            matchEndTime: '0'
        };

        const pipeline = global.redisClient.multi();
        pipeline.hSet(roomsKey, room.id, JSON.stringify(roomData));
        pipeline.del(roomPlayersKey);
        await pipeline.exec();

        room.resetting = false;
    }

    // === Hero Info ===
    async getPlayerHeroInfo(roomId, playerId) {
        // Берём данные профиля игрока
        const profile = await global.redisClient.hGetAll(`player:${playerId}:profile`);
        let heroData = { hero_id: 0, hero_skin: 0, hero_level: 1, hero_rank: 1 };

        // Если есть ключ героя для комнаты, берём hero_id и skin
        const heroKey = `player:hero:${roomId}:${playerId}`;
        const heroRedis = await global.redisClient.hGetAll(heroKey);
        if (heroRedis) {
            heroData.hero_id = parseInt(heroRedis.hero_id || 0);
            heroData.hero_skin = parseInt(heroRedis.skin_id || 0);
        }

        // Подставляем level и rank из профиля
        if (profile && profile.hero_levels) {
            try {
                const heroLevels = JSON.parse(profile.hero_levels);
                const heroIndex = heroData.hero_id > 0 ? heroData.hero_id - 1 : 0;
                const levelInfo = heroLevels[heroIndex] || { level: 1, rank: 1 };
                heroData.hero_level = levelInfo.level || 1;
                heroData.hero_rank = levelInfo.rank || 1;
            } catch (err) {
                console.warn(`Failed to parse hero_levels for player ${playerId}`, err);
            }
        }

        return heroData;
    }

    // Данные всех игроков
    async getAllPlayersHeroInfo(playerIds, roomId) {
        const heroMap = {};

        for (const pid of playerIds) {
            heroMap[pid] = await this.getPlayerHeroInfo(roomId, pid);
        }
        console.log(heroMap);
        
        return heroMap;
    }

    async getPlayersWithInfo(playerIds, roomId) {
        if (!playerIds || playerIds.length === 0) return [];

        const playersWithInfo = [];
        const heroDataMap = await this.getAllPlayersHeroInfo(playerIds, roomId);

        for (const pid of playerIds) {
            const playerInfo = await playerRedisService.getPlayerFromRedis(pid);
            if (!playerInfo) continue;

            const playerStats = await playerInGameController.getPlayerStats(pid, roomId);
            if (!playerStats) continue;
            console.log(`✅[playerStats] - `, playerStats);

            const heroData = heroDataMap[pid] || {};

            playersWithInfo.push({
                playerId: pid,
                player_name: playerInfo.player_name || 'Unknown',
                rating: playerInfo.rating ?? -1,
                hero_id: heroData.hero_id || 0,
                hero_skin: heroData.hero_skin || 0,
                hero_level: heroData.hero_level || 1,
                hero_rank: heroData.hero_rank || 1,
                hp: playerStats.hp,
                armor: playerStats.armor,
                max_hp: playerStats.max_hp,
                max_armor: playerStats.max_armor,
            });
        }

        return playersWithInfo;
    }

    // === Уведомления игроков ===
    async notifyRoomPlayers(room, message) {
        const players = await this.getRoomPlayers(room.id);

        for (const playerId of players) {
            const ws = this.getPlayerConnection(playerId);
            if (ws && ws.readyState === WebSocket.OPEN) {
                ws.send(JSON.stringify(message));
                console.log(`Sent ${message.action} to local player ${playerId}`);
            } else {
                await global.redisClient.publish('websocket_messages', JSON.stringify({
                    playerId,
                    roomId: room.id,
                    data: message
                }));
                console.log(`Published ${message.action} to Redis for remote player ${playerId}`);
            }
        }
    }

    async publishToPlayer(playerId, data, roomId) {
        if (!global.redisClient) return;

        const message = { playerId, data, roomId };

        await global.redisClient.publish('websocket_messages', JSON.stringify(message));
        console.log(`Published ${data.action} to Redis for player ${playerId}`);
    }

    // === Отключение игроков ===
    async handlePlayerDisconnect(playerId) {
        console.log(`Handling disconnect for player ${playerId}`);

        const room = await this.removePlayerFromRoom(playerId);
        if (room) {
            console.log(`Player ${playerId} removed from room ${room.id}`);
            await this.notifyRoomPlayers(room, {
                action: 'player_left',
                player_id: playerId
            });

            const remainingPlayers = await this.getRoomPlayers(room.id);
            if (remainingPlayers.length === 0) {
                console.log(`No players left in room ${room.id}, resetting room immediately`);
                await this.endGame(room, { force: true });
            }
        }

        await playerRedisService.getPlayerFromRedis(playerId); // сохранение/подгрузка, если нужно
        return room;
    }

    // === Подключения игроков ===
    registerPlayerConnection(playerId, ws) { this.connectedPlayers.set(playerId, ws); }
    unregisterPlayerConnection(playerId) { this.connectedPlayers.delete(playerId); }
    getPlayerConnection(playerId) { return this.connectedPlayers.get(playerId); }
    isPlayerConnected(playerId) {
        const client = this.getPlayerConnection(playerId);
        return client && client.readyState === WebSocket.OPEN;
    }
}

const roomManager = new RoomManager();

async function initializeRoomManager() {
    await roomManager.initialize();
    await roomManager.resetAllRooms();
    return roomManager;
}

module.exports = { roomManager, initializeRoomManager };