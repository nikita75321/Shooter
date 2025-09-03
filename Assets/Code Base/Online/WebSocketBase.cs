// using System;
// using System.Collections.Generic;
// using Newtonsoft.Json;
// using UnityEngine;

// [Serializable]
// public class PlayerStatsResponse
// {
//     public int rating;
//     public int best_rating;
//     public int money;
//     public int donat_money;
//     public int overral_kill;
//     public int match_count;
//     public int win_count;
//     public int revive_count;
//     public int max_damage;
//     public int shoot_count;
// }
// [Serializable]
// public class ClanInfoResponse
// {
//     public int clanId;
//     public string clanName;
//     public string myName;
//     public int myRating;
//     public int clanPoints;
//     public int playerCount;
//     public int maxPlayers;
//     public bool isOpen;
//     public int needRating;
//     public List<ClanMember> members;
// }
// [Serializable]
// public class ClanMember
// {
//     public int id;
//     public string name;
//     public bool isLeader;
//     public int rating;
//     public int points;
// }

// [Serializable]
// public class GameModesStatus
// {
//     public GameModeStatus mode1;
//     public GameModeStatus mode2;
//     public GameModeStatus mode3;
//     public long serverTime;
// }

// [Serializable]
// public class GameModeStatus
// {
//     public bool available;
//     public float timeLeft;
// }

// [Serializable]
// public class TimeUntilMonthEndResponse
// {
//     public int days;
//     public int hours;
//     public int minutes;
// }

// public class WebSocketBase : MonoBehaviour
// {
//     public static WebSocketBase Instance { get; private set; }

//     // wss://server2.growagardenoffline.online:3000
//     // wss://game.growagardenoffline.online:3000

//     protected static string WebSocketURL = "wss://game.beatragdollsandbox.space:3000";
//     protected bool IsDebug;

//     public static void InitializeSocket(bool isWebGL, bool isDebug)
//     {
//         if (isWebGL)
//             Instance = new GameObject("WebGLNetworkManager").AddComponent<WebSocketWebGL>();
//         else
//             Instance = new GameObject("AndroidNetworkManager").AddComponent<WebSocketAndroid>();

//         Instance.IsDebug = isDebug;
//     }

//     public static void Load(Action<bool> callback)
//     {
//         Instance.InitializeWebSocket(callback.Invoke);
//     }

//     #region Actions
//     public event Action OnClose;
//     public event Action<bool> DemoAction;
//     public event Action OnPlayerRegister;
//     public event Action<bool, string> OnNameUpdated;
//     public event Action<int, string> OnClanCreated;
//     public event Action<List<ClanSearch.ClanData>> OnClanListReceived;
//     public event Action<int, string> OnClanJoined;
//     public event Action<PlayerStatsResponse> OnPlayerStatsUpdated;
//     public event Action<ClanInfo> OnClanInfoReceived;
//     public event Action OnClanLeft;
//     public event Action<string, string> OnClanLeadershipTransferred;
//     public event Action<Dictionary<string, object>> OnServerDataReceived;
//     public event Action<ClanTopWithCurrentResponse> OnClanTopWithCurrentReceived;
//     public event Action<List<LeaderboardPlayer>, LeaderboardPlayer> OnRatingLeaderboardReceived;
//     public event Action<List<LeaderboardPlayer>, LeaderboardPlayer> OnKillsLeaderboardReceived;

//     public event Action<GameModesStatus> OnGameModesStatusReceived;
//     public event Action<TimeUntilMonthEndResponse> OnTimeUntilMonthEndReceived;

//     #endregion

//     protected virtual void InitializeWebSocket(Action<bool> callback) { }

//     public void UnsubscribeAllCompact()
//     {
//         ClearEvent(ref OnClose);
//     }

//     private void ClearEvent<T>(ref T handler) where T : Delegate
//     {
//         if (handler != null)
//         {
//             foreach (Delegate d in handler.GetInvocationList())
//             {
//                 handler = (T)Delegate.Remove(handler, d);
//             }
//         }
//         handler = null;
//     }

//     protected virtual void SendWebSocketRequest(string action, Dictionary<string, object> data) { }
//     protected virtual void SendWebSocketRequest(string action) { }

//     protected virtual void CloseConnect()
//     {
//         OnClose?.Invoke();
//         UnsubscribeAllCompact();
//     }

//     protected virtual void LocalSave<T>(string action, T data)
//     {
//         if (Geekplay.Instance == null || Geekplay.Instance.PlayerData == null)
//         {
//             Debug.LogError("Geekplay.Instance or PlayerData is null!");
//             return;
//         }

