// //websocket.js
const WebSocket = require('ws');
const { v4: uuidv4 } = require('uuid');
const { GameConstants } = require('../config/constants');
const ConnectionController = require('../controllers/connectionController');

const playerInGameController = require('../controllers/playerInGameController');
const damageController = require('../controllers/damageController');

const { flushPlayerData } = require('./servicesflushService');
const playerController = require('../controllers/playerController');
const ClanController = require('../controllers/clanController');
const currencyController = require('../controllers/currencyController');
const statsController = require('../controllers/statsController');
const systemController = require('../controllers/systemController');
const heroController = require('../controllers/heroController');
const clanSync = require('../services/clanSyncService');
const playerRedisService = require('../services/playerRedisService')

const { roomManager, initializeRoomManager } = require('../controllers/roomManager');
const { formatDateTime, isValidMessage, sendError, handleBinaryMessage } = require('../services/utils');

const connectedPlayers = new Map();

const { Constants } = require('../config/constants');
const wsKey = Constants.wsKey;

// Инициализация RoomManager после подключения Redis
async function InitRoomManager() {
    try {
        const success = await initializeRoomManager();
        if (success) console.log('RoomManager initialized successfully in WebSocket');
    } catch (error) {
        console.error('Failed to initialize RoomManager:', error);
    }
}

