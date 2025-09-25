using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

#region Все структуры
[Serializable]
public class PlayerStatsResponse
{
    public int rating;
    public int best_rating;
    public int money;
    public int donat_money;
    public int overral_kill;
    public int match_count;
    public int win_count;
    public int revive_count;
    public int max_damage;
    public int shoot_count;
}
[Serializable]
public class ClanInfoResponse
{
    public string clanId;
    public string clanName;
    public string myName;
    public int myRating;
    public int clanPoints;
    public int playerCount;
    public int maxPlayers;
    public bool isOpen;
    public int needRating;
    public List<ClanMember> members;
}

[Serializable]
public class GameModesStatus
{
    public GameModeStatus mode1;
    public GameModeStatus mode2;
    public GameModeStatus mode3;
    public long serverTime;
}

[Serializable]
public class GameModeStatus
{
    public bool available;
    public float timeLeft;
}

[Serializable]
public class TimeUntilMonthEndResponse
{
    public int days;
    public int hours;
    public int minutes;
}

[Serializable]
public class ClanInfo
{
    public int id;
    public string name;
    public ClanLeader leader;
    public ClanStats stats;
    public List<ClanMember> members = new List<ClanMember>();
    // Для удобства добавим свойства с правильным именованием
    public int Id => id;
    public string Name => name;
    public ClanLeader Leader => leader ?? new ClanLeader();
    public ClanStats Stats => stats ?? new ClanStats();
    public List<ClanMember> Members => members ?? new List<ClanMember>();
}

[Serializable]
public class ClanLeader
{
    public string id;
    public string name;
    public int rating;
    public int best_rating;
    public string Id => id;
    public string Name => name;
    public int Rating => rating;
    public int BestRating => best_rating;
}

[Serializable]
public class ClanStats
{
    public int place;
    public int current_level;
    public int clan_points;
    public int player_count;
    public int max_players;
    public int need_rating;
    public bool is_open;
    public int Place => place;
    public int Level => current_level;
    public int Clan_points => clan_points;
    public int PlayerCount => player_count;
    public int MaxPlayers => max_players;
    public int NeedRating => need_rating;
    public bool IsOpen => is_open;
}

[Serializable]
public class ClanMember
{
    public string id;
    public string name;
    public bool is_leader;
    public MemberStats stats;
    public string Id => id;
    public string Name => name;
    public bool IsLeader => is_leader;
    public MemberStats Stats => stats ?? new MemberStats();
}

[Serializable]
public class MemberStats
{
    public int rating;
    public int best_rating;
    public int clan_points;
    public int kills;
    public int matches;
    public int wins;
    public int Rating => rating;
    public int BestRating => best_rating;
    public int ClanPoints => clan_points;
    public int Kills => kills;
    public int Matches => matches;
    public int Wins => wins;
}

[Serializable]
public class ClanTopWithCurrentResponse
{
    public bool success;
    public List<ClanShortInfo> top_clans;
    public ClanShortInfo current_clan;
    public string error; // для сообщений об ошибках
}

[Serializable]
public class ClanShortInfo
{
    public string clan_id;
    public string clan_name;
    public string leader_name;
    public string leader_id;
    public int need_rating;
    public bool is_open;
    public int clan_points;
    public int clan_level;
    public int max_players;
    public int player_count;
    public int clan_place;
}


#region Структуры для комнат
[Serializable]
public class RoomInfoResponse
{
    public bool success;
    public RoomInfo room;
    public string error;
}

[Serializable]
public class PlayerRoomsResponse
{
    public List<RoomInfo> rooms;
}

[Serializable]
public class MatchmakingStatsResponse
{
    public Dictionary<string, MatchmakingStats> stats;
}

[Serializable]
public class ForceCloseRoomResponse
{
    public bool success;
    public string message;
    public string room_id;
}

[Serializable]
public class ForceEndGameResponse
{
    public bool success;
    public string message;
    public string room_id;
}
#endregion






#region Структуры для игроков в игре
[Serializable]
public class PlayerJoinedResponse
{
    public string room_id;
    public string player_id;
    public string username;
    public int rating;
    public int hero_id;
}

[Serializable]
public class LeaveRoomResponse
{
    public bool success;
    public string room_id;
    public string error;
}

[Serializable]
public class PlayerTransformUpdateResponse
{
    public bool success;
    public long timestamp;
    public List<PlayerTransformData> other_transforms;
    public PlayerTransformData my_transform;
}

[Serializable]
public class PlayerTransformData
{
    public string player_id;
    public string username;
    public int hero_id;
    public Position position;
    public Rotation rotation;
    public BoolsState bools_state;
    public string current_weapon;
    public bool is_alive;
    public float noize_volume;
    public long timestamp;
}
[Serializable]
public class Position
{
    public float x;
    public float y;
    public float z;
    
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public class Rotation
{
    public float x;
    public float y;
    public float z;
    public float w;

    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
}

[Serializable]
public class BoolsState
{
    public bool isMoving;
    public bool isShooting;
    public bool isReloading;
    public bool isHealing;
    public bool isReviving;
    public bool isPickingUp;
    public bool isDead;
}

[Serializable]
public class PlayerStatsUpdateResponse
{
    public bool success;
    public string player_id;
    public PlayerStatsData stats;
    public long timestamp;
}

[Serializable]
public class PlayerStatsData
{
    public int kills;
    public int deaths;
    public int damage;
    public bool is_alive;
    public int score;
    public long last_update;
}

[Serializable]
public class PlayerInfoResponse
{
    public string player_id;
    public string username;
    public int rating;
    public int hero_id;
    public bool is_ready;
    public bool is_alive;
    public int kills;
    public int deaths;
}
#endregion






#region Boosts
[Serializable]
public class BoostPickupResponse
{
    public bool success;
    public string player_id;
    public int boost_id;
    public string boost_type;
}

[Serializable]
public class BoostTakenResponse
{
    public string player_id;
    public int boost_id;
    public string boost_type; 
}
#endregion

#region Upgrades
[Serializable]
public class UpgradePickupResponse
{
    public bool success;
    public string player_id;
    public int upgrade_id;
    public string upgrade_type;
}

[Serializable]
public class UpgradeTakenResponse
{
    public string player_id;
    public int upgrade_id;
    public string upgrade_type;
}

[Serializable]
public class UpgradeDroppedResponse
{
    public string player_id;
    public int upgrade_id;
    public string upgrade_type;
    public Position position; // уже есть класс Position
}
#endregion

#endregion
public class WebSocketBase : MonoBehaviour
{
    public static WebSocketBase Instance { get; private set; }

    protected static string WebSocketURL;
    protected bool IsDebug;

    public static void InitializeSocket(bool isWebGL, bool isDebug, string uri)
    {
        try
        {
            if (Instance != null)
            {
                Instance.Shutdown();
            }

            WebSocketURL = uri;

            if (isWebGL)
                Instance = new GameObject("WebGLNetworkManager").AddComponent<WebSocketWebGL>();
            else
                Instance = new GameObject("AndroidNetworkManager").AddComponent<WebSocketAndroid>();

            Instance.IsDebug = isDebug;

            Debug.Log($"Initializing WebSocket with URL: {WebSocketURL}");
        }
        catch (Exception e)
        {
            Debug.LogError($"InitializeSocket error: {e}");
        }
    }

    public static void Load(Action<bool> callback)
    {
        Instance.InitializeWebSocket(callback.Invoke);
    }

    #region Actions
    public event Action OnClose;
    public event Action<string> OnServerTimeUpdate;

    public event Action<bool, string> OnPlayerLoginResponse;

    public event Action<bool> DemoAction;
    public event Action OnPlayerRegister;
    public event Action<bool, string> OnNameUpdated;
    public event Action<string, string> OnClanCreated;
    public event Action<List<ClanSearch.ClanData>> OnClanListReceived;
    public event Action<string, string> OnClanJoined;
    public event Action<PlayerStatsResponse> OnPlayerStatsUpdated;
    public event Action<ClanInfo> OnClanInfoReceived;
    public event Action OnClanLeft;
    public event Action<string, string> OnClanLeadershipTransferred;
    public event Action<Dictionary<string, object>> OnServerDataReceived;
    public event Action<ClanTopWithCurrentResponse> OnClanTopWithCurrentReceived;
    public event Action<List<LeaderboardPlayer>, LeaderboardPlayer> OnRatingLeaderboardReceived;
    public event Action<List<LeaderboardPlayer>, LeaderboardPlayer> OnKillsLeaderboardReceived;