//         if (Geekplay_VK.Instance != null) return;

//         Geekplay.Instance.Save(); // Сохраняем изменения
//     }

//     public virtual void Shutdown() { }

//     public void OnDestroy()
//     {
//         Shutdown();
//     }

//     #region Handler
//     protected void HandleServerResponse(Dictionary<string, object> message)
//     {
//         string action = message["action"].ToString();

//         switch (action)
//         {
//             case "demo_response":
//                 HandleJoinConfirmation(message);
//                 break;
//             case "game_modes_status_response":
//                 HandleGameModesStatusResponse(message);
//                 break;
//             case "time_until_month_end_response":
//                 HandleTimeUntilMonthEndResponse(message);
//                 break;


//             case "register_player_response":
//                 HandleRegisterPlayer(message);
//                 break;
//             case "update_name_response":
//                 HandleUpdateNameResponse(message);
//                 break;
//             case "get_player_data_response":
//                 HandleGetPlayerDataResponse(message);
//                 break;


//             case "player_stats_updated":
//                 HandlePlayerStatsUpdated(message);
//                 break;
//             case "update_hero_stats_response":
//                 HandleHeroStatsUpdated(message);
//                 break;
//             case "update_hero_levels_response":
//                 HandleUpdateHeroLevelsResponse(message);
//                 break;
//             case "spend_hero_cards_response":
//                 HandleSpendHeroCardsResponse(message);
//                 break;


//             case "create_clan_response":
//                 HandleCreateClanResponse(message);
//                 break;
//             case "clan_list_response":
//                 HandleClanListResponse(message);
//                 break;
//             case "clan_search_results":
//                 HandleClanListResponse(message);
//                 break;
//             case "get_clan_info_response":
//                 HandleClanInfoResponse(message);
//                 break;
//             case "join_clan_response":
//                 HandleJoinClanResponse(message);
//                 break;
//             case "leave_clan_response":
//                 HandleLeaveClanResponse(message);
//                 break;
//             case "transfer_leadership_response":
//                 HandleTransferLeadershipResponse(message);
//                 break;
//             case "update_clan_settings_response":
//                 HandleUpdateClanSettingsResponse(message);
//                 break;
//             case "clan_info_with_top_response":
//                 HandleClanTopWithCurrentResponse(message);
//                 break;


//             case "add_currency_response":
//                 HandleAddCurrencyResponse(message);
//                 break;

//             case "rating_leaderboard_response":
//                 HandleRatingLeaderboardResponse(message);
//                 break;
//             case "kills_leaderboard_response":
//                 HandleKillsLeaderboardResponse(message);
//                 break;

//             default:
//                 Debug.LogWarning($"Received server message with action: {action}");
//                 break;
//         }
//     }

//     private void HandleJoinConfirmation(Dictionary<string, object> message)
//     {
//         bool testBool = false;
//         if (message.TryGetValue("demo_massage", out var demoMassage))
//         {
//             testBool = true;
//         }
//         DemoAction?.Invoke(testBool);
//     }

//     private void HandleRegisterPlayer(Dictionary<string, object> message)
//     {
//         WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
//         {
//             if (message.TryGetValue("success", out var successObj) && (bool)successObj)
//             {
//                 // Сохраняем player_id как строку (формат "id-рандомное_число")
//                 if (message.TryGetValue("player_id", out var playerIdObj))
//                 {
//                     string playerId = playerIdObj.ToString();
//                     Geekplay.Instance.PlayerData.id = playerId; // или отдельное поле для player_id, если нужно
//                 }

//                 // Сохраняем имя игрока
//                 if (message.TryGetValue("player_name", out var playerNameObj))
//                 {
//                     string playerName = playerNameObj.ToString();
//                     Geekplay.Instance.PlayerData.name = playerName;
//                 }

//                 Geekplay.Instance.Save();
//                 OnPlayerRegister?.Invoke();
//             }
//             else
//             {
//                 string errorMessage = message.TryGetValue("error", out var errorObj)
//                     ? errorObj.ToString()
//                     : "Unknown registration error";

//                 Debug.LogError($"Player registration failed: {errorMessage}");
//             }
//         });
//     }
//     private void HandleGetPlayerDataResponse(Dictionary<string, object> message)
//     {
//         Debug.Log("HandleGetPlayerDataResponse");
//         if (message.TryGetValue("success", out var successObj) && (bool)successObj)
//         {
//             // Debug.Log(1);
//             if (message.TryGetValue("player", out var playerObj))
//             {
//                 // Debug.Log(2);
//                 Debug.Log(playerObj.ToString());
//                 var playerData = JsonConvert.DeserializeObject<Dictionary<string, object>>(playerObj.ToString());
//                 OnServerDataReceived?.Invoke(playerData);
//                 // Debug.Log(3);
//             }
//         }
//         else
//         {
//             // Debug.Log(4);
//             Debug.LogError("Failed to load player data from server");
//         }
//     }

