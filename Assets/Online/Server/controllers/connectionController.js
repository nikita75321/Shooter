const Utils = require('../services/utils');

class ConnectionController {
    constructor() {
        this.connectedPlayers = new Map();
    }

    async handlePlayerLogin(ws, data) {
        // Логика входа игрока
        const requiredFields = ['player_id'];
        if (!isValidMessage(data, requiredFields)) {
            return sendError(ws, 'Missing player_id');
        }

        try {
            const playerData = await Utils.getPlayerFromRedis(data.player_id);
            if (!playerData) {
                return sendError(ws, 'Player not found');
            }

            // Сохраняем playerId в WebSocket
            ws.playerId = data.player_id;
            this.connectedPlayers.set(data.player_id, ws);

            // Устанавливаем клан игрока в Redis если он есть
            // if (playerData.clan_name) {
            //     // Здесь нужно получить clan_id по имени клана
            //     // Пока просто устанавливаем флаг
            //     await Utils.setPlayerClanInRedis(data.player_id );
            // }

            ws.send(JSON.stringify({
                action: 'player_login_response',
                success: true,
                player_id: data.player_id,
                player_name: playerData.username,
                rating: playerData.rating,
                clan_name: playerData.clan_name || null
            }));

        } catch (error) {
            console.error('Player login error:', error);
            sendError(ws, 'Login failed');
        }
    }

    async handlePlayerDisconnect(playerId) {
        if (playerId) {
            this.connectedPlayers.delete(playerId);
            console.log(`Player ${playerId} disconnected`);
            
            // Здесь можно добавить логику сохранения данных при отключении
        }
    }

    getConnectedPlayer(playerId) {
        return this.connectedPlayers.get(playerId);
    }

    getConnectedPlayersCount() {
        return this.connectedPlayers.size;
    }
}

module.exports = new ConnectionController();