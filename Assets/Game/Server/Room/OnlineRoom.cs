using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using System.Collections;
using System;

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

    public RoomInfo()
    {
        players = new Dictionary<string, PlayerInGameInfo>();
        bots = new List<string>();
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
    public Vector3 position;
    public Quaternion rotation;
    public string animationState;
    public bool isAlive;
    public int kills;
    public int deaths;
    public int hero_rank;
    public int hero_level;

    public PlayerInGameInfo(string playerId, string player_name, int rating = -1, int heroId = -1)
    {
        this.playerId = playerId;
        this.player_name = player_name;
        this.rating = rating;
        this.hero_id = heroId;
        this.isReady = false;
        this.position = Vector3.zero;
        this.rotation = Quaternion.identity;
        this.animationState = "idle";
        this.isAlive = true;
        this.kills = 0;
        this.deaths = 0;
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
        WebSocketBase.Instance.OnRoomForceClosed += HandleRoomForceClosed;
        WebSocketBase.Instance.OnGameForceEnded += HandleGameForceEnded;
        WebSocketBase.Instance.OnRoomInfoReceived += HandleRoomInfoReceived;
        WebSocketBase.Instance.OnLeaveRoomResponse += HandleLeaveRoomResponse;

        WebSocketBase.Instance.OnPlayerTransformUpdateResponse += HandlePlayerTransformUpdateResponse;
        WebSocketBase.Instance.OnPlayerStatsUpdateResponse += HandlePlayerStatsUpdateResponse;

        WebSocketBase.Instance.OnPlayerDamaged += HandlePlayerDamaged;
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
            WebSocketBase.Instance.OnRoomForceClosed -= HandleRoomForceClosed;
            WebSocketBase.Instance.OnGameForceEnded -= HandleGameForceEnded;
            WebSocketBase.Instance.OnRoomInfoReceived -= HandleRoomInfoReceived;
            WebSocketBase.Instance.OnLeaveRoomResponse -= HandleLeaveRoomResponse;

            WebSocketBase.Instance.OnPlayerTransformUpdateResponse -= HandlePlayerTransformUpdateResponse;
            WebSocketBase.Instance.OnPlayerStatsUpdateResponse -= HandlePlayerStatsUpdateResponse;

            WebSocketBase.Instance.OnPlayerDamaged -= HandlePlayerDamaged;
        }

        if (serverCor != null)
        {
            StopCoroutine(serverCor);
            serverCor = null;
        }
    }

    public void StartServerUpdate()
    {
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
            if (IsInRoom && CurrentRoom.id == response.room_id)
            {
                CurrentRoom.RemovePlayer(response.player_id);
                Debug.Log($"Player {response.player_id} left room {response.room_id}");
            }
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

    public void UpdateLocalPlayerTransform(Vector3 position, Quaternion rotation, string animation)
    {
        var localPlayer = GetLocalPlayerInfo();
        if (localPlayer != null)
        {
            localPlayer.UpdateTransform(position, rotation, animation);

            // Отправляем на сервер
            WebSocketBase.Instance.SendPlayerTransformUpdate(
                position,
                rotation,
                animation
            );
        }
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
        while (true && player != null)
        {
            WebSocketBase.Instance.SendPlayerTransformUpdate(player.Controller.transform.position,
                                                            player.Controller.transform.rotation,
                                                            "poka xz");
            yield return _waitForSeconds0_1;
        }
    }

    // Обработчик ответа с трансформами игроков
    private void HandlePlayerTransformUpdateResponse(PlayerTransformUpdateResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Debug.Log($"Received transform update - Success: {response.success}, My transform: {response.my_transform != null}, Other transforms: {response.other_transforms?.Count}");

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
        Debug.Log("UpdateOtherPlayersTransforms");
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
                player.animationState = transformData.animation;
                player.isAlive = transformData.is_alive;

                // Обновляем визуальное представление
                UpdatePlayerVisualization(player);

                Debug.Log($"Updated other player: {player.player_name}");
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
                    animationState = transformData.animation,
                    isAlive = transformData.is_alive
                };

                CurrentRoom.AddPlayer(newPlayer);
                UpdatePlayerVisualization(newPlayer);

                Debug.Log($"Added new player from transform: {transformData.username}");
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
            player.animationState = transformData.animation;
            player.isAlive = transformData.is_alive;

            // Обновляем визуальное представление
            UpdatePlayerVisualization(player);

            Debug.Log($"Updated local player transform: {player.player_name}");
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

    #region DamageLogic
    private void HandlePlayerDamaged(WebSocketBase.PlayerDamagedResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            if (!IsInRoom) return;

            // 🔹 Проверка target_id
            if (string.IsNullOrEmpty(response.target_id))
            {
                Debug.LogWarning("Received player_damaged event without target_id. Ignoring.");
                return;
            }

            var player = CurrentRoom.GetPlayer(response.target_id);
            if (player == null) return;

            Debug.Log($"Deal damage to {player.player_name}");

            // Если это локальный игрок
            if (player.playerId == Geekplay.Instance.PlayerData.id)
            {
                Debug.Log($"You were hit by {response.attacker_id} for {response.amount} damage. New HP: {response.new_hp}");
                // Можно обновить визуальный HUD, эффекты и т.д.
            }

            // Обновляем HP игрока
            player.isAlive = response.new_hp > 0;
            // Можно добавить поле currentHP, если его нет:
            // player.currentHP = response.new_hp;

            // Визуализация
            UpdatePlayerVisualization(player);

            if (!player.isAlive)
            {
                Debug.Log($"{player.player_name} died from {response.attacker_id}");
                // Можно вызвать анимацию смерти и прочее
            }
        });
    }
    #endregion
    
    #endregion
}