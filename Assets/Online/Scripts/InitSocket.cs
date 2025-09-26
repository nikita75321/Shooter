using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

public class InitSocket : MonoBehaviour
{
    public static InitSocket Instance { get; private set; }
    public static Action<bool> socketConnected;

    [Header("Status")]
    [ShowInInspector] public static bool ISCONECTED;

    [SerializeField] private bool isWebGL, isDebug, testServer;
    [SerializeField] private string testURL;
    [SerializeField] private string prodURL;

    [Header("Reconnect Settings")]
    [SerializeField] private float autoReconnectTimeout = 10f;
    [SerializeField] private float reconnectAttemptInterval = 1f;

    private Action<bool> socketConnectedHandler;
    private Coroutine reconnectCoroutine;
    private bool isReconnecting;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        socketConnectedHandler = status =>
        {
            if (status)
            {
                if (!string.IsNullOrEmpty(Geekplay.Instance.PlayerData.id))
                {
                    Debug.Log("connect");
                    // Подключаем игрока к серверу
                    WebSocketBase.Instance.ConnectPlayer(
                        Geekplay.Instance.PlayerData.id,
                        Geekplay.Instance.PlayerData.name
                    );
                }
                else
                    Debug.Log(2);
                ISCONECTED = true;
            }
            else
            {
                ISCONECTED = false;
            }
        };

        socketConnected += socketConnectedHandler;

        InitializeWebSocket();
        DG.Tweening.DOTween.SetTweensCapacity(500, 50);
        DG.Tweening.DOTween.logBehaviour = DG.Tweening.LogBehaviour.Verbose;
    }

    private void Start()
    {
        StartCoroutine(PingPong());
    }

    private static WaitForSeconds _waitForSeconds1 = new(1);
    private IEnumerator PingPong()
    {
        while (true)
        {
            yield return _waitForSeconds1;
            WebSocketBase.Instance.PingPong();
        }
    }

    private void InitializeWebSocket()
    {
        StartConnectionAttempt(null);
    }

    private bool EnsureSocketInitialized()
    {
        if (WebSocketBase.Instance == null)
        {
            if (testServer)
                WebSocketBase.InitializeSocket(isWebGL, isDebug, testURL);
            else
                WebSocketBase.InitializeSocket(isWebGL, isDebug, prodURL);
        }

        if (WebSocketBase.Instance == null)
        {
            Debug.LogError("WebSocketBase instance is not available.");
            return false;
        }

        WebSocketBase.Instance.OnClose -= SocketClosed;
        WebSocketBase.Instance.OnClose += SocketClosed;
        return true;
    }

    private void StartConnectionAttempt(Action<bool> onCompleted)
    {
        if (!EnsureSocketInitialized())
        {
            return;
        }

        WebSocketBase.Load(status =>
        {
            // socketConnected.Invoke(status);
            if (status)
            {
                isReconnecting = false;                
            }
            else
            {
                ISCONECTED = false;
            }

            socketConnected?.Invoke(status);
            onCompleted?.Invoke(status);

        });
    }

    private void StartReconnectRoutine()
    {
        if (isReconnecting || !isActiveAndEnabled)
        {
            return;
        }

        isReconnecting = true;
        reconnectCoroutine = StartCoroutine(AutoReconnectCoroutine());
    }
    private void StopReconnectRoutine()
    {
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
            reconnectCoroutine = null;
        }

        isReconnecting = false;
    }

    private IEnumerator AutoReconnectCoroutine()
    {
        isReconnecting = true;
        float startTime = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - startTime < autoReconnectTimeout)
        {
            Debug.Log("Attempting to reconnect to the server...");

            bool attemptCompleted = false;
            bool attemptSuccess = false;

            StartConnectionAttempt(result =>
            {
                attemptCompleted = true;
                attemptSuccess = result;
            });

            while (!attemptCompleted && Time.realtimeSinceStartup - startTime < autoReconnectTimeout)
            {
                yield return null;
            }

            if (!attemptCompleted)
            {
                break;
            }

            if (attemptSuccess)
            {
                Debug.Log("Auto reconnect succeeded.");
                isReconnecting = false;
                reconnectCoroutine = null;
                yield break;
            }

            if (Time.realtimeSinceStartup - startTime >= autoReconnectTimeout)
            {
                break;
            }

            yield return new WaitForSecondsRealtime(reconnectAttemptInterval);
        }

        Debug.LogWarning($"Failed to reconnect within {autoReconnectTimeout} seconds.");
        isReconnecting = false;
        reconnectCoroutine = null;
    }

    public void ManualReconnect()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("InitSocket is not active. Manual reconnect aborted.");
            return;
        }

        Debug.Log("Manual reconnect requested.");
        StopReconnectRoutine();
        StartConnectionAttempt(null);
    }

    private void SocketClosed()
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Debug.Log("Connection Closed");

            if (!isActiveAndEnabled)
            {
                Debug.Log(1);
                return;
            }
            Debug.Log(2);

            StartReconnectRoutine();
            ISCONECTED = false;
        });
    }

    private void OnDestroy()
    {
        // Правильно отписываемся от сохраненного делегата
        if (socketConnectedHandler != null)
        {
            socketConnected -= socketConnectedHandler;
        }

        StopReconnectRoutine();

        // Также отписываемся от других событий
        if (WebSocketBase.Instance != null)
        {
            WebSocketBase.Instance.OnClose -= SocketClosed;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}