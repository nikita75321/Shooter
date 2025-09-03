using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public class GeekplayEditor : MonoBehaviour, IGeekplay
{
    public bool emulateWeb; 
    [HideInInspector]
    public bool leaderboardLoad, playerLoad, paysLoad, languageLoad;
    bool sceneLoad = false;
    [Title("Change In Inspector")]
    public bool haveInapp;
    [SerializeField] private InAppSO[] purchasesList;
    [SerializeField] private RewardSO[] rewards;

    [Title("Change In Code")]
    public bool GameStoped;

    [Title("PlayerData")]
    public PlayerData playerData; //сохраняемые данные
    public PlayerData PlayerData
    {
        get => playerData;
        set => playerData = value;
    }

    [Title("Automatic Variables")] 
    public string yanValueType;//тип валюты
    public string YanValueType
    {
        get => yanValueType;
        set => yanValueType = value;
    }
    public string language; //язык
    public string Language
    {
        get => language;
        set => language = value;
    }

    public bool mobile; //Устройство игрока мобильное?
    public bool Mobile
    {
        get => mobile;
        set => mobile = value;
    }
    
    public static GeekplayEditor Instance { get; private set; }
    [HideInInspector]
    public OurGames ourGames;
    public OurGames OurGames
    {
        get => ourGames;
        set => ourGames = value;
    }

    [HideInInspector]
    public Leaderboard leaderboard;
    public Leaderboard Leaderboard
    {
        get => leaderboard;
        set => leaderboard = value;
    }

    [HideInInspector]
    public Platform platform; //Платформа
    public Platform Platform
    {
        get => platform;
        set => platform = value;
    }
    private string developerNameYandex = "GeeKid%20-%20школа%20программирования";
    private IEnumerator cor;
    [HideInInspector]
    public string rewardTag; //Тэг награды
    public string RewardTag
    {
        get => rewardTag;
        set => rewardTag = value;
    }
    private bool adOpen; //Реклама открыта?
    [HideInInspector]
    public string purchasedTag; //Тэг покупки
    public string PurchasedTag
    {
        get => purchasedTag;
        set => purchasedTag = value;
    }
    private bool wasLoad; //Игра загружалась?
    private bool canAd;
    private bool GameIsReady;
    string colorDebug = "yellow"; //Цвет Дебага
    [HideInInspector]
    public string[] lS = new string[10];
    public string[] LS
    {
        get => lS;
    }
    [HideInInspector]
    public string[] lN = new string[10];
    public string[] LN
    {
        get => lN;
    }
    [HideInInspector]
    public int leaderNumber;
    public int LeaderNumber
    {
        get => leaderNumber;
        set => leaderNumber = value;
    }
    [HideInInspector]
    public int leaderNumberN;
    public int LeaderNumberN
    {
        get => leaderNumberN;
        set => leaderNumberN = value;
    }

    [Title("Actions")] 
    public UnityEvent onInterstitialStart;
    public UnityEvent OnInterstitialStart
    {
        get => onInterstitialStart;
        set => onInterstitialStart = value;
    }
    
    public UnityEvent onInterstitialClose;
    public UnityEvent OnInterstitialClose
    {
        get => onInterstitialClose;
        set => onInterstitialClose = value;
    }
    
    public UnityEvent onRewardAdStart;
    public UnityEvent OnRewardAdStart
    {
        get => onRewardAdStart;
        set => onRewardAdStart = value;
    }
    public UnityEvent onRewardAdClose;
    public UnityEvent OnRewardAdClose
    {
        get => onRewardAdClose;
        set => onRewardAdClose = value;
    }
    public UnityEvent onFocusOff;
    public UnityEvent OnFocusOff
    {
        get => onFocusOff;
        set => onFocusOff = value;
    }
    public UnityEvent onFocusOn;
    public UnityEvent OnFocusOn
    {
        get => onFocusOn;
        set => onFocusOn = value;
    }
    
    [Title("For Inspector")] 
    public GameObject interPanel;
    public GameObject rewardPanel;
    public Button closeInterstitialButton;

    public void CreateClass(bool inapps, InAppSO[] purchases, RewardSO[] rewardsL, string yan, string lang, bool mob, PlayerData pd, Platform pl)
    {
        PlayerData = pd;
        haveInapp = inapps;
        purchasesList = purchases;
        rewards = rewardsL;
        YanValueType = yan;
        language = lang;
        mobile = mob;
        Platform = pl;
    }

    protected void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            SceneManager.activeSceneChanged += ChangedActiveScene;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (PlayerPrefs.HasKey("PlayerData"))
        {
            string jsonString = PlayerPrefs.GetString("PlayerData");
            PlayerData = JsonUtility.FromJson<PlayerData>(jsonString);
        }
        else
        {
            PlayerData = new PlayerData();
            Debug.Log("New PlayerData");
            Debug.Log(Geekplay.Instance.PlayerData);
        }
        closeInterstitialButton.onClick.AddListener(CloseInterBtn);
        SceneManager.LoadScene(1);
    }

    void CloseInterBtn()
    {
        OnInterstitialClose.Invoke();
    }

    #region Ad
        public void ShowInterstitialAd() //МЕЖСТРАНИЧНАЯ РЕКЛАМА - ПОКАЗАТЬ
        {
            onInterstitialStart.Invoke();
            if (emulateWeb)
            {
                interPanel.SetActive(true);
            }
            else
            {
                onInterstitialClose.Invoke();
            }
        }

        public void OnRewarded() //ВОЗНАГРАЖДЕНИЕ ПОСЛЕ ПРОСМОТРА РЕКЛАМЫ
        {
            for (int i = 0; i < rewards.Length; i++)
            {
                if (rewardTag == rewards[i].rewardName)
                {
                    rewards[i].Reward();
                    Save();
                }
            }
        }
    #endregion

    #region ForLeaderboard
        public void SetMyScore(int score)
        {
            
        }

        public void SetMyPlace(int place)
        {

        }

        public void GetLeadersScore(string value)
        {

        }

        public void GetLeadersName(string value)
        {

        }
    #endregion

    #region SaveAndLoad
        public void Save() //СОХРАНЕНИЕ
        {
            string jsonString = "";

            jsonString = JsonUtility.ToJson(PlayerData);
            PlayerPrefs.SetString("PlayerData", jsonString);
        }

        private void ChangedActiveScene(Scene current, Scene next)
        {
            if (!sceneLoad && next.buildIndex == 1)
            {
                sceneLoad = true;
            }
        }
    #endregion   

    #region InApp
        public void ChangeYanType()
        {
            
        }

        public void OnPurchasedItem() //начислить покупку (при удачной оплате)
        {
            PlayerData.lastBuy = "";
            for (int i = 0; i < purchasesList.Length; i++)
            {
                if (purchasedTag == purchasesList[i].rewardName)
                {
                    Analytics.instance.SendEvent("Buy_InApp_"+ purchasesList[i].rewardName);
                    purchasesList[i].Reward();
                    Save();
                }
            }
        }

        public void CheckBuysOnStart(string idOrTag) //проверить покупки на старте
        {
            
        }

        public void SetPurchasedItem() //начислить уже купленные предметы на старте
        {
            for (int i = 0; i < purchasesList.Length; i++)
            {
                if (PlayerData.lastBuy == purchasesList[i].rewardName)
                {
                    if (purchasesList[i] != null)
                    {
                        purchasesList[i].Reward();
                        Analytics.instance.SendEvent("Buy_InApp_"+ purchasesList[i].rewardName);
                        PlayerData.lastBuy = "";
                        Save();
                    }
                }
            }
        }

        public void NotSetPurchasedItem() //начислить уже купленные предметы на старте
        {
            PlayerData.lastBuy = "";
            Save();
        }
    #endregion

    #region YandexStats
        public void GameReady()
        {

        }
        public void GameStart()
        {
            GameIsReady = true;
        }
        public void GameStop()
        {
            GameIsReady = false;
        }
    #endregion

    #region Social
        public void RateGame() //ПРОСЬБА ОЦЕНИТЬ ИГРУ
        {

        }

        public void GamePlayed(int id)
        {

        }

        public void GameNotPlayed(int id)
        {

        }

        public void OpenAllGames(string uri)
        {
            
        }

        public void OpenGame(string uri)
        {
            
        }
    #endregion

    #region Base

        public void EditorReward(IEnumerator cor)
        {
            StartCoroutine(cor);
        }
        public void StopMusAndGame()
        {
            adOpen = true;
            AudioListener.volume = 0;
            AudioListener.pause = true;
            Time.timeScale = 0;
        }

        public void ResumeMusAndGame()
        {
            adOpen = false;
            if (PlayerData.SoundOn)
            {
                AudioListener.volume = 1;
                AudioListener.pause = false;
            }
            Time.timeScale = 1;
            if (GameIsReady)
            {
                GameStart();
            }
        }

        //ФОКУС И ЗВУК
        void OnApplicationFocus(bool hasFocus)
        {
            if (emulateWeb)
                Silence(!hasFocus);
        }

        void OnApplicationPause(bool isPaused)
        {
            if (emulateWeb)
                Silence(isPaused);
        }

        private void Silence(bool silence)
        {
            AudioListener.volume = silence ? 0 : 1;
            Time.timeScale = silence ? 0 : 1;

            if (adOpen)
            {
                Time.timeScale = 0;
                AudioListener.volume = 0;
                AudioListener.pause = true;
            }

            if (PlayerData != null)
            {
                if (!PlayerData.SoundOn)
                {
                    AudioListener.volume = 0;
                    AudioListener.pause = true;
                }   
            }
            
            if (GameStoped)
            {
                Time.timeScale = 0;
            }
            
            if (Time.timeScale == 0)
            {
                onFocusOff.Invoke();
            }
            else
            {
                onFocusOn.Invoke();
            }
        }

        public void ItIsMobile()
        {
            mobile = true;
        }
        
        public void ChangeSound()
        {
            if (PlayerData.SoundOn)
            {
                PlayerData.SoundOn = false;
                AudioListener.volume = 0;
                AudioListener.pause = true;
            }
            else
            {
                PlayerData.SoundOn = true;
                AudioListener.volume = 1;
                AudioListener.pause = false;
            }
        }
    #endregion

    #region NotNeed
        public void GoToGroup()
        {

        }

        public void RewardForGroup()
        {

        }

        public void ToStarGame()
        {

        }

        public void ShareGame() //ПОДЕЛИТЬСЯ ИГРОЙ НА СТЕНЕ
        {

        }

        public void InvitePlayers() //ПРИГЛАСИТЬ ДРУГА
        {

        }

        public void HappyTime()
        {
            
        }
        public void SetToLeaderboard(string leaderboardName, float value) //ЗАНЕСТИ В ЛИДЕРБОРД
        {
            
        }   
    #endregion
}
