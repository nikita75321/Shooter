// roomManager.js
const { GameConstants, Constants } = require('../config/constants');

const BOT_CONFIG = {
    MIN_HERO_ID: 0,
    MAX_HERO_ID: 7,
    DEFAULT_HERO_SKIN_COUNT: 9,
    HERO_WEIGHT_DECAY: 0.7
};
const WebSocket = require('ws');
const playerRedisService = require('../services/playerRedisService');
const playerInGameController = require('./playerInGameController');

const toInt = (value, fallback = 0) => {
    const parsed = parseInt(value, 10);
    return Number.isFinite(parsed) ? parsed : fallback;
};

class RoomManager {
    constructor() {
        this.roomTimers = new Map();
        this.matchTimers = new Map();
        this.connectedPlayers = new Map();
        this.validModes = [1, 2, 3];
        this.isRedisConnected = false;
        this.initializationPromise = null;
        this.botHeroLottery = null;
        this.botHeroSkinCounts = {};
        console.log('RoomManager created, waiting for Redis connection...');
    }

    cloneSpawnPoint(point) {
        if (!point) {
            return {
                position: { x: 0, y: 0, z: 0 },
                rotation: { x: 0, y: 0, z: 0, w: 1 }
            };
        }

        return {
            position: {
                x: Number(point.position?.x ?? 0),
                y: Number(point.position?.y ?? 0),
                z: Number(point.position?.z ?? 0)
            },
            rotation: {
                x: Number(point.rotation?.x ?? 0),
                y: Number(point.rotation?.y ?? 0),
                z: Number(point.rotation?.z ?? 0),
                w: Number(point.rotation?.w ?? 1)
            }
        };
    }

    shuffleArray(array) {
        for (let i = array.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [array[i], array[j]] = [array[j], array[i]];
        }
        return array;
    }

    allocateSpawnPoints(count) {
        const configuredPoints = Array.isArray(GameConstants.SPAWN_POINTS)
            ? GameConstants.SPAWN_POINTS
            : [];

        if (!count || count <= 0) {
            return [];
        }

        if (configuredPoints.length === 0) {
            return Array.from({ length: count }, () => this.cloneSpawnPoint());
        }

        const result = [];
        let available = this.shuffleArray(
            configuredPoints.map(point => this.cloneSpawnPoint(point))
        );

        while (result.length < count) {
            if (available.length === 0) {
                available = this.shuffleArray(
                    configuredPoints.map(point => this.cloneSpawnPoint(point))
                );
            }

            const nextPoint = available.pop();
            result.push(this.cloneSpawnPoint(nextPoint));
        }

        return result;
    }

    buildBotHeroLottery() {
        const pool = [];
        const minId = BOT_CONFIG.MIN_HERO_ID;
        const maxId = BOT_CONFIG.MAX_HERO_ID;

        for (let heroId = minId; heroId <= maxId; heroId++) {
            const weight = Math.max(1, maxId - heroId + 1);
            for (let i = 0; i < weight; i++) {
                pool.push(heroId);
            }
        }

        return pool;
    }
    
    _ensureHeroWeights() {
        if (
            this._heroCumWeights &&
            this._heroIds &&
            this._heroIds.length === (BOT_CONFIG.MAX_HERO_ID - BOT_CONFIG.MIN_HERO_ID + 1)
        ) {
            return;
        }

        const decay = BOT_CONFIG.HERO_WEIGHT_DECAY;
        const minId = BOT_CONFIG.MIN_HERO_ID;
        const maxId = BOT_CONFIG.MAX_HERO_ID;

        const ids = [];
        const weights = [];

        for (let id = minId; id <= maxId; id++) {
            ids.push(id);
            // геометрическое затухание: 1, decay, decay^2, ...
            const w = Math.pow(decay, id - minId);
            weights.push(w);
        }

        // нормируем и делаем кумулятив
        const sum = weights.reduce((a, b) => a + b, 0);
        let acc = 0;
        const cum = weights.map(w => (acc += w / sum));

        this._heroIds = ids;
        this._heroCumWeights = cum; // последняя ≈ 1
    }

    getRandomBotHeroId() {
        this._ensureHeroWeights();
        const r = Math.random();
        const cum = this._heroCumWeights;
        const ids = this._heroIds;

        for (let i = 0; i < cum.length; i++) {
            if (r <= cum[i]) return ids[i];
        }
        return ids[ids.length - 1];
    }

