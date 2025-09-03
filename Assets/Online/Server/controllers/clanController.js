// controllers/clanController.js
const { db } = require('../config/db');

const ClanRedisService = require('../services/clanRedisService');
const clanSync = require('../services/clanSyncService')
const playerRedisService = require('../services/playerRedisService');
const { updatePlayerInRedis, sendError } = require('../services/utils');


class ClanController {
    //=====================Обработчики=====================
    async handleCreateClan(ws, data) {
        try {
            const result = await clanSync.createClan(data);
            ws.send(JSON.stringify({
                action: 'create_clan_response',
                success: true,
                clan_id: result.clanId,
                clan_name: result.clanData.clan_name
            }));
        } catch (err) {
            console.error('Create clan error:', err);
            sendError(ws, err.message);
        }
    }

    async handleGetAllClans(ws) {
        try {
            const clans = await ClanRedisService.getAllClansFromRedis();
            ws.send(JSON.stringify({
                action: 'get_all_clans_response',
                clans
            }));
        } catch (err) {
            console.error('Get all clans error:', err);
            sendError(ws, err.message);
        }
    }

    // async handleSearchClans(ws, data) {
    //     try {
    //         if (!data.query) return sendError(ws, 'Missing query');
    //         const allClans = await ClanRedisService.getAllClansFromRedis();
    //         const filtered = allClans.filter(c => c.clan_name.toLowerCase().includes(data.query.toLowerCase()));
    //         ws.send(JSON.stringify({
    //             action: 'search_clans_response',
    //             clans: filtered
    //         }));
    //     } catch (err) {
    //         console.error('Search clans error:', err);
    //         sendError(ws, err.message);
    //     }
    // }
    async handleSearchClans(ws, data) {
        try {
            const query = data.query || data.search_term; // поддержка обоих вариантов
            if (!query) return sendError(ws, 'Missing query');

            const allClans = await ClanRedisService.getAllClansFromRedis();
            const filtered = allClans.filter(c => c.clan_name.toLowerCase().includes(query.toLowerCase()));

            ws.send(JSON.stringify({
                action: 'search_clans_response',
                clans: filtered
            }));
        } catch (err) {
            console.error('Search clans error:', err);
            sendError(ws, err.message);
        }
    }

    // async handleGetClanInfoWithCurrent(ws, data) {
    //     try {
    //         if (!data.player_id) return sendError(ws, 'Missing player_id');
    //         if (!data.clan_id) return sendError(ws, 'Missing clan_id');

    //         const allClans = await ClanRedisService.getAllClansFromRedis();

    //         // Сортировка по очкам и формирование топ-10
    //         const sortedClans = allClans.sort((a, b) => (b.clan_points || 0) - (a.clan_points || 0));
    //         const topClans = sortedClans.slice(0, 10).map((clan, index) => ({
    //             ...clan,
    //             place: index + 1 // уникальное место
    //         }));

    //         // Текущий клан
    //         const currentClan = await ClanRedisService.getClanFromRedis(data.clan_id);

    //         ws.send(JSON.stringify({
    //             action: 'get_clan_top_with_current_response',
    //             success: true,
    //             topClans,
    //             currentClan
    //         }));
    //     } catch (err) {
    //         console.error('Get clan top error:', err);
    //         sendError(ws, err.message);
    //     }
    // }
    // async handleGetClanInfoWithCurrent(ws, data) {
    //     try {
    //         if (!data.player_id) return sendError(ws, 'Missing player_id');
    //         if (!data.clan_id) return sendError(ws, 'Missing clan_id');

    //         const allClans = await ClanRedisService.getAllClansFromRedis();

    //         // Для каждого клана считаем клановые очки по участникам
    //         for (const clan of allClans) {
    //             const members = await ClanRedisService.getClanMembersFromRedis(clan.clan_id);
    //             clan.clan_points = members.reduce((sum, m) => sum + (m.clan_points || 0), 0);
    //         }

