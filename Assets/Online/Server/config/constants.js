//Константы, которые нужно будет применять в других скриптах
const Constants = {
    wsKey: `ws:connection:`, 
    myVar: 'Hello!',

    playerTransformKey: `player_transform:`,
    playerHeroKey: `player:hero:`,
    playerUpgradesKey:'player_upgrades:',

    roomKey: 'room:',
    roomPlayersKey: `room_players:`,

    matchKey: `match:`,

    playerStats: `player_stats:`,

    upgradeKey: `upgrade:`,
    roomUpgradesKey : `room_upgrades:`,

    boostKey: `boost:`,
    roomBoostsKey: `room_boosts:`
};

const GameConstants = {
    MAX_ROOMS_PER_MODE: 40,
    MODE_CAPACITY: {
        1: 12,
        2: 15,
        3: 10
    },
    MATCHMAKING_TIME: 5000, // 5 секунд
    MATCH_DURATION_MS: 90 * 1000, // 1 минута 30 секунд
    ROOM_STATES: {
        WAITING: 'waiting',
        COUNTDOWN: 'countdown',
        IN_PROGRESS: 'in_progress',
        COMPLETED: 'completed'
    },
    SPAWN_POINTS: [
        { position: { x: -21.49, y: 0.6, z: 9.04 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: -21.61, y: 0.6, z: 5.57 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: -21.62, y: 0.6, z: 7.41 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: -21.66, y: 0.6, z: -15.45 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: -21.87, y: 0.6, z: -13.34 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: 20.76, y: 0.6, z: 8.24 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: 20.84, y: 0.6, z: 6.31 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: 20.9, y: 0.6, z: -15.75 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: 20.92, y: 0.6, z: -14.04 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: 20.92, y: 0.6, z: -12.21 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: -5.36, y: 0.6, z: -2.85 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: 5.48, y: 0.6, z: -2.68 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: 0.16, y: 0.6, z: 0.12 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: 0.11, y: 0.6, z: -2.59 }, rotation: { x: 0, y: 0, z: 0, w: 1 } },
        { position: { x: 0.09, y: 0.6, z: -5.3 }, rotation: { x: 0, y: 0, z: 0, w: 1 } }
    ]
};

module.exports = { 
    Constants,
    GameConstants
};
