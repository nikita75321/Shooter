using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    private string BASE_URL = "https://game.growagardenoffline.online/api.php";
    public bool debug = false;
    
    private IEnumerator ApiRequest(string action, Dictionary<string, string> data, 
        Action<Dictionary<string, object>> onSuccess, Action<Dictionary<string, object>> onError)
    {
        if (data == null || data.Count == 0)
        {
            onError?.Invoke(new Dictionary<string, object> {
                {"error", "Request data is null or empty"},
                {"action", action}
            });
            yield break;
        }

        string jsonData;
        
        try
        {
            jsonData = JsonConvert.SerializeObject(data);
            if (debug) Debug.Log($"{action} Sending JSON: {jsonData}");
        }
        catch (Exception ex)
        {
            onError?.Invoke(new Dictionary<string, object> {
                {"error", $"JSON Serialization Error: {ex.Message}"},
                {"action", action},
                {"stackTrace", ex.StackTrace}
            });
            yield break;
        }

        using (UnityWebRequest www = new UnityWebRequest($"{BASE_URL}?action={action}", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                var errorResponse = new Dictionary<string, object> {
                    {"error", www.error},
                    {"response", www.downloadHandler.text},
                    {"action", action},
                    {"httpCode", www.responseCode}
                };
                
                if (debug) Debug.LogWarning($"{action} Error: {JsonConvert.SerializeObject(errorResponse)}");
                
                onError?.Invoke(errorResponse);
                yield break;
            }
            
            if (debug) Debug.Log($"{action} Raw response: {www.downloadHandler.text}");
            
            try
            {
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(www.downloadHandler.text);
                
                if (response.ContainsKey("success") && (bool)response["success"] == false)
                {
                    // Обработка ошибок от API
                    var errorResponse = new Dictionary<string, object> {
                        {"error", response.ContainsKey("message") ? response["message"] : "Unknown API error"},
                        {"action", action},
                        {"apiResponse", response}
                    };
                    
                    if (debug) Debug.LogWarning($"{action} API Error: {JsonConvert.SerializeObject(errorResponse)}");
                    
                    onError?.Invoke(errorResponse);
                    yield break;
                }
                
                try
                {
                    onSuccess?.Invoke(response);
                }
                catch (Exception e)
                {
                    onError?.Invoke(new Dictionary<string, object> {
                        {"error", $"{action} onSuccess Error: {e.Message}"},
                        {"action", action},
                        {"stackTrace", e.StackTrace}
                    });
                }
            }
            catch (Exception e)
            {
                onError?.Invoke(new Dictionary<string, object> {
                    {"error", $"{action} JSON Parse Error: {e.Message}"},
                    {"action", action},
                    {"stackTrace", e.StackTrace},
                    {"rawResponse", www.downloadHandler.text}
                });
            }
        }
    }
     
     public void RegisterNewUserAndEntry(string playerID,bool testServer, Action<string,string> newName, Action<Dictionary<string, object>> onError)
     {
         Dictionary<string, string> requestData = new Dictionary<string, string>()
         {
             {"player_id", playerID},
             {"is_test", testServer.ToString().ToLower()},
         };
        
         StartCoroutine(ApiRequest(
             "player_register",
             requestData,
             (response) =>
             {
                 if (response.ContainsKey("server_url") && response.ContainsKey("player_id"))
                 {
                     newName?.Invoke(response["server_url"].ToString(),response["player_id"].ToString());
                 }
                 else
                 {
                     onError?.Invoke(new Dictionary<string, object> {
                         {"error", "Missing required fields in response"},
                         {"action", "player_register"},
                         {"response", response}
                     });
                 }
             },
             onError
         ));
     }

     public void EntryServer(string playerID,bool testServer, Action<string> callback, Action<Dictionary<string, object>> onError)
     {
         Dictionary<string, string> requestData = new Dictionary<string, string>()
         {
             {"player_id", playerID},
             {"is_test", testServer.ToString()},
         };

         StartCoroutine(ApiRequest(
             "player_entry",
             requestData,
             (response) =>
             {
                 if (response.ContainsKey("server_url"))
                 {
                     callback?.Invoke(response["server_url"].ToString());
                 }
                 else
                 {
                     onError?.Invoke(new Dictionary<string, object> {
                         {"error", "Missing server_url in response"},
                         {"action", "player_entry"},
                         {"response", response}
                     });
                 }
             },
             onError
         ));
     }

     public void SendReport(string playerID, string reportText, Action<bool> callback)
     {
         Dictionary<string, string> requestData = new Dictionary<string, string>()
         {
             {"player_id", playerID},
             {"text", reportText}
         };
    
         StartCoroutine(ApiRequest(
             "user_reports",
             requestData,
             (response) =>
             {
                 if (response.ContainsKey("success"))
                 {
                     callback?.Invoke((bool)response["success"]);
                 }
             },
             (i)=>
             {
                 callback.Invoke(false);
             }));
         
     }
}