function setupWebSocketServer(server) {
    const wss = new WebSocket.Server({ noServer: true });
    
    let isInitialized = false;
    const initPromise = (async () => {
        await clearRedisData();
        isInitialized = true;
        console.log('WebSocket server initialization complete');
    })();
    
    server.on('upgrade', async (request, socket, head) => {
        try {
            if (!isInitialized) await initPromise;

            if (socket.wsHandled) { socket.destroy(); return; }
            socket.wsHandled = true;

            wss.handleUpgrade(request, socket, head, (ws) => wss.emit('connection', ws, request));
        } catch (err) {
            console.error('Upgrade error:', err);
            socket.destroy();
        }
    });

    wss.on('connection', (ws) => {
        ws.id = uuidv4();
        ws.isAlive = true;
        console.log('New WebSocket connection, total clients:', wss.clients.size);

        (async () => {
            try { await global.redisClient.set(`${wsKey}${ws.id}`, 'pending'); }
            catch (error) { console.error('Redis set error:', error); }
        })();

        ws.on('pong', () => { ws.isAlive = true; });

        ws.send(JSON.stringify({ action: 'server_time_response', server_time: formatDateTime(new Date()) }));

        ws.on('error', (error) => { console.error('WebSocket error:', error); sendError(ws, 'WebSocket error'); });

        ws.on('message', async (message, isBinary) => {
            let data;
            try {
                data = isBinary ? handleBinaryMessage(ws, message) : JSON.parse(message);
                if (!data) throw new Error('Invalid message format');
            } catch (err) {
                console.error('Message parsing error:', err);
                return sendError(ws, 'Invalid message format');
            }

            switch (data.action) {
                // =================== System ===================
                case 'ping':
                    ws.send(JSON.stringify({ action: 'pong', timestamp: data.timestamp }));
                    break;
                case 'player_login': await playerController.handlePlayerLogin(ws, data); break;
                case 'demo_request': await systemController.handleDemoRequest(ws, data); break;
                case 'get_time_until_month_end': await systemController.handleGetTimeUntilMonthEnd(ws); break;
                case 'get_game_modes_status': await systemController.handleGetGameModesStatus(ws); break;

                // =================== Player ===================
                case 'register_player': await playerController.handleRegisterPlayer(ws, data); break;
                case 'register_player_response':
                    if (data.player_id) {
                        ws.playerId = data.player_id;
                        await global.redisClient.setEx(`ws:${ws.id}:player`, 36000, data.player_id);
                    }
                    break;
                case 'player_connect':
                    await playerController.handlePlayerConnect(ws, data);
                    if (data.player_id) { ws.playerId = data.player_id; roomManager.registerPlayerConnection(ws.playerId, ws); }
                    break;
                case 'update_player_rating': await playerController.handleUpdatePlayerRating(ws, data); break;
                case 'check_name': await playerController.handleCheckName(ws, data); break;
                case 'update_name': await playerController.handleUpdatePlayerName(ws, data); break;
                case 'update_rating': await playerController.handleUpdateRating(ws, data); break;
                case 'get_player_info': await playerController.handleGetPlayerInfo(ws, data); break;
                case 'get_player_data': await playerController.handleGetPlayerData(ws, data); break;

                // =================== Clan ===================
                case 'create_clan': await ClanController.handleCreateClan(ws, data); break;
                case 'join_clan': await ClanController.handleJoinClan(ws, data); break;
                case 'leave_clan': await ClanController.handleLeaveClan(ws, data); break;
                case 'get_all_clans': await ClanController.handleGetAllClans(ws); break;
                case 'search_clans': await ClanController.handleSearchClans(ws, data); break;
                case 'get_clan_info': await ClanController.handleGetClanInfo(ws, data); break;
                case 'get_clan_top_with_current': await ClanController.handleGetClanInfoWithCurrent(ws, data); break;

                // =================== Stats ===================
                case 'update_player_stats': await statsController.handleUpdatePlayerStats(ws, data); break;
                case 'get_rating_leaderboard': await statsController.handleGetRatingLeaderboard(ws, data); break;
                case 'get_kills_leaderboard': await statsController.handleGetKillsLeaderboard(ws, data); break;

                // =================== Hero ===================
                case 'update_hero_stats': await heroController.handleUpdateHeroStats(ws, data); break;
                case 'update_favorite_hero': await heroController.handleUpdateFavoriteHero(ws, data); break;
                case 'get_hero_levels': await heroController.handleGetHeroLevels(ws, data); break;
                case 'update_hero_levels': await heroController.handleUpdateHeroLevels(ws, data); break;

                // =================== Currency ===================
                case 'claim_rewards': await currencyController.handleClaimRewards(ws, data); break;
                case 'add_currency': await currencyController.handleAddCurrency(ws, data); break;
                case 'spend_hero_cards': await currencyController.handleSpendHeroCards(ws, data); break;

                // =================== Room ===================
                case 'join_matchmaking': await handleJoinMatchmaking(ws, data); break;
                case 'leave_room': await handleLeaveRoom(ws, data); break;
                case 'force_close_room': await handleForceCloseRoom(ws, data); break;
                case 'get_room_info': await handleGetRoomInfo(ws, data); break;
                case 'get_matchmaking_stats': await handleGetMatchmakingStats(ws, data); break;
                case 'update_player_ready': await handleUpdatePlayerReady(ws, data); break;

                // =================== Match overral ===================
                case 'update_player_stats_after_battle': await statsController.handleUpdatePlayerStats(ws, data); break;
                case 'get_match_stats': await playerInGameController.handleGetMatchStats(ws, data); break;

                // =================== Movement ===================
                case 'update_player_transform': await playerInGameController.handleUpdatePlayerTransform(ws, data); break;

                // =================== Combat ===================
                case 'deal_damage': await damageController.handleDealDamage(ws, data); break;
                case 'player_death': await damageController.handlePlayerDeath(ws, data); break;

                default:
                    sendError(ws, `Unknown action: ${data.action}`);
            }
        });

        ws.on('close', async (code, reason) => {
            try {
                const playerId = ws.playerId || (await global.redisClient.get(`ws:${ws.id}:player`));
                if (playerId) {
                    // 1. Удаляем игрока из комнат и внутренней мапы соединений
                    await ConnectionController.handlePlayerDisconnect(playerId); // старый метод
                    await roomManager.handlePlayerDisconnect(playerId); // актуальный метод удаления из комнат

                    // 2. Сохраняем данные игрока в БД
                    const playerData = await playerRedisService.getPlayerFromRedis(playerId);
                    if (playerData) {
                        await flushPlayerData(playerId);
                    }
                    
                    // 3. Обновляем данные о клане в БД
                    if (playerData?.clan_id) {
                        await clanSync.syncClanFromRedisToDB(playerData.clan_id);
                    }
                    console.log(`Player ${playerId} disconnected, cleanup done`);
                }

                // 3. Чистим ключи Redis
                console.log('Deleting keys:', `${wsKey}${ws.id}`, `ws:${ws.id}:player`);
                await global.redisClient.del(`${wsKey}${ws.id}`);
                await global.redisClient.del(`ws:connection:${ws.id}:player`);
            } catch (error) {
                console.error('Error during disconnect cleanup:', error);
            }
        });
    });

    const interval = setInterval(() => {
        wss.clients.forEach((ws) => {
            if (!ws.isAlive) return ws.terminate();
            ws.isAlive = false;
            ws.ping();
        });
    }, 30000);

    wss.on('close', () => clearInterval(interval));

    scheduleDailyRestart(wss);

    return wss;
}

// ================= Helper Functions =================
async function clearRedisData() {
    const patterns = ['connection:*'];
    for (const pattern of patterns) {
        const keys = await global.redisClient.keys(pattern);
        if (keys.length) await global.redisClient.del(keys);
    }
}