    //         // Сортировка по очкам и формирование топ-10
    //         const sortedClans = allClans.sort((a, b) => (b.clan_points || 0) - (a.clan_points || 0));
    //         const topClans = sortedClans.slice(0, 10).map((clan, index) => ({
    //             ...clan,
    //             place: index + 1 // уникальное место
    //         }));

    //         // Текущий клан
    //         const currentClan = await ClanRedisService.getClanFromRedis(data.clan_id);
    //         if (currentClan) {
    //             const members = await ClanRedisService.getClanMembersFromRedis(currentClan.clan_id);
    //             currentClan.clan_points = members.reduce((sum, m) => sum + (m.clan_points || 0), 0);
    //         }

    //         ws.send(JSON.stringify({
    //             action: 'get_clan_top_with_current_response',
    //             success: true,
    //             topClans,
    //             currentClan
    //         }));
    //     } catch (err) {
    //         console.error('Get clan top error:', err);
    //         sendError(ws, err.message);
    //     }
    // }
    async handleGetClanInfoWithCurrent(ws, data) {
        try {
            if (!data.player_id) return sendError(ws, 'Missing player_id');
            if (!data.clan_id) return sendError(ws, 'Missing clan_id');
            
            const BASE_MAX_PLAYERS = 25; // базовый максимум участников

            // 1️⃣ Берем все кланы из Redis
            const allClans = await ClanRedisService.getAllClansFromRedis();

            // 2️⃣ Для каждого клана считаем сумму очков участников и пересчитываем max_players
            const clansWithStats = await Promise.all(allClans.map(async clan => {
                const members = await ClanRedisService.getClanMembersFromRedis(clan.clan_id);
                const totalPoints = members.reduce((sum, m) => sum + (m.clan_points || 0), 0);
                const currentLevel = getClanLevelByPoints(totalPoints);
                // Пересчет max_players
                const max_players = calculateMaxPlayers(BASE_MAX_PLAYERS, currentLevel);
                console.log(max_players);
                

                return {
                    ...clan,
                    current_level: currentLevel,
                    max_players,
                    clan_points: totalPoints,
                    stats: {
                        current_level: currentLevel,
                        max_players,
                        player_count: clan.player_count,
                        place: 0 // будет заполняться после сортировки
                    }
                };
            }));

            // 3️⃣ Сортировка по очкам и топ-10
            const sortedClans = clansWithStats.sort((a, b) => (b.clan_points || 0) - (a.clan_points || 0));
            const topClans = sortedClans.slice(0, 10).map((clan, index) => ({
                ...clan,
                stats: { ...clan.stats, place: index + 1 }
            }));

            // 4️⃣ Текущий клан
            const currentClan = await ClanRedisService.getClanFromRedis(data.clan_id);
            const currentMembers = await ClanRedisService.getClanMembersFromRedis(currentClan.clan_id);
            const totalCurrentPoints = currentMembers.reduce((sum, m) => sum + (m.clan_points || 0), 0);
            const currentClanLevel = getClanLevelByPoints(totalCurrentPoints);
            const currentMaxPlayers = calculateMaxPlayers(BASE_MAX_PLAYERS, currentClanLevel);
            
            currentClan.max_players = currentMaxPlayers;
            
            currentClan.stats = {
                current_level: currentClanLevel,
                clan_points: totalCurrentPoints,
                max_players: currentMaxPlayers,
                player_count: currentClan.player_count,
                place: topClans.find(c => c.clan_id === currentClan.clan_id)?.stats.place || 0
            };

            ws.send(JSON.stringify({
                action: 'get_clan_top_with_current_response',
                success: true,
                topClans,
                currentClan
            }));

        } catch (err) {
            console.error('Get clan top error:', err);
            sendError(ws, err.message);
        }
    }

    // Игрок покидает клан
    // async handleLeaveClan(ws, data) {
    //     const requiredFields = ['player_id'];
    //     if (!data || requiredFields.some(f => !data[f])) return sendError(ws, 'Missing player_id');

    //     try {
    //         // Получаем клан игрока
    //         const playerClan = await ClanRedisService.getPlayerClanFromRedis(data.player_id);
    //         if (!playerClan) return sendError(ws, 'Player not in any clan');