    getRandomBotHeroSkin(heroId) {
        const count = BOT_CONFIG.DEFAULT_HERO_SKIN_COUNT;
        return Math.floor(Math.random() * count);
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

            let bots = [];
            if (Array.isArray(roomData.bots)) {
                bots = roomData.bots;
            } else if (typeof roomData.bots === 'string' && roomData.bots.length > 0) {
                try {
                    bots = JSON.parse(roomData.bots);
                } catch (err) {
                    console.warn(`Failed to parse bots list for room ${roomId}:`, err);
                    bots = [];
                }
            }

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
        pipeline.expire(heroKey, 60 * 5); // TTL 5 минут

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

        return {
            maxHp,
            maxArmor,
            damage,
            vision
        };
    }

    async generateBotsForRoom(room, botsNeeded, playersInfo) {
        if (!botsNeeded || botsNeeded <= 0) {
            return [];
        }

        const safePlayers = Array.isArray(playersInfo) ? playersInfo : [];

        // Ориентируемся на сильнейшего реального игрока в комнате
        const maxHeroLevel = safePlayers.reduce((max, player) => {
            return Math.max(max, toInt(player?.hero_level, 1));
        }, 1);

        const maxHeroRank = safePlayers.reduce((max, player) => {
            return Math.max(max, toInt(player?.hero_rank, 1));
        }, 1);

        const referencePlayer = safePlayers.reduce((best, current) => {
            if (!current) return best;
            if (!best) return current;

            const bestLevel = toInt(best.hero_level, 0);
            const currentLevel = toInt(current.hero_level, 0);
            if (currentLevel > bestLevel) return current;
            if (currentLevel < bestLevel) return best;

            const bestRank = toInt(best.hero_rank, 0);
            const currentRank = toInt(current.hero_rank, 0);
            if (currentRank > bestRank) return current;

            return best;
        }, null) || safePlayers[0] || null;

        const rating = toInt(referencePlayer?.rating, 0);

        const bots = [];

        for (let i = 0; i < botsNeeded; i++) {
            const botId = `bot_${room.id}_${i}`;

            // 🎲 Случайный герой и скин для КАЖДОГО бота (с возможными повторами)
            const heroId = this.getRandomBotHeroId();
            const heroSkin = this.getRandomBotHeroSkin(heroId);

            const heroLevel = maxHeroLevel > 0 ? maxHeroLevel : 1;
            const heroRank = maxHeroRank > 0 ? maxHeroRank : 1;

            let stats = { maxHp: 100, maxArmor: 0, damage: 0, vision: 0 };

            // Инициализируем боевые статы бота (hp/armor/damage/vision) на основе героя/уровня/ранга
            try {
                stats = await this.initPlayerStats(room.id, botId, heroId, heroLevel, heroRank) || stats;
            } catch (err) {
                console.error(`Failed to initialize stats for bot ${botId} in room ${room.id}:`, err);
            }

            // Сохраняем временную информацию о герое бота (как у игроков)
            try {
                const heroKey = `${Constants.playerHeroKey}${room.id}:${botId}`;
                await global.redisClient.hSet(heroKey, {
                    hero_id: heroId,
                    skin_id: heroSkin,
                    level: heroLevel,
                    rank: heroRank
                });
                await global.redisClient.expire(heroKey, 60 * 5); // 5 минут
            } catch (err) {
                console.error(`Failed to persist hero info for bot ${botId}:`, err);
            }

            bots.push({
                playerId: botId,
                player_name: `Bot ${i + 1}`,
                rating,
                hero_id: heroId,
                hero_skin: heroSkin,
                hero_level: heroLevel,
                hero_rank: heroRank,
                isReady: true,
                isAlive: true,
                kills: 0,
                deaths: 0,
                hp: stats.maxHp ?? 100,
                armor: 0,
                max_hp: stats.maxHp ?? 100,
                max_armor: stats.maxArmor ?? 0
            });
        }

        return bots;
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

                const roomsKey = `rooms:${room.mode}`;
                const roomDataRaw = await global.redisClient.hGet(roomsKey, room.id);
                const roomData = roomDataRaw ? JSON.parse(roomDataRaw) : { ...room };
                const maxPlayers = toInt(room.maxPlayers ?? roomData.maxPlayers, GameConstants.MODE_CAPACITY[room.mode] || currentPlayers.length);
                const botsNeeded = Math.max(0, maxPlayers - currentPlayers.length);
                const playersInfo = await this.getPlayersWithInfo(currentPlayers, room.id);
                const bots = await this.generateBotsForRoom({ ...roomData, ...room }, botsNeeded, playersInfo);
                roomData.bots = bots;
                roomData.botCount = bots.length;
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

        const realPlayers = Array.isArray(room.players) ? room.players : [];
        const allPlayersInfo = await this.getPlayersWithInfo(realPlayers, room.id);

        const modeKey = toInt(room.mode ?? roomData.mode, room.mode);
        const capacityFallback = GameConstants.MODE_CAPACITY[modeKey] || allPlayersInfo.length;
        const maxPlayers = toInt(room.maxPlayers ?? roomData.maxPlayers, capacityFallback);
        const botsNeeded = Math.max(0, maxPlayers - realPlayers.length);

        let botInfos = [];
        if (Array.isArray(room.bots) && room.bots.length > 0) {
            botInfos = room.bots;
        } else if (Array.isArray(roomData.bots) && roomData.bots.length > 0) {
            botInfos = roomData.bots;
        }

        const botsAreObjects = Array.isArray(botInfos) && botInfos.every(bot => bot && typeof bot === 'object');
        if (!botsAreObjects || botInfos.length !== botsNeeded) {
            botInfos = await this.generateBotsForRoom({ ...roomData, ...room }, botsNeeded, allPlayersInfo);
        }

        roomData.state = GameConstants.ROOM_STATES.IN_PROGRESS;
        const matchStartTimestamp = Date.now();
        roomData.matchId = `match_${matchStartTimestamp}_${Math.random().toString(36).substr(2,9)}`;
        roomData.matchStartTime = matchStartTimestamp.toString();
        roomData.matchEndTime = (matchStartTimestamp + GameConstants.MATCH_DURATION_MS).toString();
        
        // Формируем payload в старом формате
        const totalParticipants = allPlayersInfo.length + botInfos.length;
        const spawnAssignments = this.allocateSpawnPoints(totalParticipants);
        const playerSpawns = spawnAssignments.slice(0, allPlayersInfo.length);
        const botSpawns = spawnAssignments.slice(allPlayersInfo.length);

        const playersWithSpawns = allPlayersInfo.map((player, index) => ({
            ...player,
            position: this.cloneSpawnPoint(playerSpawns[index])?.position,
            rotation: this.cloneSpawnPoint(playerSpawns[index])?.rotation
        }));

        const botsWithSpawns = botInfos.map((bot, index) => ({
            ...bot,
            position: this.cloneSpawnPoint(botSpawns[index])?.position,
            rotation: this.cloneSpawnPoint(botSpawns[index])?.rotation
        }));

        const matchStartPayload = {
            action: 'match_start',
            room_id: room.id,
            match_id: roomData.matchId,
            players: playersWithSpawns.map(p => ({
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
                max_armor: p.max_armor,
                position: p.position || { x: 0, y: 0, z: 0 },
                rotation: p.rotation || { x: 0, y: 0, z: 0, w: 1 }
            })),
            bots: botsWithSpawns.map(bot => ({
                playerId: bot.playerId,
                player_name: bot.player_name,
                rating: bot.rating,
                hero_id: bot.hero_id,
                hero_skin: bot.hero_skin,
                hero_level: bot.hero_level,
                hero_rank: bot.hero_rank,
                isReady: bot.isReady ?? true,
                isAlive: bot.isAlive ?? true,
                kills: bot.kills ?? 0,
                deaths: bot.deaths ?? 0,
                hp: bot.hp ?? bot.max_hp ?? 0,
                armor: bot.armor ?? 0,
                max_hp: bot.max_hp ?? bot.hp ?? 0,
                max_armor: bot.max_armor ?? 0,
                position: bot.position || { x: 0, y: 0, z: 0 },
                rotation: bot.rotation || { x: 0, y: 0, z: 0, w: 1 }
            }))
        };

        roomData.bots = botsWithSpawns;
        roomData.botCount = botsWithSpawns.length;
        await global.redisClient.hSet(roomsKey, room.id, JSON.stringify(roomData));
        
        for (const player of allPlayersInfo) {
            await this.publishToPlayer(player.playerId, matchStartPayload, room.id);
        }

        console.log(`Game started in room ${room.id} with ${allPlayersInfo.length} players and ${botsWithSpawns.length} bots`);

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
            heroData.hero_level = parseInt(heroRedis.level || 0);
            heroData.hero_rank = parseInt(heroRedis.rank || 0);
        }

        // Подставляем level и rank из профиля
        // if (profile && profile.hero_levels) {
        //     try {
        //         const heroLevels = JSON.parse(profile.hero_levels);
        //         const heroIndex = heroData.hero_id > 0 ? heroData.hero_id - 1 : 0;
        //         const levelInfo = heroLevels[heroIndex] || { level: 1, rank: 1 };
        //         // heroData.hero_level = levelInfo.level || 1;
        //         // heroData.hero_rank = levelInfo.rank || 1;
        //     } catch (err) {
        //         console.warn(`Failed to parse hero_levels for player ${playerId}`, err);
        //     }
        // }

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