// ================= Room/Matchmaking Functions =================
async function handleGetRoomInfo(ws, data) {
    if (!ws.playerId) {
        return sendError(ws, 'Player not connected');
    }
 
    const room = roomManager.getRoomByPlayerId(ws.playerId);
    if (room) {
        ws.send(JSON.stringify({
            action: 'room_info',
            room: roomManager.getRoomInfo(room.id)
        }));
    } else {
        sendError(ws, 'Player not in any room');
    }
}
async function handleGetMatchmakingStats(ws, data) {
    const stats = roomManager.getAllModesStatus();
    ws.send(JSON.stringify({
        action: 'matchmaking_stats',
        stats: stats
    }));
}
async function handleLeaveRoom(ws, data) {
    if (!ws.playerId) {
        return sendError(ws, 'Player not connected');
    }
 
    try {
        const room = await roomManager.handlePlayerDisconnect(ws.playerId);
     
        if (room) {
            ws.send(JSON.stringify({
                action: 'room_left',
                room_id: room.id,
                player_id: ws.playerId
            }));
         
        console.log(`Player ${ws.playerId} left room ${room.id}`);
        } else {
            sendError(ws, 'Player not in any room');
        }
    } catch (error) {
        console.error('Error leaving room:', error);
        sendError(ws, 'Failed to leave room');
    }
}
async function handleForceCloseRoom(ws, data) {
    if (!ws.playerId) {
        return sendError(ws, 'Player not connected');
    }
 
    // Optional: Add admin permission check here
    // if (!isAdmin(ws.playerId)) {
    //     return Utils.sendError(ws, 'Insufficient permissions');
    // }
 
    if (!data.room_id) {
        return sendError(ws, 'Room ID required');
    }
 
    try {
        const success = roomManager.forceResetRoom(data.room_id);
     
        if (success) {
            ws.send(JSON.stringify({
                action: 'room_force_closed',
                room_id: data.room_id,
                success: true
            }));
        } else {
            sendError(ws, 'Room not found or already closed');
        }
    } catch (error) {
        console.error('Error force closing room:', error);
        sendError(ws, 'Failed to force close room');
    }
}
async function handleJoinMatchmaking(ws, data) {
    console.log(`Join matchmaking request:`, data);

    const requiredFields = ['player_id', 'mode', 'hero_id'];
    if (!isValidMessage(data, requiredFields)) {
        return sendError(ws, 'Missing player_id, mode or hero_id');
    }

    // Проверяем, что WebSocket всё ещё открыт
    if (ws.readyState !== WebSocket.OPEN) {
        console.log(`WebSocket is not open for player ${data.player_id}, state: ${ws.readyState}`);
        return;
    }

    try {
        // Находим доступную комнату и добавляем игрока
        const room = await roomManager.findAvailableRoom(data.mode, data.player_id, data.hero_id);
        if (!room) {
            console.log(`No rooms available for mode ${data.mode}`);
            ws.send(JSON.stringify({
                action: 'matchmaking_full',
                message: 'All rooms are currently full'
            }));
            return;
        }

        const updatedRoom = await roomManager.addPlayerToRoom(room, data.player_id, data.hero_id);

        // Проверка, что соединение всё ещё открыто
        if (ws.readyState !== WebSocket.OPEN) {
            console.log(`WebSocket closed during matchmaking for ${data.player_id}`);
            await roomManager.removePlayerFromRoom(data.player_id);
            return;
        }

        ws.currentRoom = updatedRoom.id;
        ws.playerId = data.player_id;

        ws.send(JSON.stringify({
            action: 'matchmaking_joined',
            room_id: updatedRoom.id,
            hero_id: data.hero_id,
            players_in_room: updatedRoom.playerCount,
            max_players: updatedRoom.maxPlayers,
            estimated_wait: updatedRoom.state === GameConstants.ROOM_STATES.COUNTDOWN
                ? (30000 - (Date.now() - updatedRoom.startTime))
                : 30000
        }));

    } catch (error) {
        console.error('Matchmaking error:', error);
        sendError(ws, 'Failed to join matchmaking');
    }
}
async function handleUpdatePlayerReady(ws, data) {
    if (!isValidMessage(data, ['player_id', 'is_ready', 'room_id'])) {
        return sendError(ws, 'Missing required fields');
    }
    try {
        // Теперь метод асинхронный!
        const room = await roomManager.getRoomByPlayerId(data.player_id);
        if (room && room.id === data.room_id) {
            // Здесь можно обновить статус готовности игрока
            // и отправить обновление всем в комнате
         
            // Теперь метод асинхронный!
            await roomManager.notifyRoomPlayers(room, {
                action: 'player_ready_update',
                player_id: data.player_id,
                hero_id: data.hero_id,
                is_ready: Boolean(data.is_ready)
            });
            ws.send(JSON.stringify({
                action: 'player_ready_response',
                success: true,
                is_ready: data.is_ready
            }));
        } else {
            sendError(ws, 'Player not in specified room');
        }
    } catch (error) {
        console.error('Player ready update error:', error);
        sendError(ws, 'Failed to update ready status');
    }
}