    //         const clanId = playerClan.clan_id;
    //         const clan = await ClanRedisService.getClanFromRedis(clanId);
    //         if (!clan) {
    //             await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
    //             return sendError(ws, 'Clan not found');
    //         }

    //         // Получаем участников
    //         const members = await ClanRedisService.getClanMembersFromRedis(clanId);
    //         const member = members.find(m => m.player_id === data.player_id);
    //         if (!member) {
    //             await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
    //             return sendError(ws, 'Player not found in clan members');
    //         }

    //         const isLeader = member.is_leader;

    //         // Удаляем игрока из клана
    //         await ClanRedisService.removeClanMemberFromRedis(clanId, data.player_id);
    //         await ClanRedisService.updateClanInRedis(clanId, { player_count: Math.max(0, clan.player_count - 1) });
    //         await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
    //         await updatePlayerInRedis(data.player_id, { clan_id: null, clan_name: null, clan_points: 0 });

    //         let clanDeleted = false;

    //         if (isLeader) {
    //             const remainingMembers = await ClanRedisService.getClanMembersFromRedis(clanId);
    //             if (remainingMembers.length > 0) {
    //                 const newLeader = remainingMembers[0];
    //                 await ClanRedisService.updateClanInRedis(clanId, {
    //                     leader_id: newLeader.player_id,
    //                     leader_name: newLeader.player_name
    //                 });
    //                 await ClanRedisService.updateClanMemberLeaderFlag(clanId, newLeader.player_id);
    //             } else {
    //                 await ClanRedisService.removeClanFromRedis(clanId);
    //                 clanDeleted = true;
    //             }
    //         }

    //         ws.send(JSON.stringify({
    //             action: 'leave_clan_response',
    //             success: true,
    //             player_id: data.player_id,
    //             was_leader: isLeader,
    //             clan_deleted: clanDeleted,
    //             clan_name: clan.clan_name
    //         }));

    //     } catch (err) {
    //         console.error('Leave clan error:', err);
    //         sendError(ws, err.message);
    //     }
    // }
    // async handleLeaveClan(ws, data) {
    //     const requiredFields = ['player_id'];
    //     if (!data || requiredFields.some(f => !data[f])) return sendError(ws, 'Missing player_id');

    //     try {
    //         // Получаем клан игрока
    //         const playerClan = await ClanRedisService.getPlayerClanFromRedis(data.player_id);
    //         if (!playerClan) return sendError(ws, 'Player not in any clan');

    //         const clanId = playerClan.clan_id; // число или строка

    //         console.log('playerClan.clan_id:', playerClan.clan_id);
    //         console.log('typeof playerClan.clan_id:', typeof playerClan.clan_id);

    //         const clan = await ClanRedisService.getClanFromRedis(clanId);
    //         if (!clan) {
    //             await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
    //             return sendError(ws, 'Clan not found');
    //         }

    //         // Получаем участников
    //         const members = await ClanRedisService.getClanMembersFromRedis(clanId);
    //         const member = members.find(m => m.player_id === data.player_id);
    //         if (!member) {
    //             await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
    //             return sendError(ws, 'Player not found in clan members');
    //         }

    //         const isLeader = member.is_leader;

    //         // Удаляем игрока из клана
    //         await ClanRedisService.removeClanMemberFromRedis(clanId, data.player_id);
    //         const totalMembers = Math.max(0, clan.player_count - 1);

    //         // Пересчитываем max_players, если нужно
    //         const membersPoints = members.reduce((sum, m) => sum + (m.clan_points || 0), 0) - (member.clan_points || 0);
    //         const currentLevel = getClanLevelByPoints(membersPoints);
    //         const maxPlayers = calculateMaxPlayers(25, currentLevel); // или базовое max_players из клана

    //         await ClanRedisService.updateClanInRedis(clanId, { 
    //             player_count: totalMembers,
    //             max_players
    //         });

    //         await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
    //         await updatePlayerInRedis(data.player_id, { clan_id: null, clan_name: null, clan_points: 0 });

