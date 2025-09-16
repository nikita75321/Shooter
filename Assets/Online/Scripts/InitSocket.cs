using System;
using UnityEngine;

public class InitSocket : MonoBehaviour
{
    public static Action<bool> socketConnected;

    [SerializeField] private bool isWebGL, isDebug, testServer;
    [SerializeField] private string testURL;
    [SerializeField] private string prodURL;

    private Action<bool> socketConnectedHandler;
    private void Awake()
    {
        // Debug.Log("00");
        socketConnectedHandler = status =>
        {
            // Debug.Log(0);
            if (status)
            {
                // Debug.Log(1);
                if (!string.IsNullOrEmpty(Geekplay.Instance.PlayerData.id))
                {
                    Debug.Log("connect");
                    // Подключаем игрока к серверу
                    WebSocketBase.Instance.ConnectPlayer(
                        Geekplay.Instance.PlayerData.id,
                        Geekplay.Instance.PlayerData.name
                    );
                } else
                Debug.Log(2);
            }
        };
        
        socketConnected += socketConnectedHandler;

        InitializeWebSocket();
        DG.Tweening.DOTween.SetTweensCapacity(500, 50);
        DG.Tweening.DOTween.logBehaviour = DG.Tweening.LogBehaviour.Verbose;
    }

    private void InitializeWebSocket()
    {
        if (testServer)
            WebSocketBase.InitializeSocket(isWebGL, isDebug, testURL);
        else
            WebSocketBase.InitializeSocket(isWebGL, isDebug, prodURL);

        WebSocketBase.Instance.OnClose += SocketClosed;

        WebSocketBase.Load(status =>
        {
            // Debug.Log("aaa");
            socketConnected.Invoke(status);
        });
    }

    private void Start()
    {
        
    }

    private void SocketClosed()
    {
        // MainMenu.Instance.SaveHeroStatsToDatabase();
        Debug.Log("Connection Closed");
    }

    private void OnDestroy()
    {
        // Правильно отписываемся от сохраненного делегата
        if (socketConnectedHandler != null)
        {
            socketConnected -= socketConnectedHandler;
        }
        
        // Также отписываемся от других событий
        if (WebSocketBase.Instance != null)
        {
            WebSocketBase.Instance.OnClose -= SocketClosed;
        }
    }
}