//     private void HandleUpdateNameResponse(Dictionary<string, object> message)
//     {
//         bool success = Convert.ToBoolean(message["success"]);
//         string newName = message.TryGetValue("player_name", out var nameObj)
//             ? nameObj.ToString()
//             : message["new_name"].ToString();

//         OnNameUpdated?.Invoke(success, newName);
//     }

//     private void HandleClanListResponse(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("clans", out var clansObj))
//         {
//             string clansJson = clansObj.ToString();
//             // Debug.Log(clansJson);
//             var clanList = JsonConvert.DeserializeObject<List<ClanSearch.ClanData>>(clansJson);
//             OnClanListReceived?.Invoke(clanList);
//         }
//     }

//     private void HandleCreateClanResponse(Dictionary<string, object> message)
//     {
//         bool success = Convert.ToBoolean(message["success"]);
//         if (success)
//         {
//             int clanId = Convert.ToInt32(message["clan_id"]);
//             string clanName = message["clan_name"].ToString();
//             OnClanCreated?.Invoke(clanId, clanName);
//         }
//     }

//     private void HandlePlayerStatsUpdated(Dictionary<string, object> message)
//     {
//         var stats = JsonConvert.DeserializeObject<PlayerStatsResponse>(message["stats"].ToString());
//         OnPlayerStatsUpdated?.Invoke(stats);
//     }

//     private void HandleHeroStatsUpdated(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("success", out var successObj) && (bool)successObj)
//         {
//             WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
//             {
//                 if (message.TryGetValue("hero_match", out var heroMatchObj))
//                 {
//                     var heroMatchList = JsonConvert.DeserializeObject<List<int>>(heroMatchObj.ToString());
//                     if (heroMatchList.Count == Geekplay.Instance.PlayerData.heroMatch.Length)
//                     {
//                         for (int i = 0; i < heroMatchList.Count; i++)
//                         {
//                             Geekplay.Instance.PlayerData.heroMatch[i] = heroMatchList[i];
//                         }
//                     }
//                 }

//                 if (message.TryGetValue("favorite_hero", out var favHeroObj))
//                 {
//                     string heroName = favHeroObj.ToString();
//                     Geekplay.Instance.PlayerData.favoriteHero = MainMenu.Instance.GetHeroIdByName(heroName);
//                 }

//                 Geekplay.Instance.Save();
//                 Debug.Log("Hero stats updated successfully");
//             });
//         }
//         else
//         {
//             Debug.LogError("Failed to update hero stats");
//         }
//     }

//     private void HandleJoinClanResponse(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("clan_id", out var clanIdObj) &&
//             message.TryGetValue("clan_name", out var clanNameObj))
//         {
//             int clanId = Convert.ToInt32(clanIdObj);
//             string clanName = clanNameObj.ToString();
//             OnClanJoined?.Invoke(clanId, clanName);
//         }
//         else
//         {
//             Debug.Log("gg");
//         }
//     }
//     private void HandleLeaveClanResponse(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("success", out var success) && (bool)success)
//         {
//             WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
//             {
//                 // Все UI и данные PlayerData обновляем только в главном потоке
//                 Geekplay.Instance.PlayerData.clanName = null;
//                 Geekplay.Instance.PlayerData.clanId = 0;
//                 Geekplay.Instance.PlayerData.isClanLeader = false;
//                 Geekplay.Instance.PlayerData.clanPoints = 0;
//                 Geekplay.Instance.Save();

//                 OnClanLeft?.Invoke();
//             });
//         }
//     }
//     private void HandleTransferLeadershipResponse(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("success", out var success) && (bool)success)
//         {
//             string newLeaderId = message["new_leader_id"].ToString();
//             string newLeaderName = message["new_leader_name"].ToString();

//             // Обновляем локальные данные, если мы передали лидерство
//             if (Geekplay.Instance.PlayerData.id == newLeaderId)
//             {
//                 Geekplay.Instance.PlayerData.isClanLeader = false;
//                 Geekplay.Instance.Save();
//             }

//             OnClanLeadershipTransferred?.Invoke(newLeaderId, newLeaderName);
//         }
//     }

//     private void HandleClaimRewardsResponse(Dictionary<string, object> message)
//     {
//         bool success = Convert.ToBoolean(message["success"]);