    //         let clanDeleted = false;

    //         if (isLeader) {
    //             const remainingMembers = await ClanRedisService.getClanMembersFromRedis(clanId);
    //             if (remainingMembers.length > 0) {
    //                 const newLeader = remainingMembers[0];
    //                 await ClanRedisService.updateClanInRedis(clanId, {
    //                     leader_id: newLeader.player_id,
    //                     leader_name: newLeader.player_name
    //                 });
    //                 await ClanRedisService.updateClanMemberLeaderFlag(clanId, newLeader.player_id);
    //             } else {
    //                 await ClanRedisService.removeClanFromRedis(clanId);
    //                 clanDeleted = true;
    //             }
    //         }

    //         ws.send(JSON.stringify({
    //             action: 'leave_clan_response',
    //             success: true,
    //             player_id: data.player_id,
    //             was_leader: isLeader,
    //             clan_deleted: clanDeleted,
    //             clan_name: clan.clan_name
    //         }));

    //     } catch (err) {
    //         console.error('Leave clan error:', err);
    //         sendError(ws, err.message);
    //     }
    // }
    // async handleLeaveClan(ws, data) {
    //     const requiredFields = ['player_id'];
    //     if (!data || requiredFields.some(f => !data[f])) 
    //         return sendError(ws, 'Missing player_id');

    //     try {
    //         // Получаем профиль игрока
    //         const playerProfileRaw = await playerRedisService.getPlayerFromRedis(data.player_id);
    //         if (!playerProfileRaw || !playerProfileRaw.clan_id) 
    //             return sendError(ws, 'Player not in any clan');

    //         // Приводим к обычному объекту (убираем null prototype)
    //         const playerProfile = { ...playerProfileRaw };

    //         const clanId = playerProfile.clan_id.toString();

    //         // Берем клан
    //         const clan = await ClanRedisService.getClanFromRedis(clanId);
    //         if (!clan) {
    //             await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
    //             return sendError(ws, 'Clan not found');
    //         }

    //         // Получаем участников клана
    //         const members = await ClanRedisService.getClanMembersFromRedis(clanId);
    //         const member = members.find(m => m.player_id === data.player_id);
    //         if (!member) {
    //             await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
    //             return sendError(ws, 'Player not found in clan members');
    //         }

    //         const isLeader = member.is_leader;

    //         // Удаляем игрока из клана
    //         await ClanRedisService.removeClanMemberFromRedis(clanId, data.player_id);
    //         await ClanRedisService.updateClanInRedis(clanId, { 
    //             player_count: Math.max(0, clan.player_count - 1) 
    //         });
    //         await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
    //         await updatePlayerInRedis(data.player_id, { 
    //             clan_id: null, 
    //             clan_name: null, 
    //             clan_points: 0 
    //         });

    //         let clanDeleted = false;

    //         if (isLeader) {
    //             const remainingMembers = await ClanRedisService.getClanMembersFromRedis(clanId);
    //             if (remainingMembers.length > 0) {
    //                 const newLeader = remainingMembers[0];
    //                 await ClanRedisService.updateClanInRedis(clanId, {
    //                     leader_id: newLeader.player_id,
    //                     leader_name: newLeader.player_name
    //                 });
    //                 await ClanRedisService.updateClanMemberLeaderFlag(clanId, newLeader.player_id);
    //             } else {
    //                 await ClanRedisService.removeClanFromRedis(clanId);
    //                 clanDeleted = true;
    //             }
    //         }

    //         ws.send(JSON.stringify({
    //             action: 'leave_clan_response',
    //             success: true,
    //             player_id: data.player_id,
    //             was_leader: isLeader,
    //             clan_deleted: clanDeleted,
    //             clan_name: clan.clan_name
    //         }));

    //     } catch (err) {
    //         console.error('Leave clan error:', err);
    //         sendError(ws, err.message);
    //     }
    // }
    // async handleLeaveClan(ws, data) {
    //     const requiredFields = ['player_id'];
    //     if (!data || requiredFields.some(f => !data[f])) 
    //         return sendError(ws, 'Missing player_id');

