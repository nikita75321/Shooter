// using System.Collections;
// using UnityEngine;
// using System.Collections.Generic;
// using System;

// public class MatchmakingManager : MonoBehaviour
// {
//     private string currentRoomId;
//     private int currentMode;
//     private Coroutine matchmakingCoroutine;

//     // public void JoinMatchmaking(int mode)
//     // {
//     //     if (WebSocketBase.Instance != null)
//     //     {
//     //         currentMode = mode;
            
//     //         var joinMessage = new Dictionary<string, object>
//     //         {
//     //             { "action", "join_matchmaking" },
//     //             { "player_id", PlayerPrefs.GetString("PlayerId") },
//     //             { "mode", mode }
//     //         };
            
//     //         // WebSocketBase.Instance.SendWebSocketRequest("join_matchmaking", joinMessage);
            
//     //         // Запускаем корутину для отслеживания статуса
//     //         if (matchmakingCoroutine != null)
//     //             StopCoroutine(matchmakingCoroutine);
            
//     //         matchmakingCoroutine = StartCoroutine(MatchmakingStatusUpdate());
//     //     }
//     // }

//     private IEnumerator MatchmakingStatusUpdate()
//     {
//         while (true)
//         {
//             // Можно отправлять периодические запросы о статусе
//             // или ждать уведомлений от сервера
//             yield return new WaitForSeconds(5f);
            
//             if (!string.IsNullOrEmpty(currentRoomId))
//             {
//                 var statusMessage = new Dictionary<string, object>
//                 {
//                     { "action", "matchmaking_status" },
//                     { "room_id", currentRoomId }
//                 };
                
//                 // WebSocketBase.Instance.SendWebSocketRequest("matchmaking_status", statusMessage);
//             }
//         }
//     }

//     public void LeaveMatchmaking()
//     {
//         if (!string.IsNullOrEmpty(currentRoomId))
//         {
//             var leaveMessage = new Dictionary<string, object>
//             {
//                 { "action", "leave_matchmaking" },
//                 { "room_id", currentRoomId }
//             };
            
//             // WebSocketBase.Instance.SendWebSocketRequest("leave_matchmaking", leaveMessage);
//             currentRoomId = null;
            
//             if (matchmakingCoroutine != null)
//                 StopCoroutine(matchmakingCoroutine);
//         }
//     }

//     // Метод для подписки на события WebSocketBase
//     public void SubscribeToWebSocketEvents()
//     {
//         if (WebSocketBase.Instance != null)
//         {
//             // Подписываемся на обработку сообщений через общий обработчик
//             WebSocketBase.Instance.OnServerDataReceived += HandleServerData;
//         }
//     }

//     // Метод для отписки от событий
//     public void UnsubscribeFromWebSocketEvents()
//     {
//         if (WebSocketBase.Instance != null)
//         {
//             WebSocketBase.Instance.OnServerDataReceived -= HandleServerData;
//         }
//     }

//     // Обработчик сообщений от WebSocketBase
//     private void HandleServerData(Dictionary<string, object> message)
//     {
//         if (message.TryGetValue("action", out var actionObj))
//         {
//             string action = actionObj.ToString();
            
//             switch (action)
//             {
//                 case "matchmaking_joined":
//                     if (message.TryGetValue("room_id", out var roomIdObj))
//                     {
//                         currentRoomId = roomIdObj.ToString();
                        
//                         int playersInRoom = message.TryGetValue("players_in_room", out var playersObj) ? 
//                             Convert.ToInt32(playersObj) : 0;
//                         int maxPlayers = message.TryGetValue("max_players", out var maxObj) ? 
//                             Convert.ToInt32(maxObj) : 0;
//                         float estimatedWait = message.TryGetValue("estimated_wait", out var waitObj) ? 
//                             Convert.ToSingle(waitObj) : 0f;
                        
//                         // UIManager.Instance.ShowMatchmakingStatus(
//                         //     playersInRoom, 
//                         //     maxPlayers,
//                         //     estimatedWait
//                         // );
//                     }
//                     break;
                    
//                 case "match_start":
//                     if (message.TryGetValue("match_id", out var matchIdObj))
//                     {
//                         // // Начинаем загрузку игры
//                         // SceneManager.LoadScene("GameScene");
//                         // // Сохраняем информацию о матче
//                         // PlayerPrefs.SetString("CurrentMatch", matchIdObj.ToString());
//                     }
//                     break;
                    
//                 case "matchmaking_full":
//                     // UIManager.Instance.ShowMessage("Все комнаты заполнены. Попробуйте позже.");
//                     break;
//             }
//         }
//     }

//     private void OnEnable()
//     {
//         SubscribeToWebSocketEvents();
//     }

//     private void OnDisable()
//     {
//         UnsubscribeFromWebSocketEvents();
//     }

//     private void OnDestroy()
//     {
//         UnsubscribeFromWebSocketEvents();
//     }
// }

// [System.Serializable]
// public class MatchmakingMessage
// {
//     public string action;
//     public string room_id;
//     public int players_in_room;
//     public int max_players;
//     public float estimated_wait;
//     public string match_id;
// }