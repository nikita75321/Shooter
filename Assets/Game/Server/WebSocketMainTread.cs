using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class WebSocketMainTread : MonoBehaviour
{
    public static WebSocketMainTread Instance;

    #region Queue actions
    [ShowInInspector] public Queue<Action> mainTreadAction = new();
    private void Update()
    {
        while (mainTreadAction.Count > 0)
        {
            var action = mainTreadAction.Dequeue();
            action?.Invoke();
        }
    }
    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            // Debug.Log(1);
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Debug.Log(2);
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // InitSocket();
        // StartCoroutine(InitWebSocket());
    }
    
    // private void InitSocket()
    // {
    //     // Проверяем, не инициализирован ли уже сокет
    //     if (WebSocketBase.Instance == null)
    //     {
    //         WebSocketBase.InitializeSocket(true, true);
    //     }
    //     else
    //     {
    //         // Если сокет уже есть, просто подписываемся на события
    //         SetupSocketCallbacks();
    //     }
    // }

    // private void SetupSocketCallbacks()
    // {
    //     WebSocketBase.Instance.OnClose += (() =>
    //     {
    //         Debug.Log("socket turn off");
    //     });
    // }

    // private IEnumerator InitWebSocket()
    // {
    //     bool wait = false;

    //     SetupSocketCallbacks();

    //     WebSocketBase.Load((status =>
    //     {
    //         if (status)
    //         {
    //             Debug.Log("socket on");
    //         }
    //         else
    //         {
    //             Debug.Log("socket off");
    //         }
    //         wait = true;
    //     }));

    //     yield return new WaitUntil(() => wait);
    //     //----------------Server init----------------
    //     // WebSocketBase.Instance.DemoRequest();
    //     WebSocketBase.Instance.RequestPlayerData(Geekplay.Instance.PlayerData.id);	
    //     Debug.Log("Load server data");
    // }
}