// ================= Daily Restart =================
function scheduleDailyRestart(wss) {
    const now = new Date();
    const restartTime = new Date(
        now.getFullYear(),
        now.getMonth(),
        now.getDate(),
        0, 0, 0, 0
    );
    // Если время уже прошло сегодня, планируем на завтра
    if (now > restartTime) {
        restartTime.setDate(restartTime.getDate() + 1);
    }
    const timeUntilRestart = restartTime.getTime() - now.getTime();
    // Таймер для основного перезапуска
    setTimeout(() => {
        performRestartSequence(wss);
    }, timeUntilRestart);
    // Таймеры для предупреждений
    scheduleWarning(wss, timeUntilRestart - 60 * 60 * 1000, "in 1 hour");
    scheduleWarning(wss, timeUntilRestart - 30 * 60 * 1000, "in 30 minutes");
    scheduleWarning(wss, timeUntilRestart - 15 * 60 * 1000, "in 15 minutes");
    scheduleWarning(wss, timeUntilRestart - 5 * 60 * 1000, "in 5 minutes");
}

// Функция для планирования предупреждений
function scheduleWarning(wss, timeUntilWarning, message) {
    if (timeUntilWarning <= 0) return;

    setTimeout(() => {
        sendRestartNotification(wss, message);
    }, timeUntilWarning);
}

module.exports = { setupWebSocketServer, InitRoomManager, roomManager };

// //websocket.js
// const WebSocket = require('ws');
// const { v4: uuidv4 } = require('uuid');
// const { GameConstants } = require('../config/constants');
// const ConnectionController = require('../controllers/connectionController');

// const { 
//     handleRegisterPlayer,
//     handlePlayerConnect,
//     handleGetPlayerData,
//     handleUpdatePlayerName } = require('../controllers/playerController');

// const playerInGameController = require('../controllers/playerInGameController');

// const isPm2Master = process.env.NODE_APP_INSTANCE === '0';

// const { flushPlayerData } = require('./servicesflushService');

// const playerController = require('../controllers/playerController');
// const ClanController = require('../controllers/clanController');
// const currencyController = require('../controllers/currencyController');
// const statsController = require('../controllers/statsController');
// const systemController = require('../controllers/systemController');
// const heroController = require('../controllers/heroController');

// const {  roomManager, initializeRoomManager } = require('../controllers/roomManager');

// const { formatDateTime,
//         isValidMessage,
//         sendError,
//         getPlayerFromRedis,
//         handleBinaryMessage } = require('../services/utils');

// // Инициализируем RoomManager после подключения Rediss
// async function InitRoomManager() {
//     try {
//         const success = await initializeRoomManager();
//         if (success) {
//             console.log('RoomManager initialized successfully in WebSocket');
//         }
//     } catch (error) {
//         console.error('Failed to initialize RoomManager:', error);
//     }
// }

// const connectedPlayers = new Map();

// //Достаем из скрипта констант нужные нам 
// const {
//     wsKey,
//     //константы дальше
// } = require ('../config/constants');

// //Инициализация сокета
// function setupWebSocketServer(server) {
//     const wss = new WebSocket.Server({ noServer: true });
    
//     let isInitialized = false;
//     const initPromise = (async () => {
//         await clearRedisData();
//         isInitialized = true;
//         console.log('WebSocket server initialization complete');
//     })();
    
//     server.on('upgrade', async (request, socket, head) => {
//         try {
//             if (!isInitialized) {
//                 console.log('Waiting for initialization...');
//                 await initPromise;
//             }
            
//             if (socket.wsHandled) {
//                 socket.destroy();
//                 return;
//             }
//             socket.wsHandled = true;
            
//             wss.handleUpgrade(request, socket, head, (ws) => {
//                 wss.emit('connection', ws, request);
//             });
//         } catch (err) {
//             console.error('Upgrade error:', err);
//             socket.destroy();
//         }
//     });
    
//     wss.on('connection', (ws) => {
//         ws.id = uuidv4();// Добавляем уникальный ID
//         console.log('New connection in Socket, total clients:', wss.clients.size);
        
//         (async () => {
//             try {
//                 await global.redisClient.set(`${wsKey}${ws.id}`, 'pending');
                
//             } catch (error) {
//                 console.error('Redis set error:', error);
//             }
//         })();
        
//         ws.isAlive = true;
        
//         ws.on('pong', () => {
//             ws.isAlive = true;
//         });
        
//         ws.send(JSON.stringify({
//             action: 'server_time_response',
//             server_time: formatDateTime(new Date())
//         }));

//         ws.on('error', (error) => {
//             console.error('WebSocket error:', error);
//             sendError(ws, 'WebSocket error occurred');
//         });

//         ws.on('message', async (message, isBinary) => {
//             let data;