//         if (success)
//         {
//             WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
//             {
//                 // Обновляем деньги
//                 if (message.TryGetValue("money", out var moneyObj))
//                 {
//                     Geekplay.Instance.PlayerData.money = Convert.ToInt32(moneyObj);
//                 }

//                 if (message.TryGetValue("donat_money", out var donatMoneyObj))
//                 {
//                     Geekplay.Instance.PlayerData.donatMoney = Convert.ToInt32(donatMoneyObj);
//                 }

//                 // Обновляем карточки героев
//                 if (message.TryGetValue("hero_cards", out var cardsObj))
//                 {
//                     var updatedCards = JsonConvert.DeserializeObject<Dictionary<int, int>>(cardsObj.ToString());
//                     foreach (var card in updatedCards)
//                     {
//                         if (card.Key >= 0 && card.Key < Geekplay.Instance.PlayerData.persons.Length)
//                         {
//                             Geekplay.Instance.PlayerData.persons[card.Key].heroCard += card.Value;
//                         }
//                     }
//                 }

//                 Geekplay.Instance.Save();
//             });
//         }

//         // OnRewardsClaimed?.Invoke(success, message);
//     }
//     private void HandleAddCurrencyResponse(Dictionary<string, object> message)
//     {
//         bool success = Convert.ToBoolean(message["success"]);

//         if (success)
//         {
//             WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
//             {
//                 // Обновляем деньги
//                 if (message.TryGetValue("money", out var moneyObj))
//                 {
//                     Geekplay.Instance.PlayerData.money = Convert.ToInt32(moneyObj);
//                 }

//                 if (message.TryGetValue("donat_money", out var donatMoneyObj))
//                 {
//                     Geekplay.Instance.PlayerData.donatMoney = Convert.ToInt32(donatMoneyObj);
//                 }

//                 Geekplay.Instance.Save();
//                 Debug.Log($"Currency added. New balance: {Geekplay.Instance.PlayerData.money} money, {Geekplay.Instance.PlayerData.donatMoney} donat");
//             });
//         }
//         else
//         {
//             string error = message.TryGetValue("error", out var errorObj)
//                 ? errorObj.ToString()
//                 : "Unknown error";
//             Debug.LogError($"Failed to add currency: {error}");
//         }
//     }
//     private void HandleSpendHeroCardsResponse(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("success", out var successObj) && (bool)successObj)
//         {
//             WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
//             {
//                 // Update local hero cards if needed
//                 if (message.TryGetValue("updated_cards", out var cardsObj))
//                 {
//                     var updatedCards = JsonConvert.DeserializeObject<Dictionary<int, int>>(cardsObj.ToString());
//                     foreach (var card in updatedCards)
//                     {
//                         if (card.Key >= 0 && card.Key < Geekplay.Instance.PlayerData.persons.Length)
//                         {
//                             Geekplay.Instance.PlayerData.persons[card.Key].heroCard = card.Value;
//                         }
//                     }
//                     Geekplay.Instance.Save();
//                 }
//                 Debug.Log("Hero cards spent successfully");
//             });
//         }
//         else
//         {
//             string error = message.TryGetValue("error", out var errorObj)
//                 ? errorObj.ToString()
//                 : "Failed to spend hero cards";
//             Debug.LogError(error);
//         }
//     }

//     [System.Serializable]
//     public class ClanInfo
//     {
//         public int id;
//         public string name;
//         public ClanLeader leader;
//         public ClanStats stats;
//         public List<ClanMember> members = new List<ClanMember>();

//         // Для удобства добавим свойства с правильным именованием
//         public int Id => id;
//         public string Name => name;
//         public ClanLeader Leader => leader ?? new ClanLeader();
//         public ClanStats Stats => stats ?? new ClanStats();
//         public List<ClanMember> Members => members ?? new List<ClanMember>();
//     }

//     [System.Serializable]
//     public class ClanLeader
//     {
//         public int id;
//         public string name;
//         public int rating;
//         public int best_rating;

//         public int Id => id;
//         public string Name => name;
//         public int Rating => rating;
//         public int BestRating => best_rating;
//     }

//     [System.Serializable]
//     public class ClanStats
//     {
//         public int place;
//         public int current_level;
//         public int points;
//         public int player_count;
//         public int max_players;
//         public int need_rating;
//         public bool is_open;

//         public int Place => place;
//         public int Level => current_level;
//         public int Points => points;
//         public int PlayerCount => player_count;
//         public int MaxPlayers => max_players;
//         public int NeedRating => need_rating;
//         public bool IsOpen => is_open;
//     }