    //     try {
    //         // Получаем профиль игрока
    //         const playerProfileRaw = await playerRedisService.getPlayerFromRedis(data.player_id);
    //         if (!playerProfileRaw || !playerProfileRaw.clan_id) 
    //             return sendError(ws, 'Player not in any clan');

    //         const playerProfile = { ...playerProfileRaw };
    //         const clanId = playerProfile.clan_id.toString();

    //         // Берем клан
    //         const clan = await ClanRedisService.getClanFromRedis(clanId);
    //         if (!clan) {
    //             await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
    //             return sendError(ws, 'Clan not found');
    //         }

    //         // Получаем участников
    //         const members = await ClanRedisService.getClanMembersFromRedis(clanId);
    //         const member = members.find(m => m.player_id === data.player_id);
    //         if (!member) {
    //             await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
    //             return sendError(ws, 'Player not found in clan members');
    //         }

    //         const isLeader = member.is_leader;

    //         // Удаляем игрока из клана
    //         await ClanRedisService.removeClanMemberFromRedis(clanId, data.player_id);
    //         await ClanRedisService.updateClanInRedis(clanId, { 
    //             player_count: Math.max(0, clan.player_count - 1) 
    //         });
    //         await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
    //         await updatePlayerInRedis(data.player_id, { 
    //             clan_id: null, 
    //             clan_name: null, 
    //             clan_points: 0 
    //         });

    //         // Проверка оставшихся участников после удаления
    //         const remainingMembers = await ClanRedisService.getClanMembersFromRedis(clanId);
    //         let clanDeleted = false;

    //         if (remainingMembers.length === 0) {
    //             await ClanRedisService.removeClanFromRedis(clanId);
    //             clanDeleted = true;
    //         } else if (isLeader) {
    //             // Назначаем нового лидера, если уходил лидер
    //             const newLeader = remainingMembers[0];
    //             await ClanRedisService.updateClanInRedis(clanId, {
    //                 leader_id: newLeader.player_id,
    //                 leader_name: newLeader.player_name
    //             });
    //             await ClanRedisService.updateClanMemberLeaderFlag(clanId, newLeader.player_id);
    //         }

    //         ws.send(JSON.stringify({
    //             action: 'leave_clan_response',
    //             success: true,
    //             player_id: data.player_id,
    //             was_leader: isLeader,
    //             clan_deleted: clanDeleted,
    //             clan_name: clan.clan_name
    //         }));