//             if (isBinary) {
//                 data = handleBinaryMessage(ws, message);
//             }
//             else {
//                 data = JSON.parse(message);
//             }

//             if (!data) throw new Error('Invalid message format');
            
//             //Вызвать нужную функцию в зависимости от полученного экшена
//             switch (data.action) 
//             {
//                 //case 'get_server_stats': await PlayerController.handleCheckPlayer(ws, data); break; ПРИМЕР ОБРАБОТКИ ПРИШЕДШЕГО ЗАПРОСА 
//                 case 'ping':
//                     // Ответ на ping
//                     ws.send(JSON.stringify({
//                         action: 'pong',
//                         timestamp: data.timestamp // Возвращаем тот же timestamp для расчета задержки
//                     }));
//                     break;
                
//                 case 'player_login':
//                     await playerController.handlePlayerLogin(ws, data);
//                     break;

//                 case 'demo_request': 
//                     await systemController.handleDemoRequest(ws, data); 
//                     break;
//                 case 'get_time_until_month_end': 
//                     await systemController.handleGetTimeUntilMonthEnd(ws); 
//                     break;
//                 case 'get_game_modes_status': 
//                     await systemController.handleGetGameModesStatus(ws); 
//                     break;

//                 // Player actions
//                 case 'register_player': 
//                     await playerController.handleRegisterPlayer(ws, data); 
//                     break;
//                 case 'register_player_response':
//                     if (data.player_id) {
//                         ws.playerId = data.player_id;
//                         console.log(`Player ${data.player_id} connected, WebSocket ID: ${ws.id}`);
                        
//                         // Также сохраняем в Redis для отслеживания
//                         await global.redisClient.setEx(
//                             `ws:${ws.id}:player`, 
//                             3600,
//                             data.player_id
//                         );
                        
//                         // РЕГИСТРИРУЕМ В ROOMMANAGER
//                         // roomManager.registerPlayerConnection(data.player_id, ws);
//                     }
//                     break;
//                 case 'player_connect':
//                     await handlePlayerConnect(ws, data);
//                     if (data.player_id) {
//                         ws.playerId = data.player_id;
//                         roomManager.registerPlayerConnection(ws.playerId, ws);
//                         // console.log(`Player ${ws.playerId} connection registered`);
//                     }
//                     break;


//                 case 'update_player_rating': 
//                     await playerController.handleUpdatePlayerRating(ws, data); 
//                     break;
//                 case 'check_name': 
//                     await playerController.handleCheckName(ws, data); 
//                     break;
//                 case 'update_name': 
//                     await playerController.handleUpdatePlayerName(ws, data); 
//                     break;
//                 case 'update_rating': 
//                     await playerController.handleUpdateRating(ws, data); 
//                     break;
//                 case 'get_player_info': 
//                     await playerController.handleGetPlayerInfo(ws, data); 
//                     break;
//                 case 'get_player_data': 
//                     await playerController.handleGetPlayerData(ws, data); 
//                     break;


//                 // Clan actions
//                 case 'create_clan': 
//                     await ClanController.handleCreateClan(ws, data); 
//                     break;
//                 case 'join_clan': 
//                     await ClanController.handleJoinClan(ws, data); 
//                     break;
//                 case 'leave_clan': 
//                     await ClanController.handleLeaveClan(ws, data); 
//                     break;
//                 case 'get_all_clans': 
//                     await ClanController.handleGetAllClans(ws); 
//                     break;
//                 case 'search_clans': 
//                     await ClanController.handleSearchClans(ws, data); 
//                     break;
//                 case 'get_clan_info': 
//                     await ClanController.handleGetClanInfo(ws, data); 
//                     break;
//                 case 'get_clan_top_with_current': 
//                     await ClanController.handleGetClanInfoWithTop(ws, data); 
//                     break;

//                 // Stats actions
//                 case 'update_player_stats': 
//                     await playerInGameController.handleUpdatePlayerStats(ws, data);
//                 // await statsController.handleUpdatePlayerStats(ws, data);
//                     break;
//                 case 'get_rating_leaderboard': 
//                     await statsController.handleGetRatingLeaderboard(ws, data); 
//                     break;
//                 case 'get_kills_leaderboard': 
//                     await statsController.handleGetKillsLeaderboard(ws, data); 
//                     break;

//                 // Hero actions
//                 case 'update_hero_stats': 
//                     await heroController.handleUpdateHeroStats(ws, data); 
//                     break;
//                 case 'update_favorite_hero': 
//                     await heroController.handleUpdateFavoriteHero(ws, data); 
//                     break;
//                 case 'get_hero_levels': 
//                     await heroController.handleGetHeroLevels(ws, data); 
//                     break;
//                 case 'update_hero_levels': 
//                     await heroController.handleUpdateHeroLevels(ws, data); 
//                     break;

