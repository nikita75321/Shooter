//Константы, которые нужно будет применять в других скриптах
const Constants = {
    wsKey: `ws:connection:`, 
    myVar: 'Hello!',
    playerTransformKey: `player_transform:`,
    playerHeroKey: 'player:hero:',
    roomKey: 'room:',
    roomPlayersKey: `room_players:`,
    matchKey: `match:`,
    playerStats: `player_stats:`,
    boostKey: "boost:",
    upgradeKey: "upgrade:",
    roomBoostsKey: "room_boosts:"
};

const GameConstants = {
    MAX_ROOMS_PER_MODE: 40,
    MODE_CAPACITY: {
        1: 12,
        2: 15,
        3: 10
    },
    MATCHMAKING_TIME: 5000, // 5 секунд
    ROOM_STATES: {
        WAITING: 'waiting',
        COUNTDOWN: 'countdown',
        IN_PROGRESS: 'in_progress',
        COMPLETED: 'completed'
    }
};

module.exports = { 
    Constants,
    GameConstants
};