    //     } catch (err) {
    //         console.error('Leave clan error:', err);
    //         sendError(ws, err.message);
    //     }
    // }
    async handleLeaveClan(ws, data) {
        const requiredFields = ['player_id'];
        if (!data || requiredFields.some(f => !data[f])) 
            return sendError(ws, 'Missing player_id');

        const client = await db.connect();
        let clanDeleted = false;

        try {
            // Получаем профиль игрока
            const playerProfileRaw = await playerRedisService.getPlayerFromRedis(data.player_id);
            if (!playerProfileRaw || !playerProfileRaw.clan_id) 
                return sendError(ws, 'Player not in any clan');

            const playerProfile = { ...playerProfileRaw };
            const clanId = playerProfile.clan_id.toString();

            // Берем клан
            const clan = await ClanRedisService.getClanFromRedis(clanId);
            if (!clan) {
                await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
                return sendError(ws, 'Clan not found');
            }

            // Получаем участников клана
            const members = await ClanRedisService.getClanMembersFromRedis(clanId);
            const member = members.find(m => m.player_id === data.player_id);
            if (!member) {
                await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
                return sendError(ws, 'Player not found in clan members');
            }

            const isLeader = member.is_leader;

            // Удаляем игрока из клана в Redis
            await ClanRedisService.removeClanMemberFromRedis(clanId, data.player_id);
            await ClanRedisService.updateClanInRedis(clanId, { 
                player_count: Math.max(0, clan.player_count - 1) 
            });
            await ClanRedisService.setPlayerClanInRedis(data.player_id, null);
            await updatePlayerInRedis(data.player_id, { 
                clan_id: null, 
                clan_name: null, 
                clan_points: 0 
            });

            // Проверяем оставшихся участников
            const remainingMembers = await ClanRedisService.getClanMembersFromRedis(clanId);

            if (remainingMembers.length === 0) {
                // Нет участников — удаляем клан из Redis и БД
                clanDeleted = true;
                await ClanRedisService.removeClanFromRedis(clanId);

                await client.query('BEGIN');
                await client.query('DELETE FROM clans WHERE clan_id=$1', [clanId]);
                await client.query('DELETE FROM clan_members WHERE clan_id=$1', [clanId]);
                await client.query('COMMIT');

            } else if (isLeader) {
                // Назначаем нового лидера
                const newLeader = remainingMembers[0];
                await ClanRedisService.updateClanInRedis(clanId, {
                    leader_id: newLeader.player_id,
                    leader_name: newLeader.player_name
                });
                await ClanRedisService.updateClanMemberLeaderFlag(clanId, newLeader.player_id);
            }

            ws.send(JSON.stringify({
                action: 'leave_clan_response',
                success: true,
                player_id: data.player_id,
                was_leader: isLeader,
                clan_deleted: clanDeleted,
                clan_name: clan.clan_name
            }));

        } catch (err) {
            await client.query('ROLLBACK');
            console.error('Leave clan error:', err);
            sendError(ws, err.message);
        } finally {
            client.release();
        }
    }

    // Добавление игрока в клан
    async handleJoinClan(ws, data) {
        const requiredFields = ['player_id', 'clan_id', 'player_name'];
        if (!data || requiredFields.some(f => !data[f])) return sendError(ws, 'Missing required fields');

        try {
            const clan = await ClanRedisService.getClanFromRedis(data.clan_id);
            if (!clan) return sendError(ws, 'Clan not found');

            const members = await ClanRedisService.getClanMembersFromRedis(data.clan_id);

            // Добавляем нового игрока
            const memberData = { 
                player_id: data.player_id, 
                player_name: data.player_name, 
                is_leader: false 
            };
            await ClanRedisService.addClanMemberToRedis(data.clan_id, memberData);

            // Обновляем клан
            await ClanRedisService.updateClanInRedis(data.clan_id, { player_count: members.length + 1 });
            await ClanRedisService.setPlayerClanInRedis(data.player_id, data.clan_id);
            await updatePlayerInRedis(data.player_id, { clan_id: data.clan_id, clan_name: clan.clan_name });

            ws.send(JSON.stringify({
                action: 'join_clan_response',
                success: true,
                player_id: data.player_id,
                clan_id: data.clan_id,
                clan_name: clan.clan_name
            }));

        } catch (err) {
            console.error('Join clan error:', err);
            sendError(ws, err.message);
        }
    }

    // Получить информацию о клане
    // async handleGetClanInfo(ws, data) {
    //     try {
    //         if (!data.clan_id && !data.clan_name) {
    //             return sendError(ws, 'Provide either clan_id or clan_name');
    //         }
    //         let clanId = data.clan_id?.toString();

    //         // 1️⃣ Берем клан из Redis
    //         const clan = await ClanRedisService.getClanFromRedis(clanId);
    //         if (!clan) return sendError(ws, 'Clan not found');

    //         // 2️⃣ Берем участников из Redis
    //         const members = await ClanRedisService.getClanMembersFromRedis(clan.clan_id);

    //         // 3️⃣ Вычисляем прогресс до следующего уровня
    //         const nextLevelThreshold = getNextLevelThreshold(clan.clan_level);
    //         const levelProgress = nextLevelThreshold 
    //             ? Math.min(100, Math.round((clan.clan_points / nextLevelThreshold) * 100))
    //             : 100;