//     [System.Serializable]
//     public class ClanMember
//     {
//         public int id;
//         public string name;
//         public bool is_leader;
//         public MemberStats stats;

//         public int Id => id;
//         public string Name => name;
//         public bool IsLeader => is_leader;
//         public MemberStats Stats => stats ?? new MemberStats();
//     }

//     [System.Serializable]
//     public class MemberStats
//     {
//         public int rating;
//         public int best_rating;
//         public int clan_points;
//         public int kills;
//         public int matches;
//         public int wins;

//         public int Rating => rating;
//         public int BestRating => best_rating;
//         public int ClanPoints => clan_points;
//         public int Kills => kills;
//         public int Matches => matches;
//         public int Wins => wins;
//     }

//     public void HandleClanInfoResponse(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("success", out var successObj) && (bool)successObj)
//         {
//             // Извлекаем объект "clan" из сообщения
//             if (message.TryGetValue("clan", out var clanObj))
//             {
//                 var clanJson = JsonConvert.SerializeObject(clanObj);
//                 Debug.Log("Clan JSON: " + clanJson);

//                 var clanInfo = JsonConvert.DeserializeObject<ClanInfo>(clanJson);
//                 // Debug.Log(clanInfo.name + " - name");

//                 if (clanInfo == null)
//                 {
//                     Debug.LogError("Failed to deserialize clan info");
//                     return;
//                 }

//                 // Десериализация members отдельно, если они не попали в clanJson
//                 if (message.TryGetValue("members", out var membersObj))
//                 {
//                     var membersJson = JsonConvert.SerializeObject(membersObj);
//                     clanInfo.members = JsonConvert.DeserializeObject<List<ClanMember>>(membersJson);
//                 }

//                 clanInfo.stats ??= new ClanStats();
//                 clanInfo.leader ??= new ClanLeader();

//                 foreach (var member in clanInfo.members)
//                 {
//                     member.stats ??= new MemberStats();
//                 }

//                 OnClanInfoReceived?.Invoke(clanInfo);
//             }
//             else
//             {
//                 Debug.LogError("Clan object not found in response");
//             }
//         }
//         else
//         {
//             string errorMessage = message.TryGetValue("error", out var errorObj)
//                 ? errorObj.ToString()
//                 : "Unknown error while getting clan info";

//             Debug.LogError($"Get clan info failed: {errorMessage}");
//         }
//     }

//     private void HandleUpdateClanSettingsResponse(Dictionary<string, object> message)
//     {
//         bool success = Convert.ToBoolean(message["success"]);
//         if (success)
//         {
//             // Обновить UI или данные
//         }
//     }

//     private void HandleGameModesStatusResponse(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("modes", out var modesObj))
//         {
//             string modesJson = JsonConvert.SerializeObject(modesObj);
//             var modesStatus = JsonConvert.DeserializeObject<GameModesStatus>(modesJson);

//             // Добавляем serverTime если он есть в сообщении
//             if (message.TryGetValue("serverTime", out var serverTimeObj))
//             {
//                 modesStatus.serverTime = Convert.ToInt64(serverTimeObj);
//             }

//             OnGameModesStatusReceived?.Invoke(modesStatus);
//         }
//         else
//         {
//             Debug.LogError("Failed to parse game modes status response");
//         }
//     }

//     private void HandleTimeUntilMonthEndResponse(Dictionary<string, object> message)
//     {
//         var response = new TimeUntilMonthEndResponse
//         {
//             days = Convert.ToInt32(message["days"]),
//             hours = Convert.ToInt32(message["hours"]),
//             minutes = Convert.ToInt32(message["minutes"])
//         };

//         OnTimeUntilMonthEndReceived?.Invoke(response);
//     }

//     [Serializable]
//     public class ClanTopWithCurrentResponse
//     {
//         public bool success;
//         public List<ClanShortInfo> top_clans;
//         public ClanShortInfo current_clan;
//         public string error; // для сообщений об ошибках
//     }

//     [Serializable]
//     public class ClanShortInfo
//     {
//         public int clan_id;
//         public string clan_name;
//         public string leader_name;
//         public int leader_id;
//         public int need_rating;
//         public bool is_open;
//         public int clan_points;
//         public int clan_level;
//         public int max_players;
//         public int player_count;
//         public int place;
//     }

//     private void HandleClanTopWithCurrentResponse(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("success", out var successObj) && (bool)successObj)
//         {
//             // Debug.Log(1);
//             // Десериализуем весь ответ сразу
//             string responseJson = JsonConvert.SerializeObject(message);
//             var response = JsonConvert.DeserializeObject<ClanTopWithCurrentResponse>(responseJson);