//                 // Currency actions
//                 case 'claim_rewards':
//                     console.log('Claim rewards received:', data);
//                     await currencyController.handleClaimRewards(ws, data);
//                     break;
//                 case 'add_currency': 
//                     await currencyController.handleAddCurrency(ws, data); 
//                     break;
//                 case 'spend_hero_cards': 
//                     await currencyController.handleSpendHeroCards(ws, data); 
//                     break;
                
//                 // Room actions
//                 case 'join_matchmaking':
//                     await handleJoinMatchmaking(ws, data);
//                     break;
//                 case 'leave_room':
//                     await handleLeaveRoom(ws, data);
//                     break;
//                 case 'force_close_room':
//                     await handleForceCloseRoom(ws, data);
//                     break;
//                 case 'get_room_info':
//                     await handleGetRoomInfo(ws, data);
//                     break;
//                 case 'get_matchmaking_stats':
//                     await handleGetMatchmakingStats(ws, data);
//                     break;
                
//                 case 'update_player_transform':
//                     await playerInGameController.handleUpdatePlayerTransform(ws, data);
//                     break;
//                 case 'update_player_stats_after_batle':
//                     await statsController.handleUpdatePlayerStats(ws, data); 
//                     // await playerInGameController.handleUpdatePlayerStats(ws, data);
//                     break;
//                 case 'update_player_ready':
//                     await handleUpdatePlayerReady(ws, data);
//                     break;
//                 case 'get_match_stats':
//                     await playerInGameController.handleGetMatchStats(ws, data);
//                     break;
//                 case 'player_death':
//                     await playerInGameController.handlePlayerDeath(ws, data);
//                     break;

//                 default:
//                     sendError(ws, `Unknown action: ${data.action}`);
//             }
//         });

//         ws.on('close', async (code, reason) => {
//             console.log(`Connection closed: ${ws.id}, code: ${code}, reason: ${reason}`);
            
//             try {
//                 // Проверяем разные способы получения playerId
//                 const playerId = ws.playerId || (await global.redisClient.get(`ws:${ws.id}:player`));
                
//                 if (playerId) {
//                     // Уведомляем контроллер подключений
//                     await ConnectionController.handlePlayerDisconnect(playerId);

//                     // Обрабатываем отключение в roomManager
//                     roomManager.unregisterPlayerConnection(playerId);
//                     roomManager.handlePlayerDisconnect(playerId);

//                     console.log(`Saving data for player: ${playerId}`);
                    
//                     // 1. Получаем данные из Redis
//                     console.log(`Saving data for player: ${playerId}`);
//                     const playerData = await getPlayerFromRedis(playerId);
                    
//                     if (playerData) {
//                         console.log(`[${playerId}] Found data in Redis, starting flush...`);
//                         await flushPlayerData(playerId);                        
//                         console.log(`[${playerId}] Data successfully saved`);
//                     } 
//                     else 
//                     {
//                         console.log(`[${playerId}] No data in Redis to save`);
//                     }
//                 } else {
//                     console.log(`WebSocket ${ws.id} closed without playerId - skipping save`);
//                 }
//             } catch (error) {
//                 console.error('Error during disconnect cleanup:', error);
//             }
            
//             // Удаляем из Redis информацию о подключении
//             try {
//                 await global.redisClient.del(`${wsKey}${ws.id}`);
//                 await global.redisClient.del(`ws:${ws.id}:player`);
//             } catch (error) {
//                 console.error('Error cleaning up Redis connection:', error);
//             }
            
//             // // Удаляем из connectedPlayers
//             // if (ws.playerId) {
//             //     connectedPlayers.delete(ws.playerId);
//             //     console.log(`Player ${ws.playerId} removed from connectedPlayers`);
//             // }
            
//             console.log(`WebSocket ${ws.id} cleanup completed`);
//         });
//     });
    
//     const interval = setInterval(() => {
//         wss.clients.forEach((ws) => {
//             // if (!ws.isAlive) {
//             //     if(ws.id){
//             //         //ЧТО СДЕЛАТЬ НА ОТКЛЮЧЕНИЕ СЕРВЕРА?
//             //     }
//             //     return ws.terminate();
//             // }
//             if (!ws.isAlive) return ws.terminate();
//             ws.isAlive = false;
//             ws.ping();
//         });
//     }, 30000);

//     wss.on('close', () => {
//         console.log('Server closed');
//         clearInterval(interval);
//     });
    
//     scheduleDailyRestart(wss); //перезагрузка сервера ночью
//     return wss;
// }

// // Вызывайте эту функцию после подключения к Redis
// // initializeRoomManager();