    //         // 4️⃣ Формируем ответ в старой структуре
    //         const response = {
    //             action: 'get_clan_info_response',
    //             success: true,
    //             clan: {
    //                 id: clan.clan_id,
    //                 name: clan.clan_name,
    //                 leader: {
    //                     id: clan.leader_id,
    //                     name: clan.leader_name,
    //                     rating: clan.leader_rating || 0,
    //                     // best_rating: clan.leader_best_rating || 0,
    //                     leader_clan_points: clan.clan_points || 0
    //                 },
    //                 stats: {
    //                     place: clan.place || 0,
    //                     player_count: clan.player_count,
    //                     max_players: clan.max_players,
    //                     current_level: clan.clan_level,
    //                     next_level_progress: levelProgress,
    //                     need_rating: clan.need_rating,
    //                     is_open: clan.is_open,
    //                     points_valid: true,
    //                     points_breakdown: null
    //                 }
    //             },
    //             members: members.map(m => ({
    //                 id: m.player_id,
    //                 name: m.player_name,
    //                 is_leader: m.is_leader,
    //                 stats: {
    //                     rating: m.rating,
    //                     clan_points: m.clan_points,
    //                     // kills: m.kills || 0,
    //                     // matches: m.matches || 0,
    //                     // wins: m.wins || 0,
    //                     contribution_percent: clan.clan_points > 0 
    //                         ? Math.round((m.clan_points / clan.clan_points) * 100)
    //                         : 0
    //                 }
    //             }))
    //         };

    //         ws.send(JSON.stringify(response));

    //     } catch (err) {
    //         console.error('Get clan info error:', err);
    //         sendError(ws, err.message);
    //     }
    // }
    // async handleGetClanInfo(ws, data) {
    //     try {
    //         if (!data.clan_id && !data.clan_name) {
    //             return sendError(ws, 'Provide either clan_id or clan_name');
    //         }
    //         let clanId = data.clan_id?.toString();

    //         // 1️⃣ Берем клан из Redis
    //         const clan = await ClanRedisService.getClanFromRedis(clanId);
    //         if (!clan) return sendError(ws, 'Clan not found');

    //         // 2️⃣ Берем участников из Redis
    //         const members = await ClanRedisService.getClanMembersFromRedis(clan.clan_id);

    //         // 2.1️⃣ Считаем сумму очков всех участников
    //         const totalClanPoints = members.reduce((sum, m) => sum + (m.clan_points || 0), 0);

    //         // 2.2️⃣ Пересчитываем максимальное количество участников по уровню
    //         let maxPlayers = clan.max_players || 25;
    //         const extraPlayers = Math.floor(clan.clan_level / 2) * 5;
    //         maxPlayers += extraPlayers;

    //         // 3️⃣ Вычисляем прогресс до следующего уровня
    //         const nextLevelThreshold = getNextLevelThreshold(clan.clan_level);
    //         const levelProgress = nextLevelThreshold 
    //             ? Math.min(100, Math.round((totalClanPoints / nextLevelThreshold) * 100))
    //             : 100;

    //         // 4️⃣ Формируем ответ в старой структуре
    //         const response = {
    //             action: 'get_clan_info_response',
    //             success: true,
    //             clan: {
    //                 id: clan.clan_id,
    //                 name: clan.clan_name,
    //                 leader: {
    //                     id: clan.leader_id,
    //                     name: clan.leader_name,
    //                     rating: clan.leader_rating || 0,
    //                     leader_clan_points: members.find(m => m.is_leader)?.clan_points || 0
    //                 },
    //                 stats: {
    //                     place: clan.place || 0,
    //                     player_count: clan.player_count,
    //                     max_players: clan.max_players,
    //                     current_level: clan.clan_level,
    //                     next_level_progress: levelProgress,
    //                     need_rating: clan.need_rating,
    //                     is_open: clan.is_open,
    //                     points_valid: true,
    //                     points_breakdown: null,
    //                     clan_points: totalClanPoints // <-- сумма очков всех участников
    //                 }
    //             },
    //             members: members.map(m => ({
    //                 id: m.player_id,
    //                 name: m.player_name,
    //                 is_leader: m.is_leader,
    //                 stats: {
    //                     rating: m.rating,
    //                     clan_points: m.clan_points,
    //                     contribution_percent: totalClanPoints > 0 
    //                         ? Math.round((m.clan_points / totalClanPoints) * 100)
    //                         : 0
    //                 }
    //             }))
    //         };