    public event Action<GameModesStatus> OnGameModesStatusReceived;
    public event Action<TimeUntilMonthEndResponse> OnTimeUntilMonthEndReceived;

    #region Gameplay
    public event Action<PlayerTransformUpdateResponse> OnPlayerTransformUpdateResponse;
    public event Action<PlayerStatsUpdateResponse> OnPlayerStatsUpdateResponse;

    public event Action<PlayerDamagedResponse> OnPlayerDamaged;
    public event Action<PlayerDeathResponse> OnPlayerDeath;

    public event Action<BoostPickupResponse> OnBoostPickupResponse;
    public event Action<BoostTakenResponse> OnBoostTaken;

    public event Action<UpgradePickupResponse> OnUpgradePickupResponse;
    public event Action<UpgradeTakenResponse> OnUpgradeTaken;
    public event Action<UpgradeDroppedResponse> OnUpgradeDropped;

    public event Action<string> OnHealPlayer;
    public event Action<PlayerRespawnResponse> OnPlayerRespawnResponse;
    public event Action<PlayerRespawnedMessage> OnPlayerRespawned;
    #endregion

    #region Комнаты
    public event Action<RoomJoinedResponse> OnMatchmakingJoined;
    public event Action<PlayerLeftRoomResponse> OnPlayerLeftRoom;
    public event Action<MatchStartResponse> OnMatchStart;
    public event Action<MatchEndResponse> OnMatchEnd;
    public event Action<RoomForceClosedResponse> OnRoomForceClosed;
    public event Action<GameForceEndedResponse> OnGameForceEnded;
    public event Action<RoomInfoResponse> OnRoomInfoReceived;
    public event Action<PlayerRoomsResponse> OnPlayerRoomsReceived;
    public event Action<MatchmakingStatsResponse> OnMatchmakingStatsReceived;
    public event Action<LeaveRoomResponse> OnLeaveRoomResponse;
    public event Action<ForceCloseRoomResponse> OnForceCloseRoomResponse;
    public event Action<ForceEndGameResponse> OnForceEndGameResponse;
    public event Action<string> OnMatchmakingFull;
    #endregion






    #region Игроки
    public event Action<PlayerJoinedResponse> OnPlayerJoinedRoom;
    public event Action<List<PlayerInfoResponse>> OnRoomPlayersUpdate;
    #endregion

    #endregion
    protected virtual void InitializeWebSocket(Action<bool> callback) { }

    public void UnsubscribeAllCompact()
    {
        ClearEvent(ref OnClose);
        ClearEvent(ref OnServerTimeUpdate);
    }

    private void ClearEvent<T>(ref T handler) where T : Delegate
    {
        if (handler != null)
        {
            foreach (Delegate d in handler.GetInvocationList())
            {
                handler = (T)Delegate.Remove(handler, d);
            }
        }
        handler = null;
    }

    protected virtual void SendWebSocketRequest(string action, Dictionary<string, object> data) { }
    protected virtual void SendWebSocketRequest(string action) { }

    protected virtual void CloseConnect()
    {
        OnClose?.Invoke();
        UnsubscribeAllCompact();
    }


    public virtual void Shutdown() { }

    public void OnDestroy()
    {
        Shutdown();
    }

    //Обработчик ответов
    protected void HandleServerResponse(Dictionary<string, object> message)
    {
        string action = message["action"].ToString();

        switch (action)
        {
            case "demo_response":
                HandleJoinConfirmation(message);
                break;

            case "player_login_response":
                HandlePlayerLoginResponse(message);
                break;
            case "player_connect_response":
                HandlePlayerConnectResponse(message);
                break;


            case "game_modes_status_response":
                HandleGameModesStatusResponse(message);
                break;
            case "time_until_month_end_response":
                HandleTimeUntilMonthEndResponse(message);
                break;


            case "register_player_response":
                HandleRegisterPlayer(message);
                break;
            case "update_name_response":
                HandleUpdateNameResponse(message);
                break;
            case "get_player_data_response":
                HandleGetPlayerDataResponse(message);
                break;


            case "player_stats_updated":
                HandlePlayerStatsUpdated(message);
                break;
            case "update_hero_stats_response":
                HandleHeroStatsUpdated(message);
                break;
            case "update_hero_levels_response":
                HandleUpdateHeroLevelsResponse(message);
                break;
            case "spend_hero_cards_response":
                HandleSpendHeroCardsResponse(message);
                break;


            case "create_clan_response":
                HandleCreateClanResponse(message);
                break;
            case "get_all_clans_response":
                HandleClanListResponse(message);
                break;
            case "clan_search_results":
                HandleClanListResponse(message);
                break;
            case "get_clan_info_response":
                HandleClanInfoResponse(message);
                break;
            case "join_clan_response":
                HandleJoinClanResponse(message);
                break;
            case "leave_clan_response":
                HandleLeaveClanResponse(message);
                break;
            case "transfer_leadership_response":
                HandleTransferLeadershipResponse(message);
                break;
            case "update_clan_settings_response":
                HandleUpdateClanSettingsResponse(message);
                break;
            case "get_clan_top_with_current_response":
                HandleClanTopWithCurrentResponse(message);
                break;


            case "add_currency_response":
                HandleAddCurrencyResponse(message);
                break;

            case "rating_leaderboard_response":
                HandleRatingLeaderboardResponse(message);
                break;
            case "kills_leaderboard_response":
                HandleKillsLeaderboardResponse(message);
                break;


            case "join_matchmaking_response":
                HandleMatchmakingJoinedResponse(message);
                break;
            case "player_left":
                HandlePlayerLeftRoomResponse(message);
                break;
            case "match_start":
                HandleMatchStartResponse(message);
                break;
            case "match_end":
                HandleMatchEndResponse(message);
                break;
            case "room_force_closed":
                HandleRoomForceClosedResponse(message);
                break;
            case "game_force_ended":
                HandleGameForceEndedResponse(message);
                break;
            case "room_info_response":
                HandleRoomInfoResponse(message);
                break;
            case "player_rooms_response":
                HandlePlayerRoomsResponse(message);
                break;
            case "matchmaking_stats_response":
                HandleMatchmakingStatsResponse(message);
                break;
            case "left_room_response":
                HandleLeaveRoomResponse(message);
                break;
            case "room_force_closed_response":
                HandleForceCloseRoomResponse(message);
                break;
            case "game_force_ended_response":
                HandleForceEndGameResponse(message);
                break;
            case "matchmaking_full":
                HandleMatchmakingFullResponse(message);
                break;


            case "player_joined_room":
                HandlePlayerJoinedRoomResponse(message);
                break;
            case "room_players_update":
                HandleRoomPlayersUpdateResponse(message);
                break;



            case "update_player_transform_response":
                HandlePlayerTransformUpdateResponse(message);
                break;

            case "deal_damage_response":
                // Debug.Log("000");
                // HandlePlayerDealDamaged(message);
                break;
            case "player_death_response":
                HandlePlayerDeath(message);
                break;
            case "player_damaged":
                // Debug.Log("666");
                HandlePlayerDamaged(message);
                break;

            case "player_stats_update_response":
                HandlePlayerStatsUpdateResponse(message);
                break;

            //===============Boosts==============
            case "boost_pickup_response":
                HandleBoostPickupResponse(message);
                break;
            case "boost_taken":
                HandleBoostTaken(message);
                break;

            //===============Upgrades==============
            case "upgrade_pickup_response":
                HandleUpgradePickupResponse(message);
                break;
            case "upgrade_taken":
                HandleUpgradeTaken(message);
                break;
            case "upgrade_dropped":
                HandleUpgradeDropped(message);
                break;
            case "upgrade_drop_response":
                Debug.Log("sucsess drop");
                break;

            // ==============Useable==============
            case "player_healed":
                HandleHealPlayer(message);
                break;

            case "player_respawn_response":
                HandlePlayerRespawnResponse(message);
                break;
            case "player_respawned":
                HandlePlayerRespawned(message);
                break;



            default:
                Debug.LogWarning($"Received server message with action: {action}");
                break;
        }
    }

    //Функции ответов от сервера
    #region *Ответы*
    private void HandlePlayerLoginResponse(Dictionary<string, object> message)
    {
        bool success = Convert.ToBoolean(message["success"]);
        string responseMessage = message.TryGetValue("message", out var msgObj)
            ? msgObj.ToString()
            : string.Empty;

        OnPlayerLoginResponse?.Invoke(success, responseMessage);
    }
    private void HandlePlayerConnectResponse(Dictionary<string, object> message)
    {
        bool success = Convert.ToBoolean(message["success"]);
        if (success)
        {
            Debug.Log("Player connected successfully to server");
            // Теперь можно запрашивать данные игрока
            RequestPlayerData(Geekplay.Instance.PlayerData.id);
        }
        else
        {
            string error = message.TryGetValue("error", out var errorObj)
                ? errorObj.ToString()
                : "Connection failed";
            Debug.LogError($"Player connection failed: {error}");
        }
    }