//             OnClanTopWithCurrentReceived?.Invoke(response);
//             // Debug.Log(responseJson);
//         }
//         else
//         {
//             // Debug.Log(2);
//             string error = message.TryGetValue("error", out var errorObj)
//                 ? errorObj.ToString()
//                 : "Unknown error";
//             Debug.LogWarning($"Failed to get clan top: {error}");
//         }
//     }

//     private void HandleRatingLeaderboardResponse(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("top_players", out var topObj) &&
//             message.TryGetValue("my_stats", out var myObj))
//         {
//             var topPlayers = JsonConvert.DeserializeObject<List<LeaderboardPlayer>>(topObj.ToString());
//             var myStats = JsonConvert.DeserializeObject<LeaderboardPlayer>(myObj.ToString());
//             OnRatingLeaderboardReceived?.Invoke(topPlayers, myStats);
//         }
//     }

//     private void HandleKillsLeaderboardResponse(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("top_players", out var topObj) &&
//             message.TryGetValue("my_stats", out var myObj))
//         {
//             var topPlayers = JsonConvert.DeserializeObject<List<LeaderboardPlayer>>(topObj.ToString());
//             var myStats = JsonConvert.DeserializeObject<LeaderboardPlayer>(myObj.ToString());
//             OnKillsLeaderboardReceived?.Invoke(topPlayers, myStats);
//         }
//     }
//     #endregion

//     #region Отправка Запросов
//     public void DemoRequest()
//     {
//         SendWebSocketRequest("demo_request");
//     }

//     public void GetGameModesStatus()
//     {
//         SendWebSocketRequest("get_game_modes_status");
//     }

//     public void GetTimeUntilMonthEnd()
//     {
//         SendWebSocketRequest("get_time_until_month_end");
//     }


//     #region Clan
//     public void CreateClan(string clanName, string leaderName, int needRating = 0, bool isOpen = true)
//     {
//         var data = new Dictionary<string, object>
//         {
//             { "clan_name", clanName },
//             { "leader_id", Geekplay.Instance.PlayerData.id },
//             { "leader_name", leaderName },
//             { "need_rating", needRating },
//             { "is_open", isOpen }
//         };
//         SendWebSocketRequest("create_clan", data);
//     }

//     public void SearchClans(string searchTerm)
//     {
//         var data = new Dictionary<string, object>();

//         if (!string.IsNullOrEmpty(searchTerm))
//         {
//             data.Add("search_term", searchTerm);
//         }

//         SendWebSocketRequest("search_clans", data);
//     }

//     public void GetAllClans()
//     {
//         SendWebSocketRequest("get_all_clans");
//     }
//     public void GetClanTopWithCurrent(int? clanId = null, string playerId = null)
//     {
//         var data = new Dictionary<string, object>();

//         if (clanId.HasValue)
//             data.Add("clan_id", clanId.Value);
//         else if (!string.IsNullOrEmpty(playerId))
//             data.Add("player_id", playerId);
//         else
//         {
//             Debug.LogError("Must provide either clanId or playerId");
//             return;
//         }

//         SendWebSocketRequest("get_clan_top_with_current", data);
//     }

//     public void JoinClan(int clanId)
//     {
//         var data = new Dictionary<string, object>
//         {
//             { "clan_id", clanId },
//             { "player_id", Geekplay.Instance.PlayerData.id },
//         };
//         SendWebSocketRequest("join_clan", data);
//     }
//     public void LeaveClan()
//     {
//         var data = new Dictionary<string, object>
//         {
//             { "player_id", Geekplay.Instance.PlayerData.id }
//         };
//         SendWebSocketRequest("leave_clan", data);
//     }
//     public void TransferClanLeadership(int newLeaderId)
//     {
//         var data = new Dictionary<string, object>
//         {
//             { "current_leader_id", Geekplay.Instance.PlayerData.id },
//             { "new_leader_id", newLeaderId },
//             { "clan_id", Geekplay.Instance.PlayerData.clanId }
//         };
//         SendWebSocketRequest("transfer_clan_leadership", data);
//     }

//     public void GetClanInfo(int? clanId = null, string clanName = null)
//     {
//         if (!clanId.HasValue && string.IsNullOrEmpty(clanName))
//         {
//             Debug.LogError("Must provide either clanId or clanName");
//             return;
//         }

//         var data = new Dictionary<string, object>();

//         if (clanId.HasValue)
//             data.Add("clan_id", clanId.Value);
//         else
//             data.Add("clan_name", clanName);

//         SendWebSocketRequest("get_clan_info", data);
//     }