    //         ws.send(JSON.stringify(response));

    //     } catch (err) {
    //         console.error('Get clan info error:', err);
    //         sendError(ws, err.message);
    //     }
    // }
    async handleGetClanInfo(ws, data) {
        try {
            if (!data.clan_id && !data.clan_name) {
                return sendError(ws, 'Provide either clan_id or clan_name');
            }
            let clanId = data.clan_id?.toString();

            // 1️⃣ Берем клан из Redis
            const clan = await ClanRedisService.getClanFromRedis(clanId);
            if (!clan) return sendError(ws, 'Clan not found');

            // 2️⃣ Берем участников из Redis
            const members = await ClanRedisService.getClanMembersFromRedis(clan.clan_id);

            // 2.1️⃣ Считаем сумму очков всех участников
            const totalClanPoints = members.reduce((sum, m) => sum + (m.clan_points || 0), 0);

            // 2.2️⃣ Пересчитываем уровень клана по сумме очков
            const clanLevel = getClanLevelByPoints(totalClanPoints);

            // 3️⃣ Вычисляем прогресс до следующего уровня
            const nextLevelThreshold = getNextLevelThreshold(clan.clan_level);
            const levelProgress = nextLevelThreshold 
                ? Math.min(100, Math.round((totalClanPoints / nextLevelThreshold) * 100))
                : 100;

            // 3.1️⃣ Пересчет max_players каждые 2 уровня
            const max_players = calculateMaxPlayers(clan.max_players || 25, clanLevel);

            // 4️⃣ Формируем ответ
            const response = {
                action: 'get_clan_info_response',
                success: true,
                clan: {
                    id: clan.clan_id,
                    name: clan.clan_name,
                    leader: {
                        id: clan.leader_id,
                        name: clan.leader_name,
                        rating: clan.leader_rating || 0,
                        leader_clan_points: members.find(m => m.is_leader)?.clan_points || 0
                    },
                    stats: {
                        current_level: clanLevel,        // <-- добавили
                        place: clan.place || 0,
                        player_count: clan.player_count,
                        max_players,                            // <-- пересчитано
                        next_level_progress: levelProgress,
                        need_rating: clan.need_rating,
                        is_open: clan.is_open,
                        points_valid: true,
                        points_breakdown: null,
                        clan_points: totalClanPoints           // <-- сумма очков участников
                    }
                },
                members: members.map(m => ({
                    id: m.player_id,
                    name: m.player_name,
                    is_leader: m.is_leader,
                    stats: {
                        rating: m.rating,
                        clan_points: m.clan_points,
                        contribution_percent: totalClanPoints > 0 
                            ? Math.round((m.clan_points / totalClanPoints) * 100)
                            : 0
                    }
                }))
            };

            ws.send(JSON.stringify(response));

        } catch (err) {
            console.error('Get clan info error:', err);
            sendError(ws, err.message);
        }
    }
    
}

function getNextLevelThreshold(currentLevel) {
    const thresholds = {
        0: 150,
        1: 500,
        2: 2000,
        3: 5000,
        4: 10000,
        5: 20000,
        6: 35000,
        7: 60000,
        8: 100000,
        9: 150000,
        10: null // Максимальный уровень
    };
    return thresholds[currentLevel] || null;
}

function getClanLevelByPoints(points) {
    const thresholds = [150, 500, 2000, 5000, 10000, 20000, 35000, 60000, 100000, 150000];
    let level = 0;
    for (let i = 0; i < thresholds.length; i++) {
        if (points >= thresholds[i]) {
            level = i + 1;
        } else {
            break;
        }
    }
    return level;
}

function calculateMaxPlayers(baseMax = 25, level) {
    const extra = Math.floor(level / 2) * 5; // +5 каждые 2 уровня
    return baseMax + extra;
}

module.exports = new ClanController();