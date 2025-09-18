using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using System.Collections;
using System;

#region Class struct
[Serializable]
public class RoomInfo
{
    public string id;
    public int mode;
    public string state;
    [ShowInInspector] public Dictionary<string, PlayerInGameInfo> players;
    public List<string> bots;
    public int playerCount;
    public int botCount;
    public int maxPlayers;
    public long startTime;
    public string matchId;
    public List<MatchPlayerResult> lastMatchResults;

    public RoomInfo()
    {
        players = new Dictionary<string, PlayerInGameInfo>();
        bots = new List<string>();
        lastMatchResults = new List<MatchPlayerResult>();
    }

    public void AddPlayer(PlayerInGameInfo playerInfo)
    {
        if (!players.ContainsKey(playerInfo.playerId))
        {
            players.Add(playerInfo.playerId, playerInfo);
        }
    }

    public void RemovePlayer(string playerId)
    {
        if (players.ContainsKey(playerId))
        {
            players.Remove(playerId);
        }
    }

    public PlayerInGameInfo GetPlayer(string playerId)
    {
        return players.ContainsKey(playerId) ? players[playerId] : null;
    }

    public bool ContainsPlayer(string playerId)
    {
        return players.ContainsKey(playerId);
    }

    public List<string> GetPlayerIds()
    {
        return new List<string>(players.Keys);
    }

    public List<PlayerInGameInfo> GetAllPlayers()
    {
        return new List<PlayerInGameInfo>(players.Values);
    }
}

[Serializable]
public class PlayerInGameInfo
{
    public string playerId;
    public string player_name;
    public int rating;
    public int hero_id;
    public int hero_skin;
    public bool isReady;
    public float noizeRadius;
    public string current_weapon;
    public Vector3 position;
    public Quaternion rotation;
    public string animationState;
    public bool isAlive;
    public int kills;
    public int deaths;
    public int hero_rank;
    public int hero_level;
    public float hp;
    public float armor;
    public float max_hp;
    public float max_armor;
    public BoolsState boolsState;

    public PlayerInGameInfo(string playerId, string player_name, int rating = -1, int heroId = -1)
    {
        this.playerId = playerId;
        this.player_name = player_name;
        this.rating = rating;
        hero_id = heroId;

        boolsState = new();
    }

    public void UpdateTransform(Vector3 newPosition, Quaternion newRotation, string newAnimation)
    {
        position = newPosition;
        rotation = newRotation;
        animationState = newAnimation;
    }

    public void ResetStats()
    {
        isAlive = true;
        kills = 0;
        deaths = 0;
        position = Vector3.zero;
        rotation = Quaternion.identity;
    }
}

[Serializable]
public class MatchPlayerResult
{
    public string player_id;
    public string player_name;
    public int kills;
    public int deaths;
    public float damage;
    public float score;
    public int place;
    public bool is_winner;
}

[Serializable]
public class PlayerTransformUpdate
{
    public string player_id;
    public Vector3 position;
    public Quaternion rotation;
    public string animation;
}
[Serializable]
public class PlayerStatsUpdate
{
    public string player_id;
    public int kills;
    public int deaths;
    public bool is_alive;
}

[Serializable]
public class MatchmakingStats
{
    public int totalRooms;
    public int waiting;
    public int countdown;
    public int inProgress;
    public int completed;
    public int totalPlayers;
    public int totalBots;
}

[Serializable]
public class RoomJoinedResponse
{
    public string room_id;
    public int players_in_room;
    public int max_players;
    public long estimated_wait;
}

[Serializable]
public class PlayerLeftRoomResponse
{
    public string player_id;
    public string room_id;
    public int players_remaining;
}

[Serializable]
public class MatchStartResponse
{
    public string room_id;
    public string match_id;
    public List<PlayerInGameInfo> players;
    public List<string> bots;
}

[Serializable]
public class MatchEndResponse
{
    public string room_id;
    public string match_id;
    public List<MatchPlayerResult> results;
}

[Serializable]
public class RoomForceClosedResponse
{
    public string room_id;
    public string reason;
}

[Serializable]
public class GameForceEndedResponse
{
    public string room_id;
    public string reason;
}

[Serializable]
public class MatchmakingFullResponse
{
    public string message;
}
#endregion
public class OnlineRoom : MonoBehaviour
{
    public static OnlineRoom Instance { get; private set; }

