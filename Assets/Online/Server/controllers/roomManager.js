// roomManager.js
const { GameConstants, Constants } = require('../config/constants');
const WebSocket = require('ws');
const { getPlayerFromRedis } = require('../services/playerRedisService');

class RoomManager {
    constructor() {
        this.roomTimers = new Map();        // таймеры для матчмейкинга
        this.connectedPlayers = new Map();  // playerId -> WebSocket
        this.validModes = [1, 2, 3];        // допустимые режимы
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
    // Инициализация всех комнат в Redis
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
                            matchId: ''
                        };
                        await global.redisClient.hSet(roomsKey, roomId, JSON.stringify(roomData));
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
            if (!roomRaw) return null;

            const roomData = JSON.parse(roomRaw);
            const players = await this.getRoomPlayers(roomId);

            let bots = [];
            // try {
            //     bots = roomData.bots ? JSON.parse(roomData.bots) : [];
            // } catch (e) {
            //     console.warn(`Failed to parse bots for room ${roomId}, using empty array`);
            //     bots = [];
            // }

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

    // Получение всех игроков комнаты
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
    // Получение комнаты по ID игрока
    getRoomByPlayerId(playerId) {
        for (const room of Object.values(this.rooms)) {
            if (room.playerIds?.includes(playerId)) { // если у комнаты есть список игроков
                return room;
            }
        }
        return null;
    }