    private void HandleGetServerTime(Dictionary<string, object> message)
    {
        if (message.TryGetValue("server_time", out var data))
        {
            string serverTimeString = data.ToString();
            OnServerTimeUpdate?.Invoke(serverTimeString); // Вызываем событие обновления времени
        }
    }

    private void HandleGetServerStats(Dictionary<string, object> message)
    {
        int remainingPlayers = Convert.ToInt32(message["remainingPlayers"]);
        int remainingRooms = Convert.ToInt32(message["remainingRooms"]);
        int register = Convert.ToInt32(message["register"]);

        Debug.Log(register);
        Debug.Log(remainingPlayers);
        Debug.Log(remainingRooms);
    }

    private void HandleJoinConfirmation(Dictionary<string, object> message)
    {
        bool testBool = false;
        if (message.TryGetValue("demo_massage", out var demoMassage))
        {
            testBool = true;
        }
        DemoAction?.Invoke(testBool);
    }

    private void HandleRegisterPlayer(Dictionary<string, object> message)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            if (message.TryGetValue("success", out var successObj) && (bool)successObj)
            {
                // Сохраняем player_id как строку (формат "id-рандомное_число")
                if (message.TryGetValue("player_id", out var playerIdObj))
                {
                    string playerId = playerIdObj.ToString();
                    Geekplay.Instance.PlayerData.id = playerId; // или отдельное поле для player_id, если нужно
                }

                // Сохраняем имя игрока
                if (message.TryGetValue("player_name", out var playerNameObj))
                {
                    string playerName = playerNameObj.ToString();
                    Geekplay.Instance.PlayerData.name = playerName;
                }

                Geekplay.Instance.Save();
                OnPlayerRegister?.Invoke();
            }
            else
            {
                string errorMessage = message.TryGetValue("error", out var errorObj)
                    ? errorObj.ToString()
                    : "Unknown registration error";

                Debug.LogError($"Player registration failed: {errorMessage}");
            }
        });
    }
    private void HandleGetPlayerDataResponse(Dictionary<string, object> message)
    {
        // Debug.Log("HandleGetPlayerDataResponse");
        if (message.TryGetValue("success", out var successObj) && (bool)successObj)
        {
            // Debug.Log(1);
            if (message.TryGetValue("player", out var playerObj))
            {
                // Debug.Log(2);
                // Debug.Log(playerObj.ToString());
                var playerData = JsonConvert.DeserializeObject<Dictionary<string, object>>(playerObj.ToString());
                OnServerDataReceived?.Invoke(playerData);
                // Debug.Log(3);
            }
        }
        else
        {
            // Debug.Log(4);
            Debug.LogError("Failed to load player data from server");
        }
    }

    private void HandleUpdateNameResponse(Dictionary<string, object> message)
    {
        bool success = Convert.ToBoolean(message["success"]);
        string newName = message.TryGetValue("player_name", out var nameObj)
            ? nameObj.ToString()
            : message["new_name"].ToString();

        OnNameUpdated?.Invoke(success, newName);
    }

    private void HandleClanListResponse(Dictionary<string, object> message)
    {
        if (message.TryGetValue("clans", out var clansObj))
        {
            string clansJson = clansObj.ToString();
            // Debug.Log(clansJson);
            var clanList = JsonConvert.DeserializeObject<List<ClanSearch.ClanData>>(clansJson);
            OnClanListReceived?.Invoke(clanList);
        }
    }

    private void HandleCreateClanResponse(Dictionary<string, object> message)
    {
        bool success = Convert.ToBoolean(message["success"]);
        if (success)
        {
            string clanId = message["clan_id"].ToString();
            string clanName = message["clan_name"].ToString();
            OnClanCreated?.Invoke(clanId, clanName);
        }
    }

    private void HandlePlayerStatsUpdated(Dictionary<string, object> message)
    {
        var stats = JsonConvert.DeserializeObject<PlayerStatsResponse>(message["stats"].ToString());
        OnPlayerStatsUpdated?.Invoke(stats);
    }

    private void HandleHeroStatsUpdated(Dictionary<string, object> message)
    {
        if (message.TryGetValue("success", out var successObj) && (bool)successObj)
        {
            WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
            {
                if (message.TryGetValue("hero_match", out var heroMatchObj))
                {
                    var heroMatchList = JsonConvert.DeserializeObject<List<int>>(heroMatchObj.ToString());
                    if (heroMatchList.Count == Geekplay.Instance.PlayerData.heroMatch.Length)
                    {
                        for (int i = 0; i < heroMatchList.Count; i++)
                        {
                            Geekplay.Instance.PlayerData.heroMatch[i] = heroMatchList[i];
                        }
                    }
                }

                if (message.TryGetValue("favorite_hero", out var favHeroObj))
                {
                    string heroName = favHeroObj.ToString();
                    Geekplay.Instance.PlayerData.favoriteHero = MainMenu.Instance.GetHeroIdByName(heroName);
                }

                Geekplay.Instance.Save();
                Debug.Log("Hero stats updated successfully");
            });
        }
        else
        {
            Debug.LogError("Failed to update hero stats");
        }
    }

    private void HandleJoinClanResponse(Dictionary<string, object> message)
    {
        if (message.TryGetValue("clan_id", out var clanIdObj) &&
            message.TryGetValue("clan_name", out var clanNameObj))
        {
            string clanId = clanIdObj.ToString();
            string clanName = clanNameObj.ToString();
            OnClanJoined?.Invoke(clanId, clanName);
        }
        else
        {
            Debug.Log("gg");
        }
    }
    private void HandleLeaveClanResponse(Dictionary<string, object> message)
    {
        if (message.TryGetValue("success", out var success) && (bool)success)
        {
            WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
            {
                // Все UI и данные PlayerData обновляем только в главном потоке
                Geekplay.Instance.PlayerData.clanName = null;
                Geekplay.Instance.PlayerData.clanId = null;
                Geekplay.Instance.PlayerData.isClanLeader = false;
                Geekplay.Instance.PlayerData.clanPoints = 0;
                Geekplay.Instance.Save();

                OnClanLeft?.Invoke();
            });
        }
    }
    private void HandleTransferLeadershipResponse(Dictionary<string, object> message)
    {
        if (message.TryGetValue("success", out var success) && (bool)success)
        {
            string newLeaderId = message["new_leader_id"].ToString();
            string newLeaderName = message["new_leader_name"].ToString();

            // Обновляем локальные данные, если мы передали лидерство
            if (Geekplay.Instance.PlayerData.id == newLeaderId)
            {
                Geekplay.Instance.PlayerData.isClanLeader = false;
                Geekplay.Instance.Save();
            }

            OnClanLeadershipTransferred?.Invoke(newLeaderId, newLeaderName);
        }
    }

    private void HandleClaimRewardsResponse(Dictionary<string, object> message)
    {
        bool success = Convert.ToBoolean(message["success"]);

        if (success)
        {
            WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
            {
                // Обновляем деньги
                if (message.TryGetValue("money", out var moneyObj))
                {
                    Geekplay.Instance.PlayerData.money = Convert.ToInt32(moneyObj);
                }

                if (message.TryGetValue("donat_money", out var donatMoneyObj))
                {
                    Geekplay.Instance.PlayerData.donatMoney = Convert.ToInt32(donatMoneyObj);
                }

                // Обновляем карточки героев
                if (message.TryGetValue("hero_cards", out var cardsObj))
                {
                    var updatedCards = JsonConvert.DeserializeObject<Dictionary<int, int>>(cardsObj.ToString());
                    foreach (var card in updatedCards)
                    {
                        if (card.Key >= 0 && card.Key < Geekplay.Instance.PlayerData.persons.Length)
                        {
                            Geekplay.Instance.PlayerData.persons[card.Key].heroCard += card.Value;
                        }
                    }
                }

                Geekplay.Instance.Save();
            });
        }

        // OnRewardsClaimed?.Invoke(success, message);
    }
    private void HandleAddCurrencyResponse(Dictionary<string, object> message)
    {
        bool success = Convert.ToBoolean(message["success"]);

        if (success)
        {
            WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
            {
                // Обновляем деньги
                if (message.TryGetValue("money", out var moneyObj))
                {
                    Geekplay.Instance.PlayerData.money = Convert.ToInt32(moneyObj);
                }

                if (message.TryGetValue("donat_money", out var donatMoneyObj))
                {
                    Geekplay.Instance.PlayerData.donatMoney = Convert.ToInt32(donatMoneyObj);
                }

                Geekplay.Instance.Save();
                Debug.Log($"Currency added. New balance: {Geekplay.Instance.PlayerData.money} money, {Geekplay.Instance.PlayerData.donatMoney} donat");
            });
        }
        else
        {
            string error = message.TryGetValue("error", out var errorObj)
                ? errorObj.ToString()
                : "Unknown error";
            Debug.LogError($"Failed to add currency: {error}");
        }
    }
    private void HandleSpendHeroCardsResponse(Dictionary<string, object> message)
    {
        if (message.TryGetValue("success", out var successObj) && (bool)successObj)
        {
            WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
            {
                // Update local hero cards if needed
                if (message.TryGetValue("updated_cards", out var cardsObj))
                {
                    var updatedCards = JsonConvert.DeserializeObject<Dictionary<int, int>>(cardsObj.ToString());
                    foreach (var card in updatedCards)
                    {
                        if (card.Key >= 0 && card.Key < Geekplay.Instance.PlayerData.persons.Length)
                        {
                            Geekplay.Instance.PlayerData.persons[card.Key].heroCard = card.Value;
                        }
                    }
                    Geekplay.Instance.Save();
                }
                Debug.Log("Hero cards spent successfully");
            });
        }
        else
        {
            string error = message.TryGetValue("error", out var errorObj)
                ? errorObj.ToString()
                : "Failed to spend hero cards";
            Debug.LogError(error);
        }
    }

    public void HandleClanInfoResponse(Dictionary<string, object> message)
    {
        if (message.TryGetValue("success", out var successObj) && (bool)successObj)
        {
            // Debug.Log(1);
            // Извлекаем объект "clan" из сообщения
            if (message.TryGetValue("clan", out var clanObj))
            {
                // Debug.Log(2);
                var clanJson = JsonConvert.SerializeObject(clanObj);
                // Debug.Log("Clan JSON: " + clanJson);

                var clanInfo = JsonConvert.DeserializeObject<ClanInfo>(clanJson);

                if (clanInfo == null)
                {
                    Debug.LogError("Failed to deserialize clan info");
                    return;
                }

                // Десериализация members отдельно, если они не попали в clanJson
                if (message.TryGetValue("members", out var membersObj))
                {
                    // Debug.Log(3);
                    var membersJson = JsonConvert.SerializeObject(membersObj);
                    clanInfo.members = JsonConvert.DeserializeObject<List<ClanMember>>(membersJson);
                }

                clanInfo.stats ??= new ClanStats();
                clanInfo.leader ??= new ClanLeader();

                foreach (var member in clanInfo.members)
                {
                    member.stats ??= new MemberStats();
                }

                OnClanInfoReceived?.Invoke(clanInfo);
            }
            else
            {
                Debug.LogError("Clan object not found in response");
            }
        }
        else
        {
            string errorMessage = message.TryGetValue("error", out var errorObj)
                ? errorObj.ToString()
                : "Unknown error while getting clan info";

            Debug.LogError($"Get clan info failed: {errorMessage}");
        }
    }

    private void HandleUpdateClanSettingsResponse(Dictionary<string, object> message)
    {
        bool success = Convert.ToBoolean(message["success"]);
        if (success)
        {
            // Обновить UI или данные
        }
    }

    private void HandleGameModesStatusResponse(Dictionary<string, object> message)
    {
        if (message.TryGetValue("modes", out var modesObj))
        {
            string modesJson = JsonConvert.SerializeObject(modesObj);
            var modesStatus = JsonConvert.DeserializeObject<GameModesStatus>(modesJson);

            // Добавляем serverTime если он есть в сообщении
            if (message.TryGetValue("serverTime", out var serverTimeObj))
            {
                modesStatus.serverTime = Convert.ToInt64(serverTimeObj);
            }

            OnGameModesStatusReceived?.Invoke(modesStatus);
        }
        else
        {
            Debug.LogError("Failed to parse game modes status response");
        }
    }

    private void HandleTimeUntilMonthEndResponse(Dictionary<string, object> message)
    {
        var response = new TimeUntilMonthEndResponse
        {
            days = Convert.ToInt32(message["days"]),
            hours = Convert.ToInt32(message["hours"]),
            minutes = Convert.ToInt32(message["minutes"])
        };

        OnTimeUntilMonthEndReceived?.Invoke(response);
    }

    private void HandleClanTopWithCurrentResponse(Dictionary<string, object> message)
    {
        if (message.TryGetValue("success", out var successObj) && (bool)successObj)
        {
            try
            {
                // Получаем топ кланов
                var topClansList = new List<ClanShortInfo>();
                if (message.TryGetValue("topClans", out var topClansObj) && topClansObj is JArray topClansArray)
                {
                    // Debug.Log("topClans we have");
                    foreach (var item in topClansArray)
                    {
                        var clanDict = item.ToObject<Dictionary<string, object>>();
                        if (clanDict != null)
                        {
                            // Debug.Log("22");
                            topClansList.Add(ParseClanDict(clanDict));
                        }
                    }
                    // topClansList = topClansObj
                }

                // Текущий клан
                ClanShortInfo currentClan = null;
                if (message.TryGetValue("currentClan", out var currentClanObj) && currentClanObj is JObject currentClanJObject)
                {
                    // Debug.Log("currentClan we have");
                    currentClan = ParseClanDict(currentClanJObject.ToObject<Dictionary<string, object>>());
                }
                else
                {
                    Debug.Log("currentClan poseyali");
                }

                var response = new ClanTopWithCurrentResponse
                {
                    success = true,
                    top_clans = topClansList,
                    current_clan = currentClan
                };

                OnClanTopWithCurrentReceived?.Invoke(response);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to parse clan top response: " + ex);
            }
        }
        else
        {
            string error = message.TryGetValue("error", out var errorObj) ? errorObj.ToString() : "Unknown error";
            Debug.LogWarning($"Failed to get clan top: {error}");
        }
    }

    // Парсинг одного клана из Dictionary
    private ClanShortInfo ParseClanDict(Dictionary<string, object> dict)
    {
        int currentLevel = -1;
        // int clanPoints = -1;

        if (dict.TryGetValue("stats", out var statsObj))
        {
            // Преобразуем JObject в Dictionary
            var statsDict = statsObj as JObject;
            if (statsDict != null)
            {
                if (statsDict.TryGetValue("current_level", out var cl))
                    currentLevel = cl.Value<int>();
            }
        }

        return new ClanShortInfo
        {
            clan_id = dict.TryGetValue("clan_id", out var id) ? Convert.ToString(id) : "",
            clan_name = dict.TryGetValue("clan_name", out var name) ? name.ToString() : "",
            leader_id = dict.TryGetValue("leader_id", out var lid) ? lid.ToString() : "",
            leader_name = dict.TryGetValue("leader_name", out var lname) ? lname.ToString() : "",
            need_rating = dict.TryGetValue("need_rating", out var nr) ? Convert.ToInt32(nr) : -1,
            is_open = dict.TryGetValue("is_open", out var io) && Convert.ToBoolean(io),
            clan_points = dict.TryGetValue("clan_points", out var cp) ? Convert.ToInt32(cp) : -1,
            clan_level = currentLevel,
            max_players = dict.TryGetValue("max_players", out var mp) ? Convert.ToInt32(mp) : 24,
            player_count = dict.TryGetValue("player_count", out var pc) ? Convert.ToInt32(pc) : -1,
            clan_place = dict.TryGetValue("place", out var pl) ? Convert.ToInt32(pl) : -1
        };
    }

    private void HandleRatingLeaderboardResponse(Dictionary<string, object> message)
    {
        if (message.TryGetValue("top_players", out var topObj) &&
            message.TryGetValue("my_stats", out var myObj))
        {
            var topPlayers = JsonConvert.DeserializeObject<List<LeaderboardPlayer>>(topObj.ToString());
            var myStats = JsonConvert.DeserializeObject<LeaderboardPlayer>(myObj.ToString());
            OnRatingLeaderboardReceived?.Invoke(topPlayers, myStats);
        }
    }

    private void HandleKillsLeaderboardResponse(Dictionary<string, object> message)
    {
        if (message.TryGetValue("top_players", out var topObj) &&
            message.TryGetValue("my_stats", out var myObj))
        {
            var topPlayers = JsonConvert.DeserializeObject<List<LeaderboardPlayer>>(topObj.ToString());
            var myStats = JsonConvert.DeserializeObject<LeaderboardPlayer>(myObj.ToString());

            OnKillsLeaderboardReceived?.Invoke(topPlayers, myStats);
        }
    }

    private void HandleUpdateHeroLevelsResponse(Dictionary<string, object> message)
    {
        if (message.TryGetValue("success", out var successObj) && (bool)successObj)
        {
            int heroId = Convert.ToInt32(message["hero_id"]);
            int level = Convert.ToInt32(message["level"]);
            int rank = Convert.ToInt32(message["rank"]);

            // Обновляем локальные данные
            var hero = Geekplay.Instance.PlayerData.persons[heroId];
            hero.level = level;
            hero.rank = rank;

            Debug.Log($"Successfully updated hero {heroId}: level={level}, rank={rank}");
        }
        else
        {
            string error = message.TryGetValue("message", out var errorObj)
                ? errorObj.ToString()
                : "Unknown error";
            Debug.LogError($"Failed to update hero levels: {error}");
        }
    }

    #region Комнаты
    private void HandleMatchmakingJoinedResponse(Dictionary<string, object> message)
    {
        var response = JsonConvert.DeserializeObject<RoomJoinedResponse>(
            JsonConvert.SerializeObject(message));
        OnMatchmakingJoined?.Invoke(response);
    }

    private void HandlePlayerLeftRoomResponse(Dictionary<string, object> message)
    {
        var response = JsonConvert.DeserializeObject<PlayerLeftRoomResponse>(
            JsonConvert.SerializeObject(message));
        OnPlayerLeftRoom?.Invoke(response);
    }

    private void HandleMatchStartResponse(Dictionary<string, object> message)
    {
        Debug.Log("HandleMatchStartResponse");

        // Debug.Log(typeof(MatchStartResponse).Assembly.FullName);
        // foreach (var f in typeof(MatchStartResponse).GetFields(
        //     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        // Debug.Log($"Field: {f.Name} : {f.FieldType}");

        var response = JsonConvert.DeserializeObject<MatchStartResponse>(
            JsonConvert.SerializeObject(message));

        if (response.players != null)
        {
            var playersDict = new Dictionary<string, PlayerInGameInfo>();

            foreach (var playerResp in response.players)
            {
                Debug.Log("playerResp.max_hp " + playerResp.max_hp);
                Debug.Log("playerResp.max_armor " + playerResp.max_armor);
                var playerInfo = new PlayerInGameInfo(
                    playerResp.playerId,
                    playerResp.player_name,
                    playerResp.rating,
                    playerResp.hero_id
                )
                {
                    hp = playerResp.hp,
                    armor = playerResp.armor,
                    max_hp = playerResp.max_hp,
                    max_armor = playerResp.max_armor,
                    hero_skin = playerResp.hero_skin,
                    hero_level = playerResp.hero_level,
                    hero_rank = playerResp.hero_rank,
                    isReady = playerResp.isReady,
                    isAlive = playerResp.isAlive,
                    kills = playerResp.kills,
                    deaths = playerResp.deaths,
                    position = playerResp.position != null ? playerResp.position : Vector3.zero,
                    rotation = playerResp.rotation != null ? playerResp.rotation : Quaternion.identity,
                    animationState = !string.IsNullOrEmpty(playerResp.animationState) ? playerResp.animationState : "idle"
                };

                playersDict[playerResp.playerId] = playerInfo;
            }

            // Создаем RoomInfo с правильной структурой
            var roomInfo = new RoomInfo
            {
                id = response.room_id,
                state = "in_progress",
                players = playersDict,
                bots = response.bots ?? new List<PlayerInGameInfo>(),
                maxPlayers = OnlineRoom.Instance.CurrentRoom?.maxPlayers ?? 4,
                matchId = response.match_id,
                botCount = response.bots?.Count ?? 0
            };

            // Обновляем комнату
            OnlineRoom.Instance.UpdateRoomInfo(roomInfo);
        }

        OnMatchStart?.Invoke(response);
    }

    private void HandleMatchEndResponse(Dictionary<string, object> message)
    {
        var response = JsonConvert.DeserializeObject<MatchEndResponse>(
            JsonConvert.SerializeObject(message));
        OnMatchEnd?.Invoke(response);
    }

    private void HandleRoomForceClosedResponse(Dictionary<string, object> message)
    {
        var response = JsonConvert.DeserializeObject<RoomForceClosedResponse>(
            JsonConvert.SerializeObject(message));
        OnRoomForceClosed?.Invoke(response);

        if (OnlineRoom.Instance.IsInRoom && OnlineRoom.Instance.CurrentRoom.id == response.room_id)
        {
            OnlineRoom.Instance.ClearCurrentRoom();
        }
    }

    private void HandleGameForceEndedResponse(Dictionary<string, object> message)
    {
        var response = JsonConvert.DeserializeObject<GameForceEndedResponse>(
            JsonConvert.SerializeObject(message));
        OnGameForceEnded?.Invoke(response);
    }

    private void HandleRoomInfoResponse(Dictionary<string, object> message)
    {
        var response = JsonConvert.DeserializeObject<RoomInfoResponse>(
            JsonConvert.SerializeObject(message));
        OnRoomInfoReceived?.Invoke(response);
    }

    private void HandlePlayerRoomsResponse(Dictionary<string, object> message)
    {
        var response = JsonConvert.DeserializeObject<PlayerRoomsResponse>(
            JsonConvert.SerializeObject(message));
        OnPlayerRoomsReceived?.Invoke(response);
    }

    private void HandleMatchmakingStatsResponse(Dictionary<string, object> message)
    {
        var response = JsonConvert.DeserializeObject<MatchmakingStatsResponse>(
            JsonConvert.SerializeObject(message));
        OnMatchmakingStatsReceived?.Invoke(response);
    }

    private void HandleLeaveRoomResponse(Dictionary<string, object> message)
    {
        var response = JsonConvert.DeserializeObject<LeaveRoomResponse>(
            JsonConvert.SerializeObject(message));
        OnLeaveRoomResponse?.Invoke(response);
    }

    private void HandleForceCloseRoomResponse(Dictionary<string, object> message)
    {
        var response = JsonConvert.DeserializeObject<ForceCloseRoomResponse>(
            JsonConvert.SerializeObject(message));
        OnForceCloseRoomResponse?.Invoke(response);
    }

    private void HandleForceEndGameResponse(Dictionary<string, object> message)
    {
        var response = JsonConvert.DeserializeObject<ForceEndGameResponse>(
            JsonConvert.SerializeObject(message));
        OnForceEndGameResponse?.Invoke(response);
    }

    private void HandleMatchmakingFullResponse(Dictionary<string, object> message)
    {
        string errorMessage = message.TryGetValue("message", out var msgObj)
            ? msgObj.ToString()
            : "All rooms are currently full";
        OnMatchmakingFull?.Invoke(errorMessage);
    }
    #endregion





    #region Игроки
    private void HandlePlayerJoinedRoomResponse(Dictionary<string, object> message)
    {
        var response = JsonConvert.DeserializeObject<PlayerJoinedResponse>(
            JsonConvert.SerializeObject(message));
        OnPlayerJoinedRoom?.Invoke(response);
    }

    private void HandlePlayerTransformUpdateResponse(Dictionary<string, object> message)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<PlayerTransformUpdateResponse>(
                JsonConvert.SerializeObject(message));

            OnPlayerTransformUpdateResponse?.Invoke(response);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing transform response: {e.Message}");
        }
    }

    private void HandlePlayerStatsUpdateResponse(Dictionary<string, object> message)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<PlayerStatsUpdateResponse>(
                JsonConvert.SerializeObject(message));

            OnPlayerStatsUpdateResponse?.Invoke(response);

            Debug.Log($"Received stats update for player {response.player_id}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing player stats response: {e.Message}");
        }
    }

    private void HandleRoomPlayersUpdateResponse(Dictionary<string, object> message)
    {
        if (message.TryGetValue("players", out var playersObj))
        {
            var playersList = JsonConvert.DeserializeObject<List<PlayerInfoResponse>>(
                playersObj.ToString());
            OnRoomPlayersUpdate?.Invoke(playersList);
        }
    }

    #region Логика стрельбы и нанесение урона
    [Serializable]
    public class PlayerDamagedResponse
    {
        public string action;
        public bool success;
        public string attacker_id;
        public string target_id;
        public float damage;
        public float new_hp;
        public float new_armor;
        public Position shot_origin;
        public Position shot_direction;
        public string room_id;
    }

    [Serializable]
    public class PlayerDeathResponse
    {
        public string action;
        public string player_id;
        public string killer_id;
    }

    [Serializable]
    public class PlayerRespawnStats
    {
        public float hp;
        public float max_hp;
        public float armor;
        public float max_armor;
        public int kills;
        public int deaths;
        public float damage;
        public float vision;
        public float score;
        public long last_update;
        public bool is_alive;
        public long respawn_time;
    }

    [Serializable]
    public class PlayerRespawnResponse
    {
        public bool success;
        public string player_id;
        public PlayerRespawnStats stats;
        public long timestamp;
    }

    [Serializable]
    public class PlayerRespawnedMessage
    {
        public string player_id;
        public PlayerRespawnStats stats;
    }

    private void HandlePlayerDamaged(Dictionary<string, object> message)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<PlayerDamagedResponse>(
                JsonConvert.SerializeObject(message));

            // Debug.Log(JsonConvert.SerializeObject(response, Formatting.Indented));

            OnPlayerDamaged?.Invoke(response);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing player_damaged: {e.Message}");
        }
    }

    private void HandlePlayerDeath(Dictionary<string, object> message)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<PlayerDeathResponse>(
                JsonConvert.SerializeObject(message));

            OnPlayerDeath?.Invoke(response);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing player_death_response: {e.Message}");
        }
    }
    #endregion

    #region Бусты
    private void HandleBoostPickupResponse(Dictionary<string, object> message)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<BoostPickupResponse>(
                JsonConvert.SerializeObject(message));

            OnBoostPickupResponse?.Invoke(response);

            Debug.Log($"[Boost] Pickup response: success={response.success}, boost={response.boost_id} ({response.boost_type})");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing boost_pickup_response: {e.Message}");
        }
    }

    private void HandleBoostTaken(Dictionary<string, object> message)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<BoostTakenResponse>(
                JsonConvert.SerializeObject(message));

            OnBoostTaken?.Invoke(response);

            Debug.Log($"[Boost] Taken: player={response.player_id}, boost={response.boost_id} ({response.boost_type})");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing boost_taken: {e.Message}");
        }
    }
    #endregion

    #endregion

    #region Используемое
    private void HandleHealPlayer(Dictionary<string, object> message)
    {
        // достаём hp и max_hp из ответа
        if (message.TryGetValue("success", out var successObj) && (bool)successObj)
        {
            message.TryGetValue("player_id", out var playerId);
            OnHealPlayer?.Invoke(playerId.ToString());
        }
        else
        {
            string error = message.TryGetValue("message", out var errorObj)
                ? errorObj.ToString()
                : "Unknown error";
            Debug.LogError($"Failed to update hero levels: {error}");
        }
    }
    
    private void HandlePlayerRespawnResponse(Dictionary<string, object> message)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<PlayerRespawnResponse>(
                JsonConvert.SerializeObject(message));

            OnPlayerRespawnResponse?.Invoke(response);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing player_respawn_response: {e.Message}");
        }
    }

    private void HandlePlayerRespawned(Dictionary<string, object> message)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<PlayerRespawnedMessage>(
                JsonConvert.SerializeObject(message));

            OnPlayerRespawned?.Invoke(response);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing player_respawned: {e.Message}");
        }
    }
    #endregion





    private void HandleUpgradePickupResponse(Dictionary<string, object> message)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<UpgradePickupResponse>(
                JsonConvert.SerializeObject(message));

            OnUpgradePickupResponse?.Invoke(response);

            Debug.Log($"[Upgrade] Pickup response: success={response.success}, upgrade={response.upgrade_id} ({response.upgrade_type})");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing upgrade_pickup_response: {e.Message}");
        }
    }

    private void HandleUpgradeTaken(Dictionary<string, object> message)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<UpgradeTakenResponse>(
                JsonConvert.SerializeObject(message));

            OnUpgradeTaken?.Invoke(response);

            Debug.Log($"[Upgrade] Taken: player={response.player_id}, upgrade={response.upgrade_id} ({response.upgrade_type})");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing upgrade_taken: {e.Message}");
        }
    }

    private void HandleUpgradeDropped(Dictionary<string, object> message)
    {
        try
        {
            Debug.Log("HandleUpgradeDropped");
            var response = JsonConvert.DeserializeObject<UpgradeDroppedResponse>(
                JsonConvert.SerializeObject(message));

            OnUpgradeDropped?.Invoke(response);

            Debug.Log($"[Upgrade] Dropped: player={response.player_id}, upgrade={response.upgrade_id} ({response.upgrade_type}) at pos=({response.position.x},{response.position.y},{response.position.z})");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing upgrade_dropped: {e.Message}");
        }
    }
    #endregion





    #region *Запросы*
    public void PingPong()
    {
        SendWebSocketRequest("ping");
    }

    public void LoginPlayer(string playerId, string playerName = null)
    {
        var data = new Dictionary<string, object>
        {
            { "player_id", playerId }
        };

        // Опционально: имя для проверки/подтверждения
        if (!string.IsNullOrEmpty(playerName))
        {
            data.Add("player_name", playerName);
        }

        SendWebSocketRequest("player_login", data);
    }
    public void ConnectPlayer(string playerId, string playerName)
    {
        var data = new Dictionary<string, object>
        {
            { "player_id", playerId },
            { "player_name", playerName }
        };
        SendWebSocketRequest("player_connect", data);
    }

    public void GetStats()
    {
        var requestData = new Dictionary<string, object>
        {
            //Можем в данном формате указать, что хотим послать
            /* { "player_id", PlayerID},
             { "player_info", PlayerInfo},
             { "player_name", PlayerName}*/
        };

        SendWebSocketRequest("get_server_stats", requestData); // get_server_stats - название метода на сервере
    }

    public void DemoRequest()
    {
        SendWebSocketRequest("demo_request");
    }

    public void GetGameModesStatus()
    {
        SendWebSocketRequest("get_game_modes_status");
    }

    public void GetTimeUntilMonthEnd()
    {
        SendWebSocketRequest("get_time_until_month_end");
    }

    #region Clan
    public void CreateClan(string clanName, string leaderName, int needRating = 0, bool isOpen = true)
    {
        var data = new Dictionary<string, object>
        {
            { "clan_name", clanName },
            { "leader_id", Geekplay.Instance.PlayerData.id },
            { "leader_name", leaderName },
            { "need_rating", needRating },
            { "is_open", isOpen }
        };
        SendWebSocketRequest("create_clan", data);
    }

    public void SearchClans(string searchTerm)
    {
        var data = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            data.Add("search_term", searchTerm);
        }

        SendWebSocketRequest("search_clans", data);
    }

    public void GetAllClans()
    {
        SendWebSocketRequest("get_all_clans");
    }

    public void GetClanTopWithCurrent(string clanId = null, string playerId = null)
    {
        var data = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(clanId))
            data.Add("clan_id", clanId);
        if (!string.IsNullOrEmpty(playerId))
            data.Add("player_id", playerId);
        else
        {
            Debug.LogError("Must provide either clanId or playerId");
            return;
        }

        SendWebSocketRequest("get_clan_top_with_current", data);
    }

    public void JoinClan(string clanId)
    {
        var data = new Dictionary<string, object>
        {
            { "clan_id", clanId },
            { "player_id", Geekplay.Instance.PlayerData.id },
            { "player_name", Geekplay.Instance.PlayerData.name }
        };
        SendWebSocketRequest("join_clan", data);
    }
    public void LeaveClan()
    {
        var data = new Dictionary<string, object>
        {
            { "player_id", Geekplay.Instance.PlayerData.id }
        };
        SendWebSocketRequest("leave_clan", data);
    }
    public void TransferClanLeadership(int newLeaderId)
    {
        var data = new Dictionary<string, object>
        {
            { "current_leader_id", Geekplay.Instance.PlayerData.id },
            { "new_leader_id", newLeaderId },
            { "clan_id", Geekplay.Instance.PlayerData.clanId }
        };
        SendWebSocketRequest("transfer_clan_leadership", data);
    }

    public void GetClanInfo(string clanId = null, string clanName = null)
    {
        if (string.IsNullOrEmpty(clanId) && string.IsNullOrEmpty(clanName))
        {
            Debug.LogError("Must provide either clanId or clanName");
            return;
        }

        var data = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(clanId))
            data.Add("clan_id", clanId);
        if (!string.IsNullOrEmpty(clanName))
            data.Add("clan_name", clanName);

        SendWebSocketRequest("get_clan_info", data);
    }

    public void KickClanMember(string clanId, string memberId)
    {
        var data = new Dictionary<string, object>
        {
            { "clan_id", clanId },
            { "player_id", Geekplay.Instance.PlayerData.id },
            { "member_id", memberId }
        };
        SendWebSocketRequest("kick_clan_member", data);
    }

    public void TransferClanOwnership(string clanId, int newLeaderId)
    {
        var data = new Dictionary<string, object>
        {
            { "clan_id", clanId },
            { "player_id", Geekplay.Instance.PlayerData.id },
            { "new_leader_id", newLeaderId }
        };
        SendWebSocketRequest("transfer_clan_ownership", data);
    }
    #endregion







    #region Player
    public void RegisterPlayer(string playerName, string platform = "unknown")
    {
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Player name is required");
            return;
        }

        // Normalize platform
        string normalizedPlatform = string.IsNullOrEmpty(platform) ? "unknown" : platform.ToLower();

        var data = new Dictionary<string, object>
        {
            { "player_name", playerName },
            { "platform", normalizedPlatform },
        };

        SendWebSocketRequest("register_player", data);
    }

    // Обновление имени игрока
    public void UpdatePlayerName(string playerId, string newName)
    {
        var data = new Dictionary<string, object>
        {
            { "player_id", playerId },
            { "new_name", newName }
        };
        SendWebSocketRequest("update_name", data);
    }

    public void UpdatePlayerStatsAfterBattle(
    string playerId,
    int ratingChange = 0,
    int moneyEarned = 0,
    int donatMoneyEarned = 0,
    int kills = 0,
    bool isWin = false,
    int revives = 0,
    int damageDealt = 0,
    int shotsFired = 0,
    string favoriteHero = null,
    int? clanPointsChange = null) // Делаем параметр nullable
    {
        var data = new Dictionary<string, object>
        {
            { "player_id", playerId },
            { "rating_change", ratingChange },
            { "money_change", moneyEarned },
            { "donat_money_change", donatMoneyEarned },
            { "kills", kills },
            { "is_win", isWin },
            { "revives", revives },
            { "damage_dealt", damageDealt },
            { "shots_fired", shotsFired }
        };

        if (!string.IsNullOrEmpty(favoriteHero))
        {
            data.Add("favorite_hero", favoriteHero);
        }

        // Добавляем clan_points_change только если оно указано
        if (clanPointsChange.HasValue)
        {
            data.Add("clan_points_change", clanPointsChange.Value);
        }

        SendWebSocketRequest("update_player_stats_after_battle", data);
    }

    // Метод для обновления статистики героя
    public void UpdateHeroStats(int heroId, int matchesToAdd = 1, bool isFavorite = false)
    {
        var playerData = Geekplay.Instance.PlayerData;

        if (heroId < 0 || heroId >= playerData.heroMatch.Length)
        {
            Debug.LogError($"Invalid heroId: {heroId}");
            return;
        }

        if (isFavorite)
            playerData.favoriteHero = heroId;

        Geekplay.Instance.Save();

        // Создаем список для сериализации (Unity лучше работает с List чем с массивами)
        var heroMatchList = new List<int>(playerData.heroMatch);

        var statsData = new Dictionary<string, object>
        {
            { "player_id", playerData.id },
            { "hero_id", heroId },
            { "matches_to_add", matchesToAdd },
            { "is_favorite", isFavorite },
            { "hero_match", heroMatchList } // Изменили имя поля на hero_match
        };

        SendWebSocketRequest("update_hero_stats", statsData);
    }

    public void RequestPlayerData(string playerId)
    {
        Debug.Log("RequestPlayerData");
        var data = new Dictionary<string, object>
        {
            { "player_id", playerId }
        };
        SendWebSocketRequest("get_player_data", data);
    }

    public void ClaimRewards(Dictionary<int, int> heroCards)
    {
        var data = new Dictionary<string, object>
        {
            {"player_id", Geekplay.Instance.PlayerData.id},
            {"money", Geekplay.Instance.PlayerData.money},
            {"donat_money", Geekplay.Instance.PlayerData.donatMoney},
            {"hero_cards", heroCards},
            {"open_characters", GetUpdatedOpenCharacters()}
        };

        SendWebSocketRequest("claim_rewards", data);
    }

    private Dictionary<string, int[]> GetUpdatedOpenCharacters()
    {
        var openCharacters = new Dictionary<string, int[]>();

        // Get the current open heroes from PlayerData
        int[] openHeroes = Geekplay.Instance.PlayerData.openHeroes;

        // Assuming hero IDs correspond to indices in the openHeroes array
        for (int heroId = 0; heroId < openHeroes.Length; heroId++)
        {
            // If hero is unlocked (value = 1)
            if (openHeroes[heroId] == 1)
            {
                string heroName = MainMenu.Instance.GetHeroNameById(heroId);
                if (!string.IsNullOrEmpty(heroName))
                {
                    // Create entry with unlocked status (1) and default skin states
                    // openCharacters[heroName] = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
                    openCharacters[heroName] = Geekplay.Instance.PlayerData.persons[heroId].openSkinBody;
                }
            }
        }

        return openCharacters;
    }

    public void AddCurrency(int money, int donatMoney)
    {
        var data = new Dictionary<string, object>
        {
            {"player_id", Geekplay.Instance.PlayerData.id},
            {"money", money},
            {"donat_money", donatMoney}
        };

        SendWebSocketRequest("add_currency", data);
    }
    public void UpdatePlayerCurrency(string playerId, int money, int donatMoney)
    {
        var data = new Dictionary<string, object>
        {
            {"player_id", playerId},
            {"money", money},
            {"donat_money", donatMoney}
        };

        SendWebSocketRequest("update_player_currency", data);
    }

    public void SpendHeroCards(Dictionary<int, int> cardsToSpend)
    {
        var data = new Dictionary<string, object>
        {
            {"player_id", Geekplay.Instance.PlayerData.id},
            {"cards_to_spend", cardsToSpend}
        };

        SendWebSocketRequest("spend_hero_cards", data);
    }
    #endregion







    #region InGameplay
    #region Transform and Animation
    public void SendPlayerTransformUpdate(Vector3 position, Quaternion rotation)
    {
        var player = Level.Instance.currentLevel.player;

        var data = new Dictionary<string, object>
        {
            { "player_id", Geekplay.Instance.PlayerData.id },
            { "room_id", Geekplay.Instance.PlayerData.roomId},
            { "p_x", position.x },
            { "p_y", position.y },
            { "p_z", position.z },
            { "r_x", rotation.x },
            { "r_y", rotation.y },
            { "r_z", rotation.z },
            { "r_w", rotation.w },

            { "noize_volume", player.noiseEmitter.currentNoiseRadius},
            { "current_weapon", player.Character.currentWeaponType.ToString()},

            { "isMoving", player.IsMoving },
            { "isShooting", player.IsShoot },
            { "isReloading", player.IsReload },
            { "isHealing", player.IsUseAidKit },
            { "isReviving", player.IsRevive },
            { "isPickingUp", player.IsPickingUp },
            { "isDead", player.currentState == PlayerState.Dead }
        };

        SendWebSocketRequest("update_player_transform", data);
    }
    #endregion






    #region DamageInfo
    public void SendDealDamage(Vector3 origin, Vector3 direction, int damage, float penetration)
    {

        var data = new Dictionary<string, object>
        {
            { "attacker_id", Geekplay.Instance.PlayerData.id },
            { "room_id", Geekplay.Instance.PlayerData.roomId },
            { "shot_origin_x", origin.x },
            { "shot_origin_y", origin.y },
            { "shot_origin_z", origin.z },
            { "shot_dir_x", direction.x },
            { "shot_dir_y", direction.y },
            { "shot_dir_z", direction.z },
            { "damage", damage },
            { "penetration" , penetration}
        };

        SendWebSocketRequest("deal_damage", data);
    }
    #endregion
    public void SendPlayerStatsUpdate(int kills, int deaths, bool isAlive)
    {
        var data = new Dictionary<string, object>
        {
            { "player_id", Geekplay.Instance.PlayerData.id },
            { "kills", kills },
            { "deaths", deaths },
            { "is_alive", isAlive }
        };

        SendWebSocketRequest("update_player_stats", data);
    }
    
    public void SendPlayerRespawn(float hp, float maxHp, float armor, float maxArmor, Vector3 position, Quaternion rotation)
    {
        if (Geekplay.Instance == null || Geekplay.Instance.PlayerData == null)
        {
            Debug.LogWarning("Cannot send respawn data without player info");
            return;
        }

        if (string.IsNullOrEmpty(Geekplay.Instance.PlayerData.roomId))
        {
            Debug.LogWarning("Cannot send respawn data without room id");
            return;
        }

        var data = new Dictionary<string, object>
        {
            { "player_id", Geekplay.Instance.PlayerData.id },
            { "room_id", Geekplay.Instance.PlayerData.roomId },
            { "hp", hp },
            { "max_hp", maxHp },
            { "armor", armor },
            { "max_armor", maxArmor },
            { "p_x", position.x },
            { "p_y", position.y },
            { "p_z", position.z },
            { "r_x", rotation.x },
            { "r_y", rotation.y },
            { "r_z", rotation.z },
            { "r_w", rotation.w }
        };

        SendWebSocketRequest("player_respawn", data);
    }
    #endregion





    #region LeaderBoard

    public void RequestRatingLeaderboard()
    {
        var data = new Dictionary<string, object>
        {
            {"player_id", Geekplay.Instance.PlayerData.id}
        };
        SendWebSocketRequest("get_rating_leaderboard", data);
    }

    public void RequestKillsLeaderboard()
    {
        var data = new Dictionary<string, object>
        {
            {"player_id", Geekplay.Instance.PlayerData.id}
        };
        SendWebSocketRequest("get_kills_leaderboard", data);
    }

    public void RequestHeroLevels()
    {
        var data = new Dictionary<string, object>
        {
            {"player_id", Geekplay.Instance.PlayerData.id}
        };
        SendWebSocketRequest("get_hero_levels", data);
    }

    public void UpdateHeroLevels(int heroId)
    {
        var hero = Geekplay.Instance.PlayerData.persons[heroId];
        var data = new Dictionary<string, object>
        {
            {"player_id", Geekplay.Instance.PlayerData.id},
            {"hero_id", heroId},
            {"level", hero.level},
            {"rank", hero.rank}
        };
        SendWebSocketRequest("update_hero_levels", data);
    }
    #endregion





    #region Room Management Requests
    public void JoinMatchmaking(int mode)
    {
        var data = new Dictionary<string, object>
        {
            { "player_id", Geekplay.Instance.PlayerData.id },
            { "hero_id", Geekplay.Instance.PlayerData.currentHero },
            { "hero_skin", Geekplay.Instance.PlayerData.persons[Geekplay.Instance.PlayerData.currentHero].currentBody },
            { "mode", mode }
        };
        SendWebSocketRequest("join_matchmaking", data);
    }

    public void LeaveRoom()
    {
        var data = new Dictionary<string, object>
        {
            { "player_id", Geekplay.Instance.PlayerData.id }
        };
        SendWebSocketRequest("leave_room", data);
    }

    public void GetRoomInfo(string roomId)
    {
        var data = new Dictionary<string, object>
        {
            { "room_id", roomId },
            { "player_id", Geekplay.Instance.PlayerData.id }
        };
        SendWebSocketRequest("get_room_info", data);
    }

    public void GetPlayerRooms()
    {
        var data = new Dictionary<string, object>
        {
            { "player_id", Geekplay.Instance.PlayerData.id }
        };
        SendWebSocketRequest("get_player_rooms", data);
    }

    public void GetMatchmakingStats(string mode = "all")
    {
        var data = new Dictionary<string, object>
        {
            { "mode", mode },
            { "player_id", Geekplay.Instance.PlayerData.id }
        };
        SendWebSocketRequest("get_matchmaking_stats", data);
    }

    public void ForceCloseRoom(string roomId)
    {
        var data = new Dictionary<string, object>
        {
            { "room_id", roomId },
            { "player_id", Geekplay.Instance.PlayerData.id }
        };
        SendWebSocketRequest("force_close_room", data);
    }

    public void ForceEndGame(string roomId)
    {
        var data = new Dictionary<string, object>
        {
            { "room_id", roomId },
            { "player_id", Geekplay.Instance.PlayerData.id }
        };
        SendWebSocketRequest("force_end_game", data);
    }

    public void SendPlayerReadyState(bool isReady)
    {
        var data = new Dictionary<string, object>
        {
            { "player_id", Geekplay.Instance.PlayerData.id },
            { "is_ready", isReady }
        };

        SendWebSocketRequest("update_player_ready", data);
    }

    public void RequestRoomPlayers(string roomId)
    {
        var data = new Dictionary<string, object>
        {
            { "room_id", roomId },
            { "player_id", Geekplay.Instance.PlayerData.id }
        };
        SendWebSocketRequest("get_room_players", data);
    }

    public void KickPlayerFromRoom(string targetPlayerId, string roomId)
    {
        var data = new Dictionary<string, object>
        {
            { "room_id", roomId },
            { "player_id", Geekplay.Instance.PlayerData.id },
            { "target_player_id", targetPlayerId }
        };
        SendWebSocketRequest("kick_player_from_room", data);
    }

    #endregion





    #region Boosts
    public void SendBoostsToServer(BoostList[] boostList)
    {
        var roomId = Geekplay.Instance.PlayerData.roomId;
        var playerId = Geekplay.Instance.PlayerData.id;

        var boostsData = new List<Dictionary<string, object>>();

        Debug.Log("roomId - " + roomId);
        foreach (var boostEntry in boostList)
        {
            var boost = boostEntry.boost;
            var type = boostEntry.type;

            var boostData = new Dictionary<string, object>
            {
                { "boost_id", boost.id },
                { "type", type.ToString().ToLower() }, // armor | ammo | aidkit
                { "p_x", boost.transform.position.x },
                { "p_y", boost.transform.position.y },
                { "p_z", boost.transform.position.z },
                { "is_taken", false }
            };

            boostsData.Add(boostData);
        }

        var data = new Dictionary<string, object>
        {
            { "room_id", roomId },
            { "player_id", playerId },
            { "boosts", boostsData }
        };

        SendWebSocketRequest("spawn_room_boosts", data);
    }

    public void SendBoostPickup(string roomId, string playerId, int boostId)
    {
        var data = new Dictionary<string, object>
        {
            { "room_id", roomId },
            { "player_id", playerId },
            { "boost_id", boostId }
        };

        SendWebSocketRequest("boost_pickup", data);
    }
    #endregion





    #region Upgrades
    public void SendUpgradesToServer(UpgradesList[] upgrades)
    {
        var roomId = Geekplay.Instance.PlayerData.roomId;
        var playerId = Geekplay.Instance.PlayerData.id;

        var upgradesData = new List<Dictionary<string, object>>();

        foreach (var upgradeEntry in upgrades)
        {
            var upgrade = upgradeEntry.upgrade;
            var type = upgradeEntry.type;

            var upgradeData = new Dictionary<string, object>
            {
                { "upgrade_id", upgrade.id },
                { "type", type.ToString().ToLower() },
                { "p_x", upgrade.transform.position.x },
                { "p_y", upgrade.transform.position.y },
                { "p_z", upgrade.transform.position.z },
                { "is_taken", false }
            };

            upgradesData.Add(upgradeData);
        }

        var data = new Dictionary<string, object>
        {
            { "room_id", roomId },
            { "player_id", playerId },
            { "upgrades", upgradesData }
        };

        SendWebSocketRequest("spawn_room_upgrades", data);
    }

    public void SendUpgradePickup(string roomId, string playerId, int upgradeId)
    {
        var data = new Dictionary<string, object>
        {
            { "room_id", roomId },
            { "player_id", playerId },
            { "upgrade_id", upgradeId }
        };

        SendWebSocketRequest("upgrade_pickup", data);
    }

    public void SendUpgradeDrop(Vector3 position, int upgradeId)
    {
        var data = new Dictionary<string, object>
        {
            { "room_id", Geekplay.Instance.PlayerData.roomId },
            { "player_id", Geekplay.Instance.PlayerData.id },
            { "upgrade_id", upgradeId },
            { "p_x", position.x },
            { "p_y", position.y },
            { "p_z", position.z }
        };

        SendWebSocketRequest("upgrade_drop", data);
    }
    #endregion





    #region Useable
    public void HealPlayer(string playerId, string roomId)
    {
        var data = new Dictionary<string, object>
        {
            { "player_id", playerId },
            { "room_id", roomId }
        };

        SendWebSocketRequest("heal_player", data);
    }
    #endregion
    #endregion
}