    [Header("Referencess")]
    public Player player;

    [field: SerializeField] public RoomInfo CurrentRoom { get; private set; }
    public bool IsInRoom => CurrentRoom != null;
    public bool IsInMatch => IsInRoom && CurrentRoom.state == "in_progress";

    private WaitForSeconds _waitForSeconds0_1 = new WaitForSeconds(0.1f);
    private Coroutine serverCor;

    private bool matchStart = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Destroy(gameObject);
        }

        // Подписываемся на события WebSocketBase
        WebSocketBase.Instance.OnMatchmakingJoined += HandleRoomJoined;
        WebSocketBase.Instance.OnPlayerLeftRoom += HandlePlayerLeftRoom;
        WebSocketBase.Instance.OnMatchStart += HandleMatchStart;
        WebSocketBase.Instance.OnMatchEnd += HandleMatchEnd;
        WebSocketBase.Instance.OnRoomForceClosed += HandleRoomForceClosed;
        WebSocketBase.Instance.OnGameForceEnded += HandleGameForceEnded;
        WebSocketBase.Instance.OnRoomInfoReceived += HandleRoomInfoReceived;
        WebSocketBase.Instance.OnLeaveRoomResponse += HandleLeaveRoomResponse;

        WebSocketBase.Instance.OnPlayerTransformUpdateResponse += HandlePlayerTransformUpdateResponse;
        WebSocketBase.Instance.OnPlayerStatsUpdateResponse += HandlePlayerStatsUpdateResponse;

        WebSocketBase.Instance.OnPlayerDamaged += HandlePlayerDamaged;

        WebSocketBase.Instance.OnBoostTaken += HandleBoostTaken;

        WebSocketBase.Instance.OnHealPlayer += HandleHealPlayer;
        WebSocketBase.Instance.OnPlayerRespawned += HandlePlayerRespawned;

        WebSocketBase.Instance.OnPlayerLeftRoom += HandlePlayerLeft;
    }

    private void Start()
    {

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log(Geekplay.Instance.PlayerData.persons[Geekplay.Instance.PlayerData.currentHero].currentBody);
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от событий при уничтожении объекта
        if (WebSocketBase.Instance != null)
        {
            WebSocketBase.Instance.OnMatchmakingJoined -= HandleRoomJoined;
            WebSocketBase.Instance.OnPlayerLeftRoom -= HandlePlayerLeftRoom;
            WebSocketBase.Instance.OnMatchStart -= HandleMatchStart;
            WebSocketBase.Instance.OnMatchEnd -= HandleMatchEnd;
            WebSocketBase.Instance.OnRoomForceClosed -= HandleRoomForceClosed;
            WebSocketBase.Instance.OnGameForceEnded -= HandleGameForceEnded;
            WebSocketBase.Instance.OnRoomInfoReceived -= HandleRoomInfoReceived;
            WebSocketBase.Instance.OnLeaveRoomResponse -= HandleLeaveRoomResponse;

            WebSocketBase.Instance.OnPlayerTransformUpdateResponse -= HandlePlayerTransformUpdateResponse;
            WebSocketBase.Instance.OnPlayerStatsUpdateResponse -= HandlePlayerStatsUpdateResponse;

            WebSocketBase.Instance.OnPlayerDamaged -= HandlePlayerDamaged;

            WebSocketBase.Instance.OnBoostTaken -= HandleBoostTaken;

            WebSocketBase.Instance.OnHealPlayer -= HandleHealPlayer;
            WebSocketBase.Instance.OnPlayerRespawned -= HandlePlayerRespawned;

            WebSocketBase.Instance.OnPlayerLeftRoom -= HandlePlayerLeft;
        }

        if (serverCor != null)
        {
            StopCoroutine(serverCor);
            serverCor = null;
        }
    }

    public void StartServerUpdate()
    {
        if (serverCor != null)
        {
            StopCoroutine(serverCor);
            serverCor = null;
        }
        serverCor = StartCoroutine(UpdateServerInfo());
    }

    // Обработчики событий WebSocket
    private void HandleRoomJoined(RoomJoinedResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            var roomInfo = new RoomInfo
            {
                id = response.room_id,
                players = new Dictionary<string, PlayerInGameInfo>(),
                bots = new List<string>(),
                playerCount = response.players_in_room,
                maxPlayers = response.max_players,
                state = "waiting"
            };
            SetCurrentRoom(roomInfo);
        });
    }

    // Обработчик выхода игрока
    private void HandlePlayerLeftRoom(PlayerLeftRoomResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            // if (IsInRoom && CurrentRoom.id == response.room_id)
            // {
                var enemy = EnemiesInGame.Instance.GetEnemy(response.player_id);
                Destroy(enemy.gameObject);
                CurrentRoom.RemovePlayer(response.player_id);
                Geekplay.Instance.PlayerData.roomId = null;
                Geekplay.Instance.Save();
                Debug.Log($"Player {response.player_id} left room {response.room_id}");
            // }
        });
    }

    // Обновленный обработчик начала матча
    private void HandleMatchStart(MatchStartResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Debug.Log(1);
            if (IsInRoom && CurrentRoom.id == response.room_id)
            {
                Debug.Log(2);
                CurrentRoom.state = "in_progress";
                CurrentRoom.matchId = response.match_id;
                matchStart = true;
                StartServerUpdate();

                // Очищаем и заполняем словарь игроков
                CurrentRoom.players.Clear();
                foreach (var playerInfo in response.players)
                {
                    Debug.Log(playerInfo.player_name);
                    CurrentRoom.AddPlayer(playerInfo);
                }

                player.Controller.enabled = true;
                CurrentRoom.bots = response.bots;
                Debug.Log($"Match started in room {response.room_id} with {CurrentRoom.playerCount} players");
            }
        });
    }


    private void HandleMatchEnd(MatchEndResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            if (!IsInRoom || CurrentRoom.id != response.room_id)
            {
                return;
            }

            matchStart = false;
            if (serverCor != null)
            {
                StopCoroutine(serverCor);
                serverCor = null;
            }

            CurrentRoom.state = "completed";
            CurrentRoom.matchId = response.match_id;
            CurrentRoom.lastMatchResults = response.results ?? new List<MatchPlayerResult>();

            var localPlayerId = Geekplay.Instance.PlayerData.id;
            bool isWinner = CurrentRoom.lastMatchResults.Any(result => result.player_id == localPlayerId && result.is_winner);

            // if (response.results localPlayerId)
            // {

            // }
            for (int i = 0; i < response.results.Count; i++)
            {
                var result = response.results[i];

                if (result.player_id == localPlayerId)
                {
                    player.overallKills = result.kills;
                }
            }

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.matchState = isWinner ? MatchState.win : MatchState.lose;
            }

            Debug.Log($"Match {response.match_id} ended. Local player {(isWinner ? "won" : "lost")}.");
        });
    }

    private void HandleRoomForceClosed(RoomForceClosedResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            if (IsInRoom && CurrentRoom.id == response.room_id)
            {
                Debug.Log($"Room {response.room_id} force closed: {response.reason}");
                ClearCurrentRoom();
            }
        });
    }

    private void HandleGameForceEnded(GameForceEndedResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            if (IsInRoom && CurrentRoom.id == response.room_id)
            {
                Debug.Log($"Game force ended in room {response.room_id}: {response.reason}");
                ClearCurrentRoom();
            }
        });
    }

    private void HandleRoomInfoReceived(RoomInfoResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            if (response.success && response.room != null)
            {
                if (IsInRoom && CurrentRoom.id == response.room.id)
                {
                    UpdateRoomInfo(response.room);
                }
                else
                {
                    SetCurrentRoom(response.room);
                }
            }
        });
    }

    private void HandleLeaveRoomResponse(LeaveRoomResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Debug.Log("HandleLeaveRoomResponse");
            if (response.success && IsInRoom && CurrentRoom.id == response.room_id)
            {
                Debug.Log($"Successfully left room {response.room_id}");
                ClearCurrentRoom();
            }
        });
    }

    // Установка текущей комнаты
    public void SetCurrentRoom(RoomInfo roomInfo)
    {
        CurrentRoom = roomInfo;
        Debug.Log($"Joined room: {roomInfo.id}, Mode: {roomInfo.mode}, State: {roomInfo.state}");
    }

    // Очистка текущей комнаты
    public void ClearCurrentRoom()
    {
        if (CurrentRoom != null)
        {
            Debug.Log($"Left room: {CurrentRoom.id}");
            CurrentRoom = null;
        }
    }

    // Обновление информации о комнате
    public void UpdateRoomInfo(RoomInfo roomInfo)
    {
        if (CurrentRoom != null && CurrentRoom.id == roomInfo.id)
        {
            CurrentRoom = roomInfo;
            Debug.Log($"Room updated: {roomInfo.id}, Players: {roomInfo.playerCount}/{roomInfo.maxPlayers}");
        }
    }

    #region PlayersLogic
    // Вспомогательные методы
    private void UpdatePlayerVisualization(PlayerInGameInfo player)
    {
        if (player == null) return;
        if (player.playerId == Geekplay.Instance.PlayerData.id) return; // себя не трогаем

        Enemy enemy = EnemiesInGame.Instance.GetEnemy(player.playerId);
        if (enemy == null) return;

        enemy.SetNetworkState(player.position, player.rotation);
        if (player.current_weapon == "secondary")
        {
            enemy.currentWeapon.weapons[0].SetActive(true);
            enemy.currentWeapon.weapons[1].SetActive(false);
        }
        if (player.current_weapon == "main")
        {
            enemy.currentWeapon.weapons[1].SetActive(true);
            enemy.currentWeapon.weapons[0].SetActive(false);
        }
    }

    private void UpdatePlayerHp(WebSocketBase.PlayerDamagedResponse response)
    {
        if (response.target_id == Geekplay.Instance.PlayerData.id)
        {
            Debug.Log("Damage to us");
            var player = GetLocalPlayerInfo();
            player.hp = response.new_hp;

            Level.Instance.currentLevel.player.Character.Health.ChangeHp(response.new_hp);
            // Level.Instance.currentLevel.player.Character.Health.NewTakeDamage(response.damage);
        }
        else
        {
            Debug.Log("Damage to enemy");
            var player = GetPlayerInfo(response.target_id);
            player.hp = response.new_hp;

            Enemy enemy = EnemiesInGame.Instance.GetEnemy(response.target_id);
            // enemy.Health.ChangeHp(response.new_hp);
            enemy.Health.NewTakeDamage(response.damage);
        }
    }
    private void UpdatePlayerArmor(WebSocketBase.PlayerDamagedResponse response)
    {
        if (response.target_id == Geekplay.Instance.PlayerData.id)
        {
            var player = GetLocalPlayerInfo();
            player.armor = response.new_armor;

            Level.Instance.currentLevel.player.Character.Armor.ChangeArmor(response.new_armor);
        }
        else
        {
            var player = GetPlayerInfo(response.target_id);
            player.armor = response.new_armor;

            Enemy enemy = EnemiesInGame.Instance.GetEnemy(response.target_id);
            enemy.Armor.ChangeArmor(response.new_armor);
        }
    }

    public PlayerInGameInfo GetLocalPlayerInfo()
    {
        string localPlayerId = Geekplay.Instance.PlayerData.id;
        return IsInRoom ? CurrentRoom.GetPlayer(localPlayerId) : null;
    }

    public PlayerInGameInfo GetPlayerInfo(string playerId)
    {
        return IsInRoom ? CurrentRoom.GetPlayer(playerId) : null;
    }

    public List<PlayerInGameInfo> GetAllPlayers()
    {
        return IsInRoom ? CurrentRoom.GetAllPlayers() : new List<PlayerInGameInfo>();
    }

    public List<PlayerInGameInfo> GetAlivePlayers()
    {
        return IsInRoom ?
            CurrentRoom.GetAllPlayers().Where(p => p.isAlive).ToList() :
            new List<PlayerInGameInfo>();
    }

    public void UpdateLocalPlayerStats(int kills, int deaths, bool isAlive)
    {
        var localPlayer = GetLocalPlayerInfo();
        if (localPlayer != null)
        {
            localPlayer.kills = kills;
            localPlayer.deaths = deaths;
            localPlayer.isAlive = isAlive;

            // Отправляем на сервер
            WebSocketBase.Instance.SendPlayerStatsUpdate(
                kills,
                deaths,
                isAlive
            );
        }
    }
    public void ResetAllPlayers()
    {
        if (IsInRoom)
        {
            foreach (var player in CurrentRoom.GetAllPlayers())
            {
                player.ResetStats();
            }
        }
    }
    #endregion






    #region UpdateServerInfo
    private IEnumerator UpdateServerInfo()
    {
        while (true && player != null && matchStart)
        {
            WebSocketBase.Instance.SendPlayerTransformUpdate(player.Controller.transform.position,
                                                            player.Controller.transform.rotation);
            yield return _waitForSeconds0_1;
        }
    }

    // Обработчик ответа с трансформами игроков
    private void HandlePlayerTransformUpdateResponse(PlayerTransformUpdateResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            // Debug.Log($"Received transform update - Success: {response.success}\n   My transform: {response.my_transform != null}\n   Other transforms: {response.other_transforms?.Count}");

            if (response.success)
            {
                // Обрабатываем свои собственные данные
                if (response.my_transform != null)
                {
                    UpdatePlayerTransform(response.my_transform);
                }

                // Обрабатываем данные других игроков
                if (response.other_transforms != null && response.other_transforms.Count > 0)
                {
                    UpdateOtherPlayersTransforms(response.other_transforms);
                    UpdateOtherPlayersAnimations(response.other_transforms);
                    UpdateOtherPlayersNoizes(response.other_transforms);
                }
            }
            else
            {
                Debug.LogWarning("Transform update failed");
            }
        });
    }

    private void UpdateOtherPlayersTransforms(List<PlayerTransformData> transforms)
    {
        // Debug.Log("UpdateOtherPlayersTransforms");
        if (!IsInRoom) return;

        foreach (var transformData in transforms)
        {
            if (transformData == null) continue;

            // Пропускаем своего игрока (его данные приходят отдельно)
            if (transformData.player_id == Geekplay.Instance.PlayerData.id)
                continue;

            // Находим игрока в комнате
            var player = CurrentRoom.GetPlayer(transformData.player_id);

            if (player != null)
            {
                // Обновляем данные существующего игрока
                player.position = transformData.position.ToVector3();
                player.rotation = transformData.rotation.ToQuaternion();
                player.isAlive = transformData.is_alive;
                player.current_weapon = transformData.current_weapon;

                // Обновляем визуальное представление
                UpdatePlayerVisualization(player);

                // Debug.Log($"Updated other player: {player.player_name}");
            }
            else
            {
                // Если игрок не найден в комнате, создаем нового
                var newPlayer = new PlayerInGameInfo(
                    transformData.player_id,
                    transformData.username,
                    1000, // default rating
                    transformData.hero_id // используем hero_id из трансформа
                )
                {
                    position = transformData.position.ToVector3(),
                    rotation = transformData.rotation.ToQuaternion(),
                    isAlive = transformData.is_alive
                };

                CurrentRoom.AddPlayer(newPlayer);
                UpdatePlayerVisualization(newPlayer);

                Debug.Log($"Added new player from transform: {transformData.username}");
            }
        }
    }

    private void UpdateOtherPlayersAnimations(List<PlayerTransformData> transforms)
    {
        // Debug.Log("UpdateOtherPlayersAnimations");
        if (!IsInRoom) return;

        foreach (var transformData in transforms)
        {
            if (transformData == null) return;

            // Пропускаем своего игрока (его данные приходят отдельно)
            if (transformData.player_id == Geekplay.Instance.PlayerData.id)
                return;

            // Находим игрока в комнате
            var player = CurrentRoom.GetPlayer(transformData.player_id);
            var enemy = EnemiesInGame.Instance.GetEnemy(transformData.player_id);

            if (player != null)
            {
                Debug.Log(player);
                Debug.Log(player.boolsState);
                Debug.Log(transformData);
                Debug.Log(transformData.bools_state);
                player.boolsState.isMoving = transformData.bools_state.isMoving;
                player.boolsState.isDead = transformData.bools_state.isDead;
                player.boolsState.isHealing = transformData.bools_state.isHealing;
                player.boolsState.isPickingUp = transformData.bools_state.isPickingUp;
                player.boolsState.isReloading = transformData.bools_state.isReloading;
                player.boolsState.isShooting = transformData.bools_state.isShooting;
            }

            if (enemy != null)
            {
                // Булевые
                enemy.animator.SetBool("IsMoving", transformData.bools_state.isMoving);
                enemy.animator.SetBool("IsReload", transformData.bools_state.isReloading);
                enemy.animator.SetBool("IsShoot", transformData.bools_state.isShooting);
                // Триггеры
                if (transformData.bools_state.isReviving) enemy.animator.SetTrigger("Revive");
                if (transformData.bools_state.isPickingUp) enemy.animator.SetTrigger("");
                if (transformData.bools_state.isHealing) enemy.animator.SetTrigger("");
            }
        }
    }

    private void UpdateOtherPlayersNoizes(List<PlayerTransformData> transforms)
    {
        // Debug.Log("UpdateOtherPlayersNoizes");
        if (!IsInRoom) return;

        foreach (var transformData in transforms)
        {
            if (transformData == null) continue;

            // Пропускаем своего игрока (его данные приходят отдельно)
            if (transformData.player_id == Geekplay.Instance.PlayerData.id)
                continue;

            // Находим игрока в комнате
            var player = CurrentRoom.GetPlayer(transformData.player_id);
            var enemy = EnemiesInGame.Instance.GetEnemy(transformData.player_id);

            if (player != null)
            {
                player.noizeRadius = transformData.noize_volume;
                enemy.noizeVolume = transformData.noize_volume;
            }
        }
    }

    private void UpdatePlayerTransform(PlayerTransformData transformData)
    {
        if (!IsInRoom || transformData == null) return;

        // Находим своего игрока
        var localPlayerId = Geekplay.Instance.PlayerData.id;
        var player = CurrentRoom.GetPlayer(localPlayerId);

        if (player != null)
        {
            // Обновляем позицию и ротацию
            player.position = transformData.position.ToVector3();
            player.rotation = transformData.rotation.ToQuaternion();
            player.isAlive = transformData.is_alive;
            player.current_weapon = transformData.current_weapon;

            // Обновляем визуальное представление
            UpdatePlayerVisualization(player);

            // Debug.Log($"Updated local player transform: {player.player_name}");
        }
    }


    // Обработчик ответа со статистикой игрока
    private void HandlePlayerStatsUpdateResponse(PlayerStatsUpdateResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            if (response.success && IsInRoom && CurrentRoom.ContainsPlayer(response.player_id))
            {
                var player = CurrentRoom.GetPlayer(response.player_id);
                if (player != null)
                {
                    // Обновляем статистику игрока
                    player.kills = response.stats.kills;
                    player.deaths = response.stats.deaths;
                    player.isAlive = response.stats.is_alive;

                    Debug.Log($"Updated stats for {player.player_name}: K{player.kills}/D{player.deaths}");

                    // Обновляем визуальное представление
                    UpdatePlayerVisualization(player);
                }
            }
        });
    }

    // Обновляем информацию всех игроков
    private void UpdateAllPlayersInfo(List<PlayerInGameInfo> playersInfo)
    {
        Debug.Log("UpdateAllPlayersInfo");
        if (!IsInRoom)
        {
            Debug.Log(123);
            return;
        }

        foreach (var playerInfo in playersInfo)
        {
            // Находим существующего игрока или создаем нового
            var existingPlayer = CurrentRoom.GetPlayer(playerInfo.playerId);
            if (existingPlayer != null)
            {
                Debug.Log(1);
                // Обновляем данные существующего игрока
                UpdatePlayerInfo(existingPlayer, playerInfo);
            }
            else
            {
                Debug.Log(2);
                // Добавляем нового игрока в комнату
                CurrentRoom.AddPlayer(playerInfo);
                Debug.Log($"Added new player to room: {playerInfo.player_name}");
            }

            // Обновляем визуальное представление
            UpdatePlayerVisualization(playerInfo);
        }
    }

    // Обновляем информацию конкретного игрока
    private void UpdatePlayerInfo(PlayerInGameInfo existingPlayer, PlayerInGameInfo newPlayerInfo)
    {
        Debug.Log("UpdatePlayerInfo");
        existingPlayer.position = newPlayerInfo.position;
        existingPlayer.rotation = newPlayerInfo.rotation;
        existingPlayer.animationState = newPlayerInfo.animationState;
        existingPlayer.isAlive = newPlayerInfo.isAlive;
        existingPlayer.kills = newPlayerInfo.kills;
        existingPlayer.deaths = newPlayerInfo.deaths;
    }
    #endregion





    #region DamageLogic
    private void HandlePlayerDamaged(WebSocketBase.PlayerDamagedResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            // if (response.success)
            {
                if (!IsInRoom)
                {
                    return;
                }

                if (string.IsNullOrEmpty(response.target_id))
                {
                    Debug.LogWarning("Received player_damaged event without target_id. Ignoring.");
                    return;
                }

                var target = CurrentRoom.GetPlayer(response.target_id);
                if (target == null)
                {
                    Debug.Log("target == null");
                    return;
                }
                else
                {
                    Debug.Log($"Deal damage to {target.player_name}");
                }

                if (!string.IsNullOrEmpty(response.attacker_id) &&
                    // response.attacker_id != Geekplay.Instance.PlayerData.id &&
                    response.shot_origin != null &&
                    response.shot_direction != null)
                {
                    var attackerEnemy = EnemiesInGame.Instance.GetEnemy(response.attacker_id);
                    if (attackerEnemy != null)
                    {
                        attackerEnemy.PlayRemoteShot(response.shot_origin.ToVector3(),
                            response.shot_direction.ToVector3());
                    }
                }

                // if (response.attacker_id == Geekplay.Instance.PlayerData.id)
                // {
                //     if (response.new_hp <= 0)
                //     {
                //         player.overallKills++;
                //     }
                // }
                // Визуализация
                UpdatePlayerVisualization(target);

                UpdatePlayerArmor(response);
                UpdatePlayerHp(response);
            }
        });
    }
    #endregion






    #region BoostLogic
    public void HandleBoostTaken(BoostTakenResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            BoostsManager.Instance.UpdateVisualBoosts(response);
        });
    }
    #endregion

    #region Useable
    public void HandleHealPlayer(string playerId)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            var player = GetPlayerInfo(playerId);
            player.hp = player.max_hp;

            if (Geekplay.Instance.PlayerData.id == playerId)
            {
                Level.Instance.currentLevel.player.Character.Health.ChangeHp(player.max_hp);
            }
            else
            {
                var enemy = EnemiesInGame.Instance.GetEnemy(playerId);
                if (enemy != null)
                {
                    enemy.Health.ChangeHp(player.max_hp);
                }
            }
        });
    }
    #endregion

    private void HandlePlayerRespawned(WebSocketBase.PlayerRespawnedMessage response)
    {
        if (response == null)
        {
            return;
        }

        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Debug.Log("HandlePlayerRespawned");
            if (!IsInRoom)
            {
                return;
            }

            var player = CurrentRoom.GetPlayer(response.player_id);
            if (player == null)
            {
                return;
            }

            var stats = response.stats;
            if (stats != null)
            {
                player.isAlive = stats.is_alive;
                player.hp = stats.hp;
                player.max_hp = stats.max_hp;
                player.armor = stats.armor;
                player.max_armor = stats.max_armor;
                player.kills = stats.kills;
                player.deaths = stats.deaths;
                if (player.boolsState != null)
                {
                    player.boolsState.isDead = !stats.is_alive;
                }
            }

            if (response.player_id == Geekplay.Instance.PlayerData.id)
            {
                if (stats != null)
                {
                    var level = Level.Instance;
                    if (level != null && level.currentLevel != null && level.currentLevel.player != null)
                    {
                        var localPlayer = level.currentLevel.player;
                        localPlayer.Character.Health.state = HealthState.live;
                        localPlayer.Character.Health.FullHeal();
                        // if (!Mathf.Approximately(stats.hp, localPlayer.Character.Health.MaxHealth))
                        // {
                        localPlayer.Character.Health.ChangeHp(stats.hp);
                        // }
                        // localPlayer.Character.Armor.ChangeArmor(stats.armor);
                    }
                }
            }
            else if (stats != null)
            {
                var enemies = EnemiesInGame.Instance;
                if (enemies != null)
                {
                    var enemy = enemies.GetEnemy(response.player_id);
                    if (enemy != null)
                    {
                        enemy.Respawn(stats.hp, stats.armor);
                    }
                }
            }

            UpdatePlayerVisualization(player);
        });
    }

    private void HandlePlayerLeft(PlayerLeftRoomResponse response)
    {

    }

}