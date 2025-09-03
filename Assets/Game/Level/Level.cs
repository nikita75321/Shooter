using UnityEngine;

public class Level : MonoBehaviour
{
    public static Level Instance;
    [Header("Referencess")]
    public MainMenu mainMenu;
    public HeroData[] heroDatas;
    [SerializeField] private Player player;
    [SerializeField] private OnlineRoom onlineRoom;
    [SerializeField] private LevelPrefab[] levelPrefabs;
    public LevelPrefab currentLevel;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        
    }
    
    private void Start()
    {
        // GameStateManager.Instance.GameStart.Invoke();
    }

    public void InitLevel()
    {
        currentLevel = Instantiate(levelPrefabs[0], transform);
        player = currentLevel.player;
        onlineRoom.player = player;

        InitCharater();
    }
    
    public void InitCharater()
    {
        player.Init(heroDatas[Geekplay.Instance.PlayerData.currentHero]);
    }

    public void LevelFinish()
    {
        WebSocketBase.Instance.LeaveRoom();
        Destroy(currentLevel.gameObject);
        mainMenu.OpenMenu();
    }
}