// async function handleGetRoomInfo(ws, data) {
//     if (!ws.playerId) {
//         return sendError(ws, 'Player not connected');
//     }
    
//     const room = roomManager.getRoomByPlayerId(ws.playerId);
//     if (room) {
//         ws.send(JSON.stringify({
//             action: 'room_info',
//             room: roomManager.getRoomInfo(room.id)
//         }));
//     } else {
//         sendError(ws, 'Player not in any room');
//     }
// }

// async function handleGetMatchmakingStats(ws, data) {
//     const stats = roomManager.getAllModesStatus();
//     ws.send(JSON.stringify({
//         action: 'matchmaking_stats',
//         stats: stats
//     }));
// }
// async function handleLeaveRoom(ws, data) {
//     if (!ws.playerId) {
//         return sendError(ws, 'Player not connected');
//     }
    
//     try {
//         const room = roomManager.removePlayerFromRoom(ws.playerId);
        
//         if (room) {
//             ws.send(JSON.stringify({
//                 action: 'room_left',
//                 room_id: room.id,
//                 player_id: ws.playerId
//             }));
            
//             console.log(`Player ${ws.playerId} left room ${room.id}`);
//         } else {
//             sendError(ws, 'Player not in any room');
//         }
//     } catch (error) {
//         console.error('Error leaving room:', error);
//         sendError(ws, 'Failed to leave room');
//     }
// }
// async function handleForceCloseRoom(ws, data) {
//     if (!ws.playerId) {
//         return sendError(ws, 'Player not connected');
//     }
    
//     // Optional: Add admin permission check here
//     // if (!isAdmin(ws.playerId)) {
//     //     return Utils.sendError(ws, 'Insufficient permissions');
//     // }
    
//     if (!data.room_id) {
//         return sendError(ws, 'Room ID required');
//     }
    
//     try {
//         const success = roomManager.forceResetRoom(data.room_id);
        
//         if (success) {
//             ws.send(JSON.stringify({
//                 action: 'room_force_closed',
//                 room_id: data.room_id,
//                 success: true
//             }));
//         } else {
//             sendError(ws, 'Room not found or already closed');
//         }
//     } catch (error) {
//         console.error('Error force closing room:', error);
//         sendError(ws, 'Failed to force close room');
//     }
// }

// //чистка ключей, которые не нужно сохранять в БД
// async function clearRedisData() {
//     const patterns = [
//         'connection:*',
//         //'playerInGame:*'
//     ];
    
//     console.log('Completely flushed Redis database');
    
//     for (const pattern of patterns) {
//         const keys = await global.redisClient.keys(pattern);
//         if (keys.length > 0) {
//             await global.redisClient.del(keys);
//             console.log(`Deleted ${keys.length} keys for ${pattern}`);
//         }
//     }
// }

// // Функция для планирования перезагрузки
// function scheduleDailyRestart(wss) {
//     const now = new Date();
//     const restartTime = new Date(
//         now.getFullYear(),
//         now.getMonth(),
//         now.getDate(),
//         0, 0, 0, 0
//     );

//     // Если время уже прошло сегодня, планируем на завтра
//     if (now > restartTime) {
//         restartTime.setDate(restartTime.getDate() + 1);
//     }

//     const timeUntilRestart = restartTime.getTime() - now.getTime();

//     // Таймер для основного перезапуска
//     setTimeout(() => {
//         performRestartSequence(wss);
//     }, timeUntilRestart);

//     // Таймеры для предупреждений
//     scheduleWarning(wss, timeUntilRestart - 60 * 60 * 1000, "in 1 hour");
//     scheduleWarning(wss, timeUntilRestart - 30 * 60 * 1000, "in 30 minutes");
//     scheduleWarning(wss, timeUntilRestart - 15 * 60 * 1000, "in 15 minutes");
//     scheduleWarning(wss, timeUntilRestart - 5 * 60 * 1000, "in 5 minutes");
// }

// // Функция для планирования предупреждений
// function scheduleWarning(wss, timeUntilWarning, message) {
//     if (timeUntilWarning <= 0) return;

//     setTimeout(() => {
//         sendRestartNotification(wss, message);
//     }, timeUntilWarning);
// }

// // Функция для выполнения последовательности перезагрузки
// async function performRestartSequence(wss) {
//     // Отправляем предупреждения с обратным отсчетом
//     await sendRestartNotification(wss, "in 3 minutes");
//     await new Promise(resolve => setTimeout(resolve, 60 * 1000));
//     await sendRestartNotification(wss, "in 2 minutes");
//     await new Promise(resolve => setTimeout(resolve, 60 * 1000));
//     await sendRestartNotification(wss, "in 1 minute");
//     await new Promise(resolve => setTimeout(resolve, 50 * 1000));

