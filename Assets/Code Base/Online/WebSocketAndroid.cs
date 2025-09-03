// using UnityEngine;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Text;
// using Newtonsoft.Json;
// using WebSocketSharp;

// public class WebSocketAndroid : WebSocketBase
// {
//     private WebSocket _webSocket;

//     protected override void InitializeWebSocket(Action<bool> callback)
//     {
//         if (_webSocket != null && _webSocket.IsAlive)
//         {
//             _webSocket.Close();
//             _webSocket = null;
//         }
        
//         _webSocket = new WebSocket(WebSocketURL);
        
//         _webSocket.OnMessage += OnWebSocketMessage;
//         _webSocket.OnOpen += (sender, e) =>
//         {
//             callback?.Invoke(true);
//             Debug.Log("WebSocket Android connected");
//         };
//         _webSocket.OnError += (sender, e) =>
//         {
//             //GameManager.Instance.Analytics.SendEvent($"SocketError_{e.Message}", true);
//             Debug.LogError($"WebSocket Android error: {e.Message}");
//         };
//         _webSocket.OnClose += (sender, e) =>
//         {
//             CloseConnect();
//             callback?.Invoke(false);
//             Debug.Log($"WebSocket closed: {e.Reason}");
//         };

//          _webSocket.Connect();
//     }
    
//     public override void Shutdown()
//     {
//         if (_webSocket != null)
//         {
//             _webSocket.Close();
//             _webSocket = null;
//         }

//         UnsubscribeAllCompact();
//     }

//     #region Обрабока ответов
    

//     private void OnWebSocketMessage(object sender, MessageEventArgs e)
//     {
//         if (IsDebug) Debug.Log($"Received WebSocket Android message: {e.Data}");

//         try
//         {
//             var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.Data);

//             if (response.ContainsKey("action"))
//             {
//                 HandleServerResponse(response);
//             }
//         }
//         catch (Exception ex)
//         {
//             //GameManager.Instance.Analytics.SendEvent($"ResponseSocketError_{ex}", true);
//             Debug.LogError($"Error processing WebSocket Android message: {ex.Message}");
//         }
//     }
    
//     protected override void SendWebSocketRequest(string action, Dictionary<string, object> data) {
        
//         if (_webSocket == null || !_webSocket.IsAlive )
//         {
//             if (IsDebug) Debug.Log("закрыт");
//             return;
//         }
        
//         var requestData = new Dictionary<string, object>
//         {
//             { "action", action },
//         };

//         foreach (var kvp in data)
//         {
//             requestData[kvp.Key] = kvp.Value;
//         }

//         string json = JsonConvert.SerializeObject(requestData);
//         if (IsDebug) Debug.Log($"Sending WebSocket message: {json}");
        
//         // Конвертируем action и JSON в байты
//         byte[] actionBytes = Encoding.UTF8.GetBytes(action);
//         byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
    
//         // Создаем общий буфер: [длина action (1 байт)][action][json]
//         byte[] buffer = new byte[1 + actionBytes.Length + jsonBytes.Length];
    
//         // Записываем длину action (1 байт)
//         buffer[0] = (byte)actionBytes.Length;
    
//         // Копируем action
//         Buffer.BlockCopy(actionBytes, 0, buffer, 1, actionBytes.Length);
    
//         // Копируем json
//         Buffer.BlockCopy(jsonBytes, 0, buffer, 1 + actionBytes.Length, jsonBytes.Length);
        
//         try
//         {
//             _webSocket.Send(buffer);
//         }
//         catch (Exception ex)
//         {
//             //GameManager.Instance.Analytics.SendEvent($"SendSocketError_{ex}", true);
//             Debug.LogError($"Error sending WebSocket message: {ex.Message}");
//         }
//         data["action"] = action;
    
//         // Сериализуем в JSON
//     }

//     protected override void SendWebSocketRequest(string action)
//     {
//         if (_webSocket == null || !_webSocket.IsAlive )
//         {
//             if (IsDebug) Debug.Log("закрыт");
//             return;
//         }

//         var requestData = new Dictionary<string, object>
//         {
//             { "action", action },
//         };

//         string json = JsonConvert.SerializeObject(requestData);
//         if (IsDebug) Debug.Log($"Sending WebSocket message: {json}");
        
//         // Конвертируем action и JSON в байты
//         byte[] actionBytes = Encoding.UTF8.GetBytes(action);
//         byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
    
//         // Создаем общий буфер: [длина action (1 байт)][action][json]
//         byte[] buffer = new byte[1 + actionBytes.Length + jsonBytes.Length];
    
//         // Записываем длину action (1 байт)
//         buffer[0] = (byte)actionBytes.Length;
    
//         // Копируем action
//         Buffer.BlockCopy(actionBytes, 0, buffer, 1, actionBytes.Length);
    
//         // Копируем json
//         Buffer.BlockCopy(jsonBytes, 0, buffer, 1 + actionBytes.Length, jsonBytes.Length);
        
//         try
//         {
//             _webSocket.Send(buffer);
//         }
//         catch (Exception ex)
//         {
//             //GameManager.Instance.Analytics.SendEvent($"SendSocketError_{ex}", true);
//             Debug.LogError($"Error sending WebSocket message: {ex.Message}");
//         }
//     }
//     #endregion
// }