    // === Добавление и удаление игроков ===
    async addPlayerToRoom(room, playerId, heroId) {
        // Проверяем, что игрок подключен
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

        // Сохраняем heroId
        const playerHeroKey = `${Constants.playerHeroKey}${room.id}:${playerId}`;
        pipeline.setEx(playerHeroKey, 86400, heroId.toString());

        // Если комната в WAITING - переводим в COUNTDOWN
        const roomDataRaw = await global.redisClient.hGet(`rooms:${room.mode}`, room.id);
        const roomData = roomDataRaw ? JSON.parse(roomDataRaw) : { ...room };
        if (roomData.state === GameConstants.ROOM_STATES.WAITING) {
            console.log(`Changing room ${room.id} from WAITING to COUNTDOWN`);
            pipeline.hSet(`rooms:${room.mode}`, room.id, JSON.stringify({
                ...roomData,
                state: GameConstants.ROOM_STATES.COUNTDOWN,
                startTime: Date.now()
            }));
        }

        await pipeline.exec();

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

        return updatedRoom;
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

    // === Поиск комнат ===
    async findAvailableRoom(mode) {
        const roomsKey = `rooms:${mode}`;
        const keys = await global.redisClient.hKeys(roomsKey);

        for (const state of [GameConstants.ROOM_STATES.COUNTDOWN, GameConstants.ROOM_STATES.WAITING]) {
            for (const roomId of keys) {
                const room = await this.getRoomInfo(roomId);
                if (room.state === state && room.playerCount < room.maxPlayers) return room;
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

    // === Таймеры матчмейкинга ===
    // 5 Секунд таймер(внизу можно изменить)
    startMatchmakingTimer(room) {
        if (this.roomTimers.has(room.id)) {
            clearTimeout(this.roomTimers.get(room.id));
        }

        const timer = setTimeout(async () => {
            console.log(`Matchmaking timer expired for room ${room.id}`);

            try {
                const currentPlayers = await this.getRoomPlayers(room.id);
                if (currentPlayers.length === 0) {
                    console.log(`No players joined room ${room.id}, resetting`);
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
                roomData.matchId = `match_${Date.now()}_${Math.random().toString(36).substr(2,9)}`;

                await global.redisClient.hSet(roomsKey, room.id, JSON.stringify(roomData));

                console.log(`Starting game for room ${room.id} with ${currentPlayers.length} players and ${bots.length} bots`);
                await this.startGame({ ...roomData, players: currentPlayers, bots });

            } catch (err) {
                console.error(`Error in matchmaking timer for room ${room.id}:`, err);
            } finally {
                this.roomTimers.delete(room.id);
            }

        }, 5000); // 5 Секунд таймер

        this.roomTimers.set(room.id, timer);
    }


    clearMatchmakingTimer(roomId) {
        if (this.roomTimers.has(roomId)) {
            clearTimeout(this.roomTimers.get(roomId));
            this.roomTimers.delete(roomId);
        }
    }

    // === Старт/Завершение игры ===
    async startGame(room) {
        this.clearMatchmakingTimer(room.id);

        const roomsKey = `rooms:${room.mode}`;
        const roomDataRaw = await global.redisClient.hGet(roomsKey, room.id);
        const roomData = roomDataRaw ? JSON.parse(roomDataRaw) : { ...room };

        roomData.state = GameConstants.ROOM_STATES.IN_PROGRESS;
        roomData.matchId = `match_${Date.now()}_${Math.random().toString(36).substr(2,9)}`;
        await global.redisClient.hSet(roomsKey, room.id, JSON.stringify(roomData));

        const allPlayers = await this.getPlayersWithInfo(room.players, room.id);

        // Для каждого игрока публикуем событие в Redis
        for (const player of allPlayers) {
            await this.notifyRoomPlayers(room, {
                action: 'match_start',
                room_id: room.id,
                match_id: roomData.matchId,
                players: allPlayers,
                bots: room.bots || []
            });
            // await this.publishToPlayer(player.playerId, {
            //     action: 'match_start',
            //     room_id: room.id,
            //     match_id: roomData.matchId,
            //     players: allPlayers,
            //     bots: room.bots || []
            // }, room.id);
        }

        console.log(`Game started in room ${room.id} with ${allPlayers.length} players`);
    }

    // =================== endGame ===================
    async endGame(room) {
        const roomsKey = `rooms:${room.mode}`;
        const roomDataRaw = await global.redisClient.hGet(roomsKey, room.id);
        const roomData = JSON.parse(roomDataRaw);

        // 1. Меняем состояние комнаты  
        roomData.state = GameConstants.ROOM_STATES.COMPLETED;
        await global.redisClient.hSet(roomsKey, room.id, JSON.stringify(roomData));

        // 2. Отправляем уведомление всем игрокам через адаптированный notifyRoomPlayers
        await this.notifyRoomPlayers(room, {
            action: 'match_end',
            room_id: room.id,
            match_id: roomData.matchId
        });

        console.log(`Game ended in room ${room.id}`);

        // 3. Через 5 секунд сбрасываем комнату
        // setTimeout(() => this.resetRoom(room), 5000);
        await this.resetRoom(room);
    }

    async resetRoom(room) {
        const roomsKey = `rooms:${room.mode}`;
        if (room.resetting) return; // предотвращаем дубли
        room.resetting = true;

        console.log(`Reset room ${room.id}`);

        const roomPlayersKey = `${Constants.roomPlayersKey}${room.id}`;
        const roomData = {
            id: room.id,
            mode: room.mode.toString(),
            maxPlayers: room.maxPlayers.toString(),
            state: GameConstants.ROOM_STATES.WAITING,
            players: '[]',
            bots: '[]',
            startTime: '0',
            matchId: ''
        };

        const pipeline = global.redisClient.multi();
        pipeline.hSet(roomsKey, room.id, JSON.stringify(roomData));
        pipeline.del(roomPlayersKey);
        await pipeline.exec();

        // Сбрасываем флаг после выполнения
        room.resetting = false;
    }

    // === Подключения игроков ===
    registerPlayerConnection(playerId, ws) { this.connectedPlayers.set(playerId, ws); }
    unregisterPlayerConnection(playerId) { this.connectedPlayers.delete(playerId); }
    getPlayerConnection(playerId) { return this.connectedPlayers.get(playerId); }
    isPlayerConnected(playerId) {
        const client = this.getPlayerConnection(playerId);
        return client && client.readyState === WebSocket.OPEN;
    }

    // === Отключение игроков ===
    async handlePlayerDisconnect(playerId) {
        console.log(`Handling disconnect for player ${playerId}`);

        // 1. Удаляем из комнаты
        const room = await this.removePlayerFromRoom(playerId);
        if (room) {
            console.log(`Player ${playerId} removed from room ${room.id}`);
            // Уведомляем других игроков
            await this.notifyRoomPlayers(room, {
                action: 'player_left',
                player_id: playerId
            });

            // 2. Проверяем, остались ли игроки
            const remainingPlayers = await this.getRoomPlayers(room.id);
            if (remainingPlayers.length === 0) {
                console.log(`No players left in room ${room.id}, resetting room immediately`);
                await this.endGame(room);
            }
        }

        // 4. Сохраняем данные игрока
        const playerData = await getPlayerFromRedis(playerId);

        return room;
    }

    // === Hero ID ===
    async getPlayerHeroId(roomId, playerId) {
        const key = `${Constants.playerHeroKey}${roomId}:${playerId}`;
        const heroId = await global.redisClient.get(key);
        return heroId ? parseInt(heroId) : 0;
    }

    async getAllPlayersHeroIds(roomId, playerIds) {
        const pipeline = global.redisClient.multi();
        playerIds.forEach(pid => pipeline.get(`${Constants.playerHeroKey}${roomId}:${pid}`));
        const results = await pipeline.exec();
        const heroIds = {};
        playerIds.forEach((pid, idx) => {
            const val = results[idx];
            heroIds[pid] = val ? parseInt(val) : null;
        });
        return heroIds;
    }

    async getPlayersWithInfo(playerIds, roomId) {
        const playersWithInfo = [];
        const heroIds = await this.getAllPlayersHeroIds(roomId, playerIds);
        for (const pid of playerIds) {
            const playerInfo = await getPlayerFromRedis(pid);
            if (!playerInfo) continue;
            playersWithInfo.push({
                playerId: pid,
                username: playerInfo.player_name || 'Unknown',
                rating: playerInfo.rating ?? -1,
                heroId: heroIds[pid] ?? 0
            });
        }
        return playersWithInfo;
    }

    // === Уведомление игроков ===
    async notifyRoomPlayers(room, message) {
        const players = await this.getRoomPlayers(room.id);

        for (const playerId of players) {
            const ws = this.getPlayerConnection(playerId);
            if (ws && ws.readyState === WebSocket.OPEN) {
                // Локальная отправка
                ws.send(JSON.stringify(message));
                console.log(`Sent ${message.action} to local player ${playerId}`);
            } else {
                // Публикуем событие в Redis, чтобы другие кластеры доставили игроку
                await global.redisClient.publish('websocket_messages', JSON.stringify({
                    playerId,
                    roomId: room.id,
                    data: message
                }));
                console.log(`Published ${message.action} to Redis for remote player ${playerId}`);
            }
        }
    }

    async publishToPlayer(playerId, data, roomId, heroId) {
        if (!global.redisClient) return;

        const message = { playerId, data, roomId };
        if (heroId !== undefined) message.heroId = heroId;  // добавляем только если есть

        await global.redisClient.publish('websocket_messages', JSON.stringify(message));
        console.log(`Published ${data.action} to Redis for player ${playerId}`);
    }
}

const roomManager = new RoomManager();

async function initializeRoomManager() {
    // 1. Инициализируем RoomManager (Redis, комнаты)
    await roomManager.initialize();

    // 2. Сбрасываем все комнаты при старте сервера
    await roomManager.resetAllRooms();

    return roomManager;
}

module.exports = { roomManager, initializeRoomManager };

// // roomManager.js
// const { GameConstants, Constants } = require('../config/constants');
// const WebSocket = require('ws');

// const { getPlayerFromRedis,

//         } = require('../services/utils');

// class RoomManager {
//     constructor() {
//         this.roomTimers = new Map();
//         this.validModes = [1, 2, 3];
//         this.connectedPlayers = new Map();
//         this.initializationPromise = null;
//         this.isRedisConnected = false;

//         console.log('RoomManager created, waiting for Redis connection...');    
//     }

//     // Метод для ручной инициализации после подключения Redis
//     async initialize() {
//         try {
//             console.log('Initializing RoomManager with Redis...');
            
//             if (!global.redisClient) {
//                 throw new Error('Redis client not available in global');
//             }
            
//             // Проверяем подключение к Redis
//             await global.redisClient.ping();
//             this.isRedisConnected = true;
//             console.log('Redis connection verified for RoomManager');
            
//             // Теперь инициализируем комнаты
//             await this.initializeRedisRooms();
            
//             console.log('RoomManager initialized successfully');
//             return true;
            
//         } catch (error) {
//             console.error('RoomManager initialization failed:', error);
//             // Повторная попытка через 5 секунд
//             setTimeout(() => this.initialize(), 5000);
//             return false;
//         }
//     }

//     async initializeRedisRooms() {
//         if (this.initializationPromise) {
//             return this.initializationPromise;
//         }

//         this.initializationPromise = (async () => {
//             if (!this.isRedisConnected) {
//                 throw new Error('Redis not connected');
//             }

//             console.log('Initializing Redis rooms...');
            
//             for (const mode of this.validModes) {
//                 for (let i = 1; i <= GameConstants.MAX_ROOMS_PER_MODE; i++) {
//                     const roomId = `${mode}-${i}`;
//                     // console.log(`Initializing room: ${roomId}`);
//                     await this.initRoomInRedis(roomId, mode);
//                     // Небольшая задержка между созданием комнат
//                     await new Promise(resolve => setTimeout(resolve, 1));
//                 }
//             }
//             console.log('All rooms initialized in Redis');
//         })();

//         return this.initializationPromise;
//     }

//     async initRoomInRedis(roomId, mode) {
//         try {
//             if (!global.redisClient || !this.isRedisConnected) {
//                 throw new Error('Redis client not available');
//             }

//             const roomsKey = `rooms:${mode}`; // Hash всех комнат для режима

//             // Проверяем, есть ли комната уже
//             const existingRoom = await global.redisClient.hGet(roomsKey, roomId);
//             if (existingRoom) {
//                 console.log(`Room ${roomId} for mode ${mode} already exists`);
//                 return;
//             }

//             // Данные комнаты
//             const roomData = {
//                 id: roomId,
//                 mode: mode.toString(),
//                 maxPlayers: GameConstants.MODE_CAPACITY[mode].toString(),
//                 state: GameConstants.ROOM_STATES.WAITING,
//                 players: [], // пустой массив
//                 bots: [],    // пустой массив
//                 startTime: 0,
//                 matchId: ''
//             };

//             // Добавляем комнату в hash
//             await global.redisClient.hSet(roomsKey, roomId, JSON.stringify(roomData));

//             console.log(`Room ${roomId} for mode ${mode} initialized successfully`);
//         } catch (error) {
//             console.error(`Error initializing room ${roomId}:`, error);
//             throw error;
//         }
//     }

//     // ДОБАВЬТЕ ЭТОТ МЕТОД ДЛЯ ПРОВЕРКИ ИНИЦИАЛИЗАЦИИ
//     async ensureRoomsInitialized() {
//         if (!this.isRedisConnected) {
//             throw new Error('Redis not connected');
//         }
        
//         if (!this.initializationPromise) {
//             await this.initializeRedisRooms();
//         }
//         return this.initializationPromise;
//     }

//     // Регистрация WebSocket соединения игрока
//     registerPlayerConnection(playerId, ws) {
//         if (!playerId) {
//             console.error("⚠️ Tried to register connection with EMPTY playerId!");
//             return;
//         }
//         this.connectedPlayers.set(playerId, ws);
//         console.log(`✅ Registered connection for player ${playerId}`);
//     }

//     // Удаление WebSocket соединения игрока
//     unregisterPlayerConnection(playerId) {
//         this.connectedPlayers.delete(playerId);
//         console.log(`Player ${playerId} connection unregistered`);
//     }

//     // Получение WebSocket соединения игрока
//     getPlayerConnection(playerId) {
//         return this.connectedPlayers.get(playerId);
//     }

//     // Поиск подходящей комнаты для игрока
//     async findAvailableRoom(mode, playerId, heroId) {
//         await this.ensureRoomsInitialized();
        
//         console.log(`=== START findAvailableRoom for mode ${mode} ===`);

//         // 1. ВЫСШИЙ ПРИОРИТЕТ: Самая заполненная комната в COUNTDOWN
//         let room = await this.findMostFilledRoom(mode);
        
//         // 2. Если нет заполненных, ищем любую COUNTDOWN комнату
//         if (!room) {
//             room = await this.findRoomWithSpace(mode, GameConstants.ROOM_STATES.COUNTDOWN);
//         }
        
//         // 3. Если нет COUNTDOWN, ищем WAITING комнату
//         if (!room) {
//             room = await this.findRoomWithSpace(mode, GameConstants.ROOM_STATES.WAITING);
//         }
        
//         // 4. Если все комнаты заняты - создаем новую
//         if (!room) {
//             room = await this.createNewRoom(mode);
//         }
        
//         if (room) {
//             console.log(`Selected room: ${room.id}, state: ${room.state}, players: ${room.playerCount}/${room.maxPlayers}`);
//             const updatedRoom = await this.addPlayerToRoom(room, playerId, heroId);
//             console.log(`=== END findAvailableRoom - SUCCESS ===`);
//             return updatedRoom;
//         }
        
//         console.log(`=== END findAvailableRoom - NO ROOMS ===`);
//         return null;
//     }

//     async findSuitableRoom(mode) {
//         const suitableStates = [
//             GameConstants.ROOM_STATES.WAITING,
//             GameConstants.ROOM_STATES.COUNTDOWN
//         ];
        
//         for (const state of suitableStates) {
//             const room = await this.findRoomWithSpace(mode, state);
//             if (room) {
//                 return room;
//             }
//         }
        
//         return null;
//     }

//     async findRoomWithSpace(mode, state) {
//         const rooms = [];
        
//         // Собираем все подходящие комнаты
//         for (let i = 1; i <= GameConstants.MAX_ROOMS_PER_MODE; i++) {
//             const roomId = `${mode}-${i}`;
//             const room = await this.getRoomInfo(roomId);
            
//             if (room && room.state === state && room.playerCount < room.maxPlayers) {
//                 rooms.push(room);
//             }
//         }
        
//         if (rooms.length === 0) {
//             return null;
//         }
        
//         // СОРТИРУЕМ ПО ПРИОРИТЕТУ:
//         if (state === GameConstants.ROOM_STATES.COUNTDOWN) {
//             // Для COUNTDOWN: сначала комнаты с наибольшим количеством игроков
//             rooms.sort((a, b) => b.playerCount - a.playerCount);
//         } else {
//             // Для WAITING: сначала самые старые комнаты
//             rooms.sort((a, b) => a.startTime - b.startTime);
//         }
        
//         const bestRoom = rooms[0];
//         console.log(`Best ${state} room: ${bestRoom.id}, players: ${bestRoom.playerCount}/${bestRoom.maxPlayers}`);
        
//         return bestRoom;
//     }

//     async findMostFilledRoom(mode) {
//         let bestRoom = null;
//         let maxPlayers = 0;
        
//         for (let i = 1; i <= GameConstants.MAX_ROOMS_PER_MODE; i++) {
//             const roomId = `${mode}-${i}`;
//             const room = await this.getRoomInfo(roomId);
            
//             if (room && 
//                 room.state === GameConstants.ROOM_STATES.COUNTDOWN && 
//                 room.playerCount < room.maxPlayers) {
                
//                 // Ищем комнату с максимальным количеством игроков
//                 if (room.playerCount > maxPlayers) {
//                     maxPlayers = room.playerCount;
//                     bestRoom = room;
//                 }
//             }
//         }
        
//         if (bestRoom) {
//             console.log(`Most filled room: ${bestRoom.id}, ${bestRoom.playerCount}/${bestRoom.maxPlayers} players`);
//         }
        
//         return bestRoom;
//     }

//     async createNewRoom(mode) {
//             // Ищем первую полностью свободную комнату в состоянии WAITING
//             for (let i = 1; i <= GameConstants.MAX_ROOMS_PER_MODE; i++) {
//                 const roomId = `${mode}-${i}`;
//                 const room = await this.getRoomInfo(roomId);
                
//                 if (room && 
//                     room.state === GameConstants.ROOM_STATES.WAITING && 
//                     room.playerCount === 0) {
//                     console.log(`Found empty WAITING room: ${roomId}`);
//                     return room;
//                 }
//             }

//         // Если все комнаты заняты - возвращаем null
//         console.log(`All rooms for mode ${mode} are occupied`);
//         return null;
//     }

//     async recoverStuckRooms() {
//         console.log('Checking for stuck rooms...');
        
//         for (const mode of this.validModes) {
//             for (let i = 1; i <= GameConstants.MAX_ROOMS_PER_MODE; i++) {
//                 const roomId = `${mode}-${i}`;
//                 const room = await this.getRoomInfo(roomId);
                
//                 if (room) {
//                     // Если комната в COUNTDOWN но пустая или таймер истек
//                     if (room.state === GameConstants.ROOM_STATES.COUNTDOWN) {
//                         const players = await this.getRoomPlayers(roomId);
//                         const timeInCountdown = Date.now() - room.startTime;
                        
//                         if (players.length === 0 || timeInCountdown > GameConstants.MATCHMAKING_TIME * 2) {
//                             console.log(`Resetting stuck COUNTDOWN room: ${roomId}`);
//                             await this.resetRoom(room);
//                         }
//                     }
                    
//                     // Если комната в IN_PROGRESS но пустая
//                     if (room.state === GameConstants.ROOM_STATES.IN_PROGRESS) {
//                         const players = await this.getRoomPlayers(roomId);
//                         if (players.length === 0) {
//                             console.log(`Resetting empty IN_PROGRESS room: ${roomId}`);
//                             await this.resetRoom(room);
//                         }
//                     }
//                 }
//             }
//         }
//     }

    

//     async findRoomByState(mode, state, checkSpace = true) {
//         console.log(`Searching for room mode: ${mode}, state: ${state}, checkSpace: ${checkSpace}`);
        
//         for (let i = 1; i <= GameConstants.MAX_ROOMS_PER_MODE; i++) {
//             const roomId = `${mode}-${i}`;
//             try {
//                 const room = await this.getRoomInfo(roomId);
                
//                 if (room && room.state === state) {
//                     console.log(`Found room ${roomId}: state=${room.state}, players=${room.playerCount}/${room.maxPlayers}`);
                    
//                     if (checkSpace) {
//                         if (room.playerCount < room.maxPlayers) {
//                             console.log(`Room ${roomId} has space: ${room.playerCount}/${room.maxPlayers}`);
//                             return room;
//                         } else {
//                             console.log(`Room ${roomId} is full: ${room.playerCount}/${room.maxPlayers}`);
//                         }
//                     } else {
//                         console.log(`Room ${roomId} found without space check`);
//                         return room;
//                     }
//                 } else if (room) {
//                     console.log(`Room ${roomId} state mismatch: expected ${state}, got ${room.state}`);
//                 } else {
//                     console.log(`Room ${roomId} not found or invalid`);
//                 }
//             } catch (error) {
//                 console.error(`Error checking room ${roomId}:`, error);
//             }
//         }
        
//         console.log(`No rooms found for mode ${mode}, state ${state}`);
//         return null;
//     }

//     async addPlayerToRoom(room, playerId, heroId) {
//         // ПРОВЕРЯЕМ ЧТО ИГРОК ВСЕ ЕЩЕ ПОДКЛЮЧЕН
//         const client = this.getPlayerConnection(playerId);
//         if (!client || client.readyState !== WebSocket.OPEN) {
//             console.log(`Player ${playerId} is not connected, skipping room join`);
//             return room;
//         }

//         console.log(`Adding player ${playerId} to room ${room.id}`);
            
//         const roomKey = `${Constants.roomKey}${room.id}`;
//         const roomPlayersKey = `${Constants.roomPlayersKey}${room.id}`;
        
//         // Проверяем, не в комнате ли уже игрок
//         const currentPlayers = await this.getRoomPlayers(room.id);
        
//         if (currentPlayers.includes(playerId)) {
//             console.log(`Player ${playerId} already in room ${room.id}`);
//             return await this.getRoomInfo(room.id);
//         }

//         // Добавляем игрока в Redis
//         const pipeline = global.redisClient.multi();
//         pipeline.sAdd(roomPlayersKey, playerId);
        
//         // Обновляем список игроков
//         const updatedPlayers = [...currentPlayers, playerId];
//         pipeline.hSet(roomKey, 'players', JSON.stringify(updatedPlayers));
        
//         // Сохраняем hero_id игрока
//         const playerHeroKey = `${Constants.playerHeroKey}${room.id}:${playerId}`;
//         pipeline.setEx(playerHeroKey, 86400, heroId.toString());
        
//         // Если комната в WAITING - переводим в COUNTDOWN
//         if (room.state === GameConstants.ROOM_STATES.WAITING) {
//             console.log(`Changing room ${room.id} from WAITING to COUNTDOWN`);
//             pipeline.hSet(roomKey, 'state', GameConstants.ROOM_STATES.COUNTDOWN);
//             pipeline.hSet(roomKey, 'startTime', Date.now().toString());
//         }
        
//         await pipeline.exec();

//         // Получаем обновленную информацию о комнате
//         const updatedRoom = await this.getRoomInfo(room.id);
//         console.log(`Room ${room.id} now has ${updatedRoom.playerCount} players, max: ${updatedRoom.maxPlayers}`);

//         // Если комната заполнена - начинаем игру НЕЗАВИСИМО от состояния
//         if (updatedRoom.playerCount === updatedRoom.maxPlayers) {
//             console.log(`Room ${room.id} is full, starting game immediately`);
//             if (this.roomTimers.has(room.id)) {
//                 clearTimeout(this.roomTimers.get(room.id));
//                 this.roomTimers.delete(room.id);
//             }
//             await this.startGame(updatedRoom);
//         }
//         // Если перевели в COUNTDOWN - запускаем таймер
//         else if (room.state === GameConstants.ROOM_STATES.WAITING) {
//             this.startMatchmakingTimer(updatedRoom);
//         }

//         return updatedRoom;
//     }

//     // Удаление игрока из комнаты
//     async removePlayerFromRoom(playerId) {
//         // Находим комнату игрока
//         const room = await this.getRoomByPlayerId(playerId);
//         if (!room) return null;

//         const roomKey = `${Constants.roomKey}${room.id}`;
//         const roomPlayersKey = `${Constants.roomPlayersKey}${room.id}`;
//         const playerHeroKey = `${Constants.playerHeroKey}${room.id}:${playerId}`;

//         // Удаляем игрока из Redis
//         const pipeline = global.redisClient.multi();
//         pipeline.sRem(roomPlayersKey, playerId);
//         pipeline.del(playerHeroKey); // удаляем hero_id
        
//         // Обновляем список игроков
//         const currentPlayers = await this.getRoomPlayers(room.id);
//         const updatedPlayers = currentPlayers.filter(id => id !== playerId);
//         pipeline.hSet(roomKey, 'players', JSON.stringify(updatedPlayers));
        
//         await pipeline.exec();

//         // Уведомляем всех о выходе игрока
//         this.notifyRoomPlayers(room, {
//             action: 'player_left_room',
//             player_id: playerId,
//             room_id: room.id,
//             players_remaining: updatedPlayers.length
//         });

//         // Если комната пустая - сбрасываем её
//         if (updatedPlayers.length === 0) {
//             await this.resetRoom(room);
//         }
        
//         return room;
//     }

//     // Принудительное удаление всех из комнаты
//     async forceResetRoom(roomId) {
//         const room = await this.getRoomInfo(roomId);
//         if (room) {
//             // Уведомляем всех игроков
//             this.notifyRoomPlayers(room, {
//                 action: 'room_force_closed',
//                 room_id: roomId,
//                 reason: 'admin_action'
//             });
            
//             await this.resetRoom(room);
//             return true;
//         }
//         return false;
//     }
    
//     // Получение комнаты по ID игрока
//     async getRoomByPlayerId(playerId) {
//         for (const mode of this.validModes) {
//             for (let i = 1; i <= GameConstants.MAX_ROOMS_PER_MODE; i++) {
//                 const roomId = `${mode}-${i}`;
//                 const players = await this.getRoomPlayers(roomId);
                
//                 if (players.includes(playerId)) {
//                     return await this.getRoomInfo(roomId);
//                 }
//             }
//         }
//         return null;
//     }

//     async getRoomPlayers(roomId) {
//         try {
//             if (!this.isRedisConnected) {
//                 console.error('Redis not connected');
//                 return [];  
//             }

//             const roomPlayersKey = `${Constants.roomPlayersKey}${roomId}`;
//             const players = await global.redisClient.sMembers(roomPlayersKey);
            
//             // console.log(`Players in room ${roomId}:`, players);
//             return players;
//         } catch (error) {
//             console.error(`Error getting players for room ${roomId}:`, error);
//             return [];
//         }
//     }

//     startMatchmakingTimer(room) {
//         const timer = setTimeout(async () => {
//             await this.handleMatchmakingTimeout(room);
//         }, GameConstants.MATCHMAKING_TIME);

//         this.roomTimers.set(room.id, timer);
//     }

//     async handleMatchmakingTimeout(room) {
//         const currentPlayers = await this.getRoomPlayers(room.id);
        
//         if (currentPlayers.length > 0) {
//             // Добавляем ботов для заполнения комнаты
//             const botsNeeded = room.maxPlayers - currentPlayers.length;
//             const bots = [];
            
//             for (let i = 0; i < botsNeeded; i++) {
//                 bots.push(`bot_${room.id}_${i}`);
//             }
            
//             // Сохраняем ботов в Redis
//             const roomKey = `${Constants.roomKey}${room.id}`;
//             await global.redisClient.hSet(roomKey, 'bots', JSON.stringify(bots));
            
//             await this.startGame({ ...room, players: currentPlayers, bots });
//         } else {
//             // Если никто не присоединился - сбрасываем комнату
//             await this.resetRoom(room);
//         }
//     }

//     async startGame(room) {
//         console.log('🔍 Constants.playerHeroKey:', Constants.playerHeroKey);
//         console.log('🔍 Room ID:', room.id);
//         console.log('🔍 Players:', room.players);
//         // Останавливаем таймер
//         if (this.roomTimers.has(room.id)) {
//             clearTimeout(this.roomTimers.get(room.id));
//             this.roomTimers.delete(room.id);
//         }

//         const roomKey = `${Constants.roomKey}${room.id}`;
//         const matchId = this.generateMatchId();
        
//         // Обновляем состояние комнаты в Redis
//         await global.redisClient.hSet(roomKey, {
//             state: GameConstants.ROOM_STATES.IN_PROGRESS,
//             matchId: matchId
//         });

//         // Получаем информацию об игроках С ГЕРОЯМИ
//         const playersWithInfo = await this.getPlayersWithInfo(room.players, room.id);

//         // Уведомляем ВСЕХ игроков о начале игры
//         await this.notifyRoomPlayers(room, {
//             action: 'match_start',
//             room_id: room.id,
//             match_id: matchId,
//             players: playersWithInfo,
//             bots: room.bots || []
//         });

//         console.log(`Game started in room ${room.id} with ${playersWithInfo.length} players`);
//     }

//     async endGame(room) {
//         const roomKey = `${Constants.roomKey}${room.id}`;
        
//         // Обновляем состояние комнаты
//         await global.redisClient.hSet(roomKey, 'state', GameConstants.ROOM_STATES.COMPLETED);
        
//         // Уведомляем игроков о завершении игры
//         this.notifyRoomPlayers(room, {
//             action: 'match_end',
//             room_id: room.id,
//             match_id: room.matchId
//         });

//         // Через короткое время сбрасываем комнату
//         setTimeout(async () => {
//             await this.resetRoom(room);
//         }, 5000);
//     }

//     async resetRoom(room) {
//         const roomKey = `${Constants.roomKey}${room.id}`;
//         const roomPlayersKey = `${Constants.roomPlayersKey}${room.id}`;
        
//         const pipeline = global.redisClient.multi();
//         pipeline.hSet(roomKey, {
//             state: GameConstants.ROOM_STATES.WAITING,
//             players: '[]',
//             bots: '[]',
//             startTime: '0',
//             matchId: ''
//         });
//         pipeline.del(roomPlayersKey);
        
//         await pipeline.exec();
        
//         console.log(`Room ${room.id} reset`);
//     }

//     generateMatchId() {
//         return `match_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
//     }
    
//     async getPlayerHeroId(roomId, playerId) {
//         try {
//             const heroKey = `${Constants.playerHeroKey}${roomId}:${playerId}`;
//             const heroId = await global.redisClient.get(heroKey);
//             return heroId ? parseInt(heroId) : 0; // возвращаем 0 если не найден
//         } catch (error) {
//             console.error(`Error getting hero id for player ${playerId} in room ${roomId}:`, error);
//             return 0;
//         }
//     }
//     async getAllPlayersHeroIds(roomId, playerIds) {
//         try {
//             console.log(`🔍 Getting heroIds for room ${roomId}, players: ${playerIds}`);
            
//             const pipeline = global.redisClient.multi();

//             for (const playerId of playerIds) {
//                 const heroKey = `player:hero:${roomId}:${playerId}`; // ← ПРЯМОЙ КЛЮЧ из логов
//                 console.log(`   Key: ${heroKey}`);
//                 pipeline.get(heroKey);
//             }

//             const results = await pipeline.exec();
//             console.log('🔍 Redis results:', results);

//             const heroIds = {};
//             playerIds.forEach((playerId, i) => {
//                 // ИСПРАВЛЕНИЕ: results[i] это значение, не массив
//                 const value = results[i]; 
//                 console.log(`   Player ${playerId} raw value:`, value, 'type:', typeof value);
                
//                 if (value !== null && value !== undefined && value !== '') {
//                     const numericValue = parseInt(value, 10);
//                     console.log(`   Player ${playerId} parsed value:`, numericValue);
                    
//                     if (!isNaN(numericValue)) {
//                         heroIds[playerId] = numericValue;
//                     } else {
//                         console.warn(`❌ Failed to parse heroId for ${playerId}: ${value}`);
//                         heroIds[playerId] = null;
//                     }
//                 } else {
//                     console.warn(`❌ No heroId found for ${playerId}`);
//                     heroIds[playerId] = null;
//                 }
//             });

//             console.log('🔍 Final heroIds:', heroIds);
//             return heroIds;

//         } catch (error) {
//             console.error('❌ Error in getAllPlayersHeroIds:', error);
//             return playerIds.reduce((acc, playerId) => {
//                 acc[playerId] = null;
//                 return acc;
//             }, {});
//         }
//     }
//     /**
//      * Отправка сообщения всем игрокам в комнате
//      * @param {Room} room - объект комнаты
//      * @param {Object} payload - данные для отправки
//      */

//     // Уведомление игроков комнаты
//     async notifyRoomPlayers(room, message) {
//         try {
//             const players = await this.getRoomPlayers(room.id);
//             console.log(`Notifying ${players.length} players in room ${room.id}:`, message.action);

//             for (const playerId of players) {
//                 const client = this.getPlayerConnection(playerId);

//                 if (client && client.readyState === WebSocket.OPEN) {
//                     // Локальный клиент
//                     client.send(JSON.stringify(message));
//                     console.log(`Sent ${message.action} to player ${playerId}`);
//                 } else {
//                     // Игрок подключен к другому воркеру или оффлайн
//                     await global.redisClient.publish(
//                         'websocket_messages',
//                         JSON.stringify({
//                             playerId,
//                             data: message,
//                             roomId: room.id,
//                             heroId: room.heroId
//                         })
//                     );
//                     console.log(`Published ${message.action} to Redis for player ${playerId} data - ${message.data}`);
//                 }
//             }
//         } catch (error) {
//             console.error(`Error notifying players in room ${room.id}:`, error);
//         }
//     }

//     // === НОВЫЕ МЕТОДЫ ДЛЯ ОБРАБОТКИ ЗАПРОСОВ ===

//     // Получение статистики матчмейкинга
//     async getMatchmakingStats(mode = null) {
//         const modesToCheck = mode ? [mode] : this.validModes;
//         const stats = {};

//         for (const m of modesToCheck) {
//             let waiting = 0, countdown = 0, inProgress = 0, completed = 0;
//             let totalPlayers = 0, totalBots = 0;

//             for (let i = 1; i <= GameConstants.MAX_ROOMS_PER_MODE; i++) {
//                 const roomId = `${m}-${i}`;
//                 const room = await this.getRoomInfo(roomId);
                
//                 if (room) {
//                     stats[room.state]++;
//                     totalPlayers += room.playerCount;
//                     totalBots += room.botCount;
//                 }
//             }

//             stats[m] = {
//                 totalRooms: GameConstants.MAX_ROOMS_PER_MODE,
//                 waiting,
//                 countdown,
//                 inProgress,
//                 completed,
//                 totalPlayers,
//                 totalBots
//             };
//         }

//         return mode ? stats[mode] : stats;
//     }

//     // Получение информации о комнате
//     async getRoomInfo(roomId) {
//         try {
//             const roomKey = `${Constants.roomKey}${roomId}`;
//             const roomData = await global.redisClient.hGetAll(roomKey);
            
//             if (!roomData || !roomData.id) {
//                 console.log(`Room ${roomId} not found in Redis`);
//                 return null;
//             }

//             // ВАЖНО: Получаем актуальный список игроков из Redis
//             const players = await this.getRoomPlayers(roomId);
//             const bots = JSON.parse(roomData.bots || '[]');
            
//             const roomInfo = {
//                 id: roomData.id,
//                 mode: parseInt(roomData.mode || '0'),
//                 maxPlayers: parseInt(roomData.maxPlayers || '0'),
//                 state: roomData.state || GameConstants.ROOM_STATES.WAITING,
//                 players: players,
//                 bots: bots,
//                 playerCount: players.length, // Используем актуальное количество
//                 botCount: bots.length,
//                 startTime: parseInt(roomData.startTime || '0'),
//                 matchId: roomData.matchId || ''
//             };
            
//             return roomInfo;
            
//         } catch (error) {
//             console.error(`Error getting room info for ${roomId}:`, error);
//             return null;
//         }
//     }

//     // Получение всех комнат игрока
//     async getPlayerRooms(playerId) {
//         const playerRooms = [];
        
//         for (const mode of this.validModes) {
//             for (let i = 1; i <= GameConstants.MAX_ROOMS_PER_MODE; i++) {
//                 const roomId = `${mode}-${i}`;
//                 const players = await this.getRoomPlayers(roomId);
                
//                 if (players.includes(playerId)) {
//                     const roomInfo = await this.getRoomInfo(roomId);
//                     if (roomInfo) {
//                         playerRooms.push({
//                             id: roomInfo.id,
//                             mode: roomInfo.mode,
//                             state: roomInfo.state,
//                             players: roomInfo.playerCount,
//                             bots: roomInfo.botCount,
//                             maxPlayers: roomInfo.maxPlayers
//                         });
//                     }
//                 }
//             }
//         }
        
//         return playerRooms;
//     }

//     async getPlayersWithInfo(playerIds, roomId) {
//         const playersWithInfo = [];
        
//         const heroIds = await this.getAllPlayersHeroIds(roomId, playerIds);
//         console.log('🔍 HeroIds from getAllPlayersHeroIds:', heroIds);

//         for (const playerId of playerIds) {
//             try {
//                 const playerInfo = await getPlayerFromRedis(playerId);
//                 console.log(`🔍 Player ${playerId} info:`, playerInfo);

//                 if (!playerInfo) {
//                     console.warn(`⚠️ Player ${playerId} not found in Redis`);
//                     continue;
//                 }

//                 const heroId = heroIds[playerId];
//                 console.log(`🔍 Player ${playerId} heroId:`, heroId, 'type:', typeof heroId);

//                 // ИСПРАВЛЕННАЯ ПРОВЕРКА - учитываем, что 0 это валидный ID
//                 if (heroId === null || heroId === undefined) {
//                     console.warn(`⚠️ HeroId not found for player ${playerId} in room ${roomId}`);
//                 } else {
//                     console.log(`✅ HeroId found for player ${playerId}: ${heroId}`);
//                 }

//                 playersWithInfo.push({
//                     playerId,
//                     username: playerInfo.username || 'Unknown',
//                     rating: playerInfo.rating ?? -1,
//                     heroId: heroId ?? 0,
//                     isReady: true,
//                     position: { x: 0, y: 0, z: 0 },
//                     rotation: { x: 0, y: 0, z: 0, w: 1 },
//                     animationState: "idle",
//                     isAlive: true,
//                     kills: 0,
//                     deaths: 0
//                 });

//                 console.log(`✅ Added player ${playerId} to list with heroId: ${heroId ?? 0}`);

//             } catch (error) {
//                 console.error(`❌ Error getting info for player ${playerId}:`, error);
//             }
//         }
        
//         console.log('🔍 Final playersWithInfo:', playersWithInfo);
//         return playersWithInfo;
//     }
//     async debugHeroKeys(roomId, playerIds) {
//         console.log('🐛 DEBUG: Checking hero keys in Redis');
        
//         for (const playerId of playerIds) {
//             const possibleKeys = [
//                 `${Constants.playerHeroKey}${roomId}:${playerId}`,
//                 `hero:${roomId}:${playerId}`,
//                 `player:${playerId}:hero:${roomId}`,
//                 `room:${roomId}:player:${playerId}:hero`
//             ];
            
//             for (const key of possibleKeys) {
//                 const value = await global.redisClient.get(key);
//                 console.log(`   Key: ${key} -> Value: ${value}`);
//             }
//         }
//     }

//     // Принудительное завершение игры
//     async forceEndGame(roomId) {
//         const room = await this.getRoomInfo(roomId);
//         if (room && room.state === GameConstants.ROOM_STATES.IN_PROGRESS) {
//             await this.endGame(room);
            
//             this.notifyRoomPlayers(room, {
//                 action: 'game_force_ended',
//                 room_id: roomId,
//                 reason: 'admin_action'
//             });
            
//             return true;
//         }
//         return false;
//     }

//     // Получение статуса всех режимов
//     async getAllModesStatus() {
//         const status = {};
        
//         for (const mode of this.validModes) {
//             const stats = await this.getMatchmakingStats(mode);
//             status[mode] = {
//                 totalPlayers: stats.totalPlayers,
//                 availableRooms: stats.waiting + stats.countdown,
//                 inProgressGames: stats.inProgress,
//                 totalRooms: stats.totalRooms
//             };
//         }
        
//         return status;
//     }

//     // Обработка отключения игрока
//     async handlePlayerDisconnect(playerId) {
//         console.log(`Handling disconnect for player ${playerId}`);
        
//         // Удаляем из комнаты
//         const room = await this.removePlayerFromRoom(playerId);
//         if (room) {
//             console.log(`Player ${playerId} removed from room ${room.id}`);
//         }
        
//         // Удаляем соединение
//         this.unregisterPlayerConnection(playerId);
        
//         return room !== null;
//     }

//     // Проверка подключения игрока
//     isPlayerConnected(playerId) {
//         const client = this.getPlayerConnection(playerId);
//         return client && client.readyState === WebSocket.OPEN;
//     }

//     // Отправка сообщения конкретному игроку
//     sendToPlayer(playerId, message) {
//         const client = this.getPlayerConnection(playerId);
//         if (client && client.readyState === WebSocket.OPEN) {
//             client.send(JSON.stringify(message));
//             return true;
//         }
//         return false;
//     }
// }

// module.exports = RoomManager;

// // // Запускайте эту функцию периодически
// // setInterval(() => {
// //     this.recoverStuckRooms();
// // }, 30000); // Каждые 30 секунд