//     // Последние 10 секунд - секундный отсчет
//     for (let i = 10; i > 0; i--) {
//         await sendRestartNotification(wss, `in ${i} seconds`);
//         await new Promise(resolve => setTimeout(resolve, 1000));
//     }

//     // Отправляем уведомление о перезагрузке всем клиентам
//     wss.clients.forEach(client => {
//         if (client.readyState === WebSocket.OPEN) {
//             client.send(JSON.stringify({
//                 action: 'call',
//                 message: 'server_restart'
//             }));
//             setTimeout(() => {
//                 client.close(1001, 'Server restarting'); // 1001 = "going away"
//             }, 3000); // 5 секунд на завершение
//         }
//     });
    
//     // Даём время на закрытие соединений
//     setTimeout(() => {
//         wss.close(() => {
//             console.log('Сервер корректно остановлен');
//             process.exit(0);
//         });
//     }, 2000); // 5 секунд на завершение
// }

// //Логика для комнат
// async function handleJoinMatchmaking(ws, data) {
//     console.log(`Join matchmaking request:`, data);
    
//     const requiredFields = ['player_id', 'mode', 'hero_id'];
//     if (!isValidMessage(data, requiredFields)) {
//         return sendError(ws, 'Missing player_id, mode or hero_id');
//     }

//     // ПРОВЕРЯЕМ ЧТО WebSocket ВСЕ ЕЩЕ ОТКРЫТ
//     if (ws.readyState !== WebSocket.OPEN) {
//         console.log(`WebSocket is not open for player ${data.player_id}, state: ${ws.readyState}`);
//         return;
//     }

//     try {
//         // roomManager.registerPlayerConnection(data.player_id, ws);
//         const room = await roomManager.findAvailableRoom(data.mode, data.player_id, data.hero_id);

//         if (room) {
//             // ЕЩЕ РАЗ ПРОВЕРЯЕМ СОЕДИНЕНИЕ
//             if (ws.readyState !== WebSocket.OPEN) {
//                 console.log(`WebSocket closed during matchmaking for ${data.player_id}`);
//                 await roomManager.removePlayerFromRoom(data.player_id);
//                 return;
//             }
//             // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: убедимся что комната все еще доступна
//             const currentRoomInfo = await roomManager.getRoomInfo(room.id);
//             if (currentRoomInfo && currentRoomInfo.state === GameConstants.ROOM_STATES.IN_PROGRESS) {
//                 console.log(`Room ${room.id} started game during matchmaking`);
//                 ws.send(JSON.stringify({
//                     action: 'matchmaking_failed',
//                     message: 'Room started game during matchmaking',
//                     room_id: room.id
//                 }));
//                 return;
//             }
            
//             console.log(`Player ${data.player_id} joined room ${room.id}`);
//             ws.currentRoom = room.id;
//             ws.playerId = data.player_id;

//             ws.send(JSON.stringify({
//                 action: 'matchmaking_joined',
//                 room_id: room.id,
//                 hero_id: data.hero_id,
//                 players_in_room: room.playerCount,
//                 max_players: room.maxPlayers,
//                 estimated_wait: room.state === 'countdown' ? 
//                     (30000 - (Date.now() - room.startTime)) : 30000
//             }));
//         } else {
//             console.log(`No rooms available for mode ${data.mode}`);
//             ws.send(JSON.stringify({
//                 action: 'matchmaking_full',
//                 message: 'All rooms are currently full'
//             }));
//         }
//     } catch (error) {
//         console.error('Matchmaking error:', error);
//         sendError(ws, 'Failed to join matchmaking');
//     }
// }

// async function handleUpdatePlayerReady(ws, data) {
//     if (!isValidMessage(data, ['player_id', 'is_ready', 'room_id'])) {
//         return sendError(ws, 'Missing required fields');
//     }

//     try {
//         // Теперь метод асинхронный!
//         const room = await roomManager.getRoomByPlayerId(data.player_id);
//         if (room && room.id === data.room_id) {
//             // Здесь можно обновить статус готовности игрока
//             // и отправить обновление всем в комнате
            
//             // Теперь метод асинхронный!
//             await roomManager.notifyRoomPlayers(room, {
//                 action: 'player_ready_update',
//                 player_id: data.player_id,
//                 hero_id: data.hero_id,
//                 is_ready: Boolean(data.is_ready)
//             });

//             ws.send(JSON.stringify({
//                 action: 'player_ready_response',
//                 success: true,
//                 is_ready: data.is_ready
//             }));
//         } else {
//             sendError(ws, 'Player not in specified room');
//         }
//     } catch (error) {
//         console.error('Player ready update error:', error);
//         sendError(ws, 'Failed to update ready status');
//     }
// }

// module.exports = {
//     setupWebSocketServer,
//     InitRoomManager,
//     roomManager
// };
//660 cтрок