//     public void KickClanMember(int clanId, int memberId)
//     {
//         var data = new Dictionary<string, object>
//         {
//             { "clan_id", clanId },
//             { "player_id", Geekplay.Instance.PlayerData.id },
//             { "member_id", memberId }
//         };
//         SendWebSocketRequest("kick_clan_member", data);
//     }

//     public void TransferClanOwnership(int clanId, int newLeaderId)
//     {
//         var data = new Dictionary<string, object>
//         {
//             { "clan_id", clanId },
//             { "player_id", Geekplay.Instance.PlayerData.id },
//             { "new_leader_id", newLeaderId }
//         };
//         SendWebSocketRequest("transfer_clan_ownership", data);
//     }
//     #endregion







//     #region Player
//     public void RegisterPlayer(string playerName,
//                           string platform = "unknown",
//                           Dictionary<string, int[]> initialCharacters = null,
//                           string favoriteHero = "нет")
//     {
//         if (string.IsNullOrEmpty(playerName))
//         {
//             Debug.LogWarning("Player name is required");
//             return;
//         }

//         var openCharacters = new Dictionary<string, int[]>
//         {
//             // Пример для одного персонажа с 9 скинами (0 - закрыт, 1 - открыт)
//             // Первый скин открыт (1), остальные закрыты (0)
//             { "Kayel", new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 } }
//         };

//         string openCharactersJson = JsonConvert.SerializeObject(openCharacters);

//         // Normalize platform
//         string normalizedPlatform = string.IsNullOrEmpty(platform) ? "неизвестно" : platform.ToLower();

//         var data = new Dictionary<string, object>
//         {
//             { "player_name", playerName },
//             { "platform", normalizedPlatform },
//             { "open_characters", openCharactersJson },
//             { "love_hero", favoriteHero }
//         };

//         SendWebSocketRequest("register_player", data);
//     }

//     // Обновление имени игрока
//     public void UpdatePlayerName(string playerId, string newName)
//     {
//         var data = new Dictionary<string, object>
//         {
//             { "player_id", playerId },
//             { "new_name", newName }
//         };
//         SendWebSocketRequest("update_name", data);
//     }

//     public void UpdatePlayerStatsAfterBattle(
//     string playerId,
//     int ratingChange = 0,
//     int moneyEarned = 0,
//     int donatMoneyEarned = 0,
//     int kills = 0,
//     bool isWin = false,
//     int revives = 0,
//     int damageDealt = 0,
//     int shotsFired = 0,
//     string favoriteHero = null,
//     int? clanPointsChange = null) // Делаем параметр nullable
//     {
//         var data = new Dictionary<string, object>
//         {
//             { "player_id", playerId },
//             { "rating_change", ratingChange },
//             { "money_change", moneyEarned },
//             { "donat_money_change", donatMoneyEarned },
//             { "kills", kills },
//             { "is_win", isWin },
//             { "revives", revives },
//             { "damage_dealt", damageDealt },
//             { "shots_fired", shotsFired }
//         };

//         if (!string.IsNullOrEmpty(favoriteHero))
//         {
//             data.Add("favorite_hero", favoriteHero);
//         }

//         // Добавляем clan_points_change только если оно указано
//         if (clanPointsChange.HasValue)
//         {
//             data.Add("clan_points_change", clanPointsChange.Value);
//         }

//         SendWebSocketRequest("update_player_stats", data);
//     }

//     // Метод для обновления статистики героя
//     public void UpdateHeroStats(int heroId, int matchesToAdd = 1, bool isFavorite = false)
//     {
//         if (heroId < 0 || heroId >= Geekplay.Instance.PlayerData.heroMatch.Length)
//         {
//             Debug.LogError($"Invalid heroId: {heroId}");
//             return;
//         }

//         var playerData = Geekplay.Instance.PlayerData;

//         if (isFavorite)
//             playerData.favoriteHero = heroId;

//         Geekplay.Instance.Save();

//         // Создаем список для сериализации (Unity лучше работает с List чем с массивами)
//         var heroMatchList = new List<int>(playerData.heroMatch);

//         var statsData = new Dictionary<string, object>
//         {
//             { "player_id", playerData.id },
//             { "hero_id", heroId },
//             { "matches_to_add", matchesToAdd },
//             { "is_favorite", isFavorite },
//             { "hero_match", heroMatchList } // Изменили имя поля на hero_match
//         };

//         SendWebSocketRequest("update_hero_stats", statsData);
//     }

//     public void RequestPlayerData(string playerId)
//     {
//         Debug.Log("RequestPlayerData");
//         var data = new Dictionary<string, object>
//         {
//             { "player_id", playerId }
//         };
//         SendWebSocketRequest("get_player_data", data);
//     }

//     public void ClaimRewards(int money, int donatMoney, Dictionary<int, int> heroCards)
//     {
//         var data = new Dictionary<string, object>
//         {
//             {"player_id", Geekplay.Instance.PlayerData.id},
//             {"money", money},
//             {"donat_money", donatMoney},
//             {"hero_cards", heroCards},
//             {"open_characters", GetUpdatedOpenCharacters()}
//         };

//         SendWebSocketRequest("claim_rewards", data);
//     }

//     private Dictionary<string, int[]> GetUpdatedOpenCharacters()
//     {
//         var openCharacters = new Dictionary<string, int[]>();

//         // Get the current open heroes from PlayerData
//         int[] openHeroes = Geekplay.Instance.PlayerData.openHeroes;

//         // Assuming hero IDs correspond to indices in the openHeroes array
//         for (int heroId = 0; heroId < openHeroes.Length; heroId++)
//         {
//             // If hero is unlocked (value = 1)
//             if (openHeroes[heroId] == 1)
//             {
//                 string heroName = MainMenu.Instance.GetHeroNameById(heroId);
//                 if (!string.IsNullOrEmpty(heroName))
//                 {
//                     // Create entry with unlocked status (1) and default skin states
//                     // openCharacters[heroName] = new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
//                     openCharacters[heroName] = Geekplay.Instance.PlayerData.persons[heroId].openSkinBody;
//                 }
//             }
//         }

//         return openCharacters;
//     }

//     public void AddCurrency(int money, int donatMoney)
//     {
//         var data = new Dictionary<string, object>
//         {
//             {"player_id", Geekplay.Instance.PlayerData.id},
//             {"money", money},
//             {"donat_money", donatMoney}
//         };

//         SendWebSocketRequest("add_currency", data);
//     }
//     public void UpdatePlayerCurrency(string playerId, int money, int donatMoney)
//     {
//         var data = new Dictionary<string, object>
//         {
//             {"player_id", playerId},
//             {"money", money},
//             {"donat_money", donatMoney}
//         };

//         SendWebSocketRequest("update_player_currency", data);
//     }

//     public void SpendHeroCards(Dictionary<int, int> cardsToSpend)
//     {
//         var data = new Dictionary<string, object>
//         {
//             {"player_id", Geekplay.Instance.PlayerData.id},
//             {"cards_to_spend", cardsToSpend}
//         };

//         SendWebSocketRequest("spend_hero_cards", data);
//     }

//     private void HandleUpdateHeroLevelsResponse(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("success", out var successObj) && (bool)successObj)
//         {
//             int heroId = Convert.ToInt32(message["hero_id"]);
//             int level = Convert.ToInt32(message["level"]);
//             int rank = Convert.ToInt32(message["rank"]);

//             // Обновляем локальные данные
//             var hero = Geekplay.Instance.PlayerData.persons[heroId];
//             hero.level = level;
//             hero.rank = rank;

//             Debug.Log($"Successfully updated hero {heroId}: level={level}, rank={rank}");
//         }
//         else
//         {
//             string error = message.TryGetValue("message", out var errorObj)
//                 ? errorObj.ToString()
//                 : "Unknown error";
//             Debug.LogError($"Failed to update hero levels: {error}");
//         }
//     }

//     #endregion

//     #region LeaderBoard

//     public void RequestRatingLeaderboard()
//     {
//         var data = new Dictionary<string, object>
//         {
//             {"player_id", Geekplay.Instance.PlayerData.id}
//         };
//         SendWebSocketRequest("get_rating_leaderboard", data);
//     }

//     public void RequestKillsLeaderboard()
//     {
//         var data = new Dictionary<string, object>
//         {
//             {"player_id", Geekplay.Instance.PlayerData.id}
//         };
//         SendWebSocketRequest("get_kills_leaderboard", data);
//     }

//     public void RequestHeroLevels()
//     {
//         var data = new Dictionary<string, object>
//         {
//             {"player_id", Geekplay.Instance.PlayerData.id}
//         };
//         SendWebSocketRequest("get_hero_levels", data);
//     }

//     public void UpdateHeroLevels(int heroId)
//     {
//         var hero = Geekplay.Instance.PlayerData.persons[heroId];
//         var data = new Dictionary<string, object>
//         {
//             {"player_id", Geekplay.Instance.PlayerData.id},
//             {"hero_id", heroId},
//             {"level", hero.level},
//             {"rank", hero.rank}
//         };
//         SendWebSocketRequest("update_hero_levels", data);
//     }
//     #endregion

//     #endregion
// }