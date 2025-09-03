using UnityEngine;

public class GameModeService : MonoBehaviour
{
    public static GameModeService Instance { get; private set; }

    [field:SerializeField]public float Mode2TimeLeft { get; private set; }
    [field:SerializeField]public float Mode3TimeLeft { get; private set; }
    [field:SerializeField] public bool IsMode2Available { get; private set; }
    [field:SerializeField] public bool IsMode3Available { get; private set; }

    private float _nextRequestTime;
    private bool _needUpdate;
    public bool regimsIsOpen;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        WebSocketBase.Instance.OnGameModesStatusReceived += HandleResponse;
        RequestUpdate();
    }

    private void OnDestroy()
    {
        WebSocketBase.Instance.OnGameModesStatusReceived -= HandleResponse;
    }

    private void Update()
    {
        // Обновляем таймеры
        if (Mode2TimeLeft > 0) Mode2TimeLeft -= Time.deltaTime;
        // if (Mode3TimeLeft > 0) Mode3TimeLeft -= Time.deltaTime;

        // Запрос только при полном истечении времени
        if (Mode2TimeLeft <= 0)
        // || Mode3TimeLeft <= 0)
        {
            if (regimsIsOpen)
            {
                RequestUpdate();
            }
            else if (Geekplay.Instance.PlayerData.currentMode != 1)
            {
                RequestUpdate();
            }
            // После запроса устанавливаем временное значение чтобы не спамить
            Mode2TimeLeft = 1f;
            Mode3TimeLeft = 1f;
        }
    }

    private void HandleResponse(GameModesStatus status)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            IsMode2Available = status.mode2.available;
            // IsMode3Available = status.mode3.available;

            // Обновляем таймеры только при получении новых данных
            Mode2TimeLeft = status.mode2.timeLeft;
            // Mode3TimeLeft = status.mode3.timeLeft;
            if (Geekplay.Instance.PlayerData.currentMode == 2 && !IsMode2Available)
                // || Geekplay.Instance.PlayerData.currentMode == 2 && !IsMode3Available)
            {
                Debug.Log("default");
                MenuCanvas.Instance.SwitchMode(0);
            }
        });
    }

    public void RequestUpdate()
    {
        WebSocketBase.Instance.GetGameModesStatus();
    }
}