using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using HybridWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class WebSocketWebGL : WebSocketBase {

    private WebSocket _webSocket;
    protected override void InitializeWebSocket(Action<bool> callback)
    {
        if (_webSocket != null && _webSocket.GetState() != WebSocketState.Closed)
        {
            _webSocket.Close();
            _webSocket = null;
        }
        
        // bool onOpen = false;

        _webSocket = WebSocketFactory.CreateInstance(WebSocketURL);

        _webSocket.OnOpen += () =>
        {
            if (IsDebug) Debug.Log($"WebSocket GL {WebSocketURL} connected!");
            callback?.Invoke(true);
        };

        _webSocket.OnMessage += OnWebSocketMessage;

        _webSocket.OnError += (string errMsg) =>
        {
            Debug.LogWarning($"WebSocket WebGL error: {errMsg}");
        };

        _webSocket.OnClose += (WebSocketCloseCode code) =>
        {
            CloseConnect();
            if(IsDebug) Debug.Log($"WebSocket closed: {code}");
            callback?.Invoke(false);
        };

        _webSocket.Connect();
    }

    private void OnWebSocketMessage(byte[] message) 
    {
        try 
        {
            if (message == null || message.Length == 0)
            {
                Debug.LogWarning("Received empty message in OnWebSocketMessage");
                return;
            }
    
            string json = Encoding.UTF8.GetString(message);

            if (IsDebug)
            {
                try
                {
                    // Парсим JSON в JObject
                    var jsonObj = JObject.Parse(json);

                    // Проверяем наличие поля "action"
                    if (jsonObj["action"] != null)
                    {
                        string actionName = jsonObj["action"].ToString();
                        string prettyJson = jsonObj.ToString(Formatting.Indented);

                        //-----ВЕРНУТЬ!-----
                        Debug.Log($"<color=yellow>{actionName}</color>:\n{prettyJson}");
                    }
                    else
                    {
                        Debug.LogWarning("JSON does not contain 'action' field!");
                        Debug.Log($"Raw JSON:\n{json}");
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError($"JSON parsing failed: {ex.Message}\nRaw data: {json}");
                }
            }
            
            var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (response != null && response.ContainsKey("action")) 
            {
                HandleServerResponse(response);
            }
            else
            {
                Debug.LogError("Invalid message format - missing 'action' field");
            }
        } 
        catch (Exception ex) 
        {
            Debug.LogError($"Error processing message: {ex}\n{ex.StackTrace}");
        }
    }
    
    // Метод для отправки бинарных сообщений с action
    protected override void SendWebSocketRequest(string action, Dictionary<string, object> data) {

        // Добавляем action в данные
        if (_webSocket == null)
        {
            //InitializeWebSocket(null);
            return;
        }
        
        switch (_webSocket.GetState())
        {
            case WebSocketState.Connecting:
                if (IsDebug) Debug.Log("Подключаюсь");
                return;
            case WebSocketState.Open:
                break;
            case WebSocketState.Closing:
                if (IsDebug) Debug.Log("Закрываюсь");
                return;
            case WebSocketState.Closed:
                if (IsDebug) Debug.Log("закрыт");
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        

        var requestData = new Dictionary<string, object>
        {
            { "action", action },
        };

        foreach (var kvp in data)
        {
            requestData[kvp.Key] = kvp.Value;
        }

        string json = JsonConvert.SerializeObject(requestData);
        if (IsDebug) Debug.Log($"Sending WebSocket message: {json}");
        
        // Конвертируем action и JSON в байты
        byte[] actionBytes = Encoding.UTF8.GetBytes(action);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
    
        // Создаем общий буфер: [длина action (1 байт)][action][json]
        byte[] buffer = new byte[1 + actionBytes.Length + jsonBytes.Length];
    
        // Записываем длину action (1 байт)
        buffer[0] = (byte)actionBytes.Length;
    
        // Копируем action
        Buffer.BlockCopy(actionBytes, 0, buffer, 1, actionBytes.Length);
    
        // Копируем json
        Buffer.BlockCopy(jsonBytes, 0, buffer, 1 + actionBytes.Length, jsonBytes.Length);
        
        try
        {
            _webSocket.Send(buffer);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error sending WebSocket message: {ex.Message}");
        }
        data["action"] = action;
    
        // Сериализуем в JSON
    }

    protected override void SendWebSocketRequest(string action)
    {
        switch (_webSocket.GetState())
        {
            case WebSocketState.Connecting:
                if (IsDebug) Debug.Log("Подключаюсь");
                return;
            case WebSocketState.Open:
                break;
            case WebSocketState.Closing:
                if (IsDebug) Debug.Log("Закрываюсь");
                return;
            case WebSocketState.Closed:
                if (IsDebug) Debug.Log("закрыт");
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        // Добавляем action в данные
        if (_webSocket == null)
        {
            //InitializeWebSocket(null);
            return;
        }

        var requestData = new Dictionary<string, object>
        {
            { "action", action },
        };
        
        string json = JsonConvert.SerializeObject(requestData);
        if (IsDebug) Debug.Log($"Sending WebSocket message: {json}");
        
        // Конвертируем action и JSON в байты
        byte[] actionBytes = Encoding.UTF8.GetBytes(action);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
    
        // Создаем общий буфер: [длина action (1 байт)][action][json]
        byte[] buffer = new byte[1 + actionBytes.Length + jsonBytes.Length];
    
        // Записываем длину action (1 байт)
        buffer[0] = (byte)actionBytes.Length;
    
        // Копируем action
        Buffer.BlockCopy(actionBytes, 0, buffer, 1, actionBytes.Length);
    
        // Копируем json
        Buffer.BlockCopy(jsonBytes, 0, buffer, 1 + actionBytes.Length, jsonBytes.Length);
        
        try
        {
            _webSocket.Send(buffer);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending WebSocket message: {ex.Message}");
        }
        
    }

    public override void Shutdown()
    {
        if (_webSocket != null)
        {
            _webSocket.Close();
            _webSocket = null;
        }
        
        UnsubscribeAllCompact();
    }
}