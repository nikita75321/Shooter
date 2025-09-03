using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

public class Geekplay_GameDistribution : MonoBehaviour, IGeekplay
{
    public bool rewardAdLoaded;
    public bool loadGd;
    
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
    public string yanValueType = ""; //тип валюты
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
    
    
    int TimeInGame; //Устройство игрока мобильное?
    public int timeInGame
    {
        get => TimeInGame;
        set => TimeInGame = value;
    }

    public bool mobile; //Устройство игрока мобильное?
    public bool Mobile
    {
        get => mobile;
        set => mobile = value;
    }

    public static Geekplay_GameDistribution Instance { get; private set; }
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
    bool adOpen; //Устройство игрока мобильное?
    public bool AdOpen
    {
        get => adOpen;
        set => adOpen = value;
    }
    [HideInInspector]
    public string purchasedTag; //Тэг покупки
    public string PurchasedTag
    {
        get => purchasedTag;
        set => purchasedTag = value;
    }
    private bool wasLoad; //Игра загружалась?
    private bool canAd;
    private bool canShowAd;
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
    
    public bool NeedCursor;
    public bool needCursor
    {
        get => NeedCursor;
        set => NeedCursor = value;
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
            
            GameDistribution.OnResumeGame += ResumeMusAndGame;
            GameDistribution.OnPauseGame += StopMusAndGame;
            GameDistribution.OnPreloadRewardedVideo += OnPreloadRewardedVideo;
            GameDistribution.OnRewardedVideoFailure += ResumeMusAndGame;
            GameDistribution.OnRewardGame += OnRewarded;
            PreloadRewardedAd();
            canShowAd = true;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadGDCheck()
    {
        loadGd = true;
    }

    IEnumerator Start()
    {
        while (!loadGd)
        {
            yield return null;
        }
        if (PlayerPrefs.HasKey("PlayerData"))
        {
            string jsonString = PlayerPrefs.GetString("PlayerData");
            PlayerData = JsonUtility.FromJson<PlayerData>(jsonString);
        }
        else
        {
            PlayerData = new PlayerData();
        }
        language = Geekplay.Instance.PlayerData.saveLanguage;
        SceneManager.LoadScene(1);
    }

    public void ChangeSound()
    {
        
    }
    
    public void AdOffInapp()
    {

    }

    #region Ad
    
        IEnumerator CanAdShow()
        {
            yield return new WaitForSeconds(60);
            canShowAd = true;
        }
        public void PreloadRewardedAd()
        {
            GameDistribution.Instance.PreloadRewardedAd();
        }
    
        public void OnPreloadRewardedVideo(int loaded)
        {
            if (loaded == 1)
            {
                rewardAdLoaded = true;
            }
            else
            {
                rewardAdLoaded = false;
                PreloadRewardedAd();
            }
        }
        public void ShowInterstitialAd() //МЕЖСТРАНИЧНАЯ РЕКЛАМА - ПОКАЗАТЬ
        {
            if (canShowAd)
            {
                canShowAd = false;
                StartCoroutine(CanAdShow());
                GameDistribution.Instance.ShowAd();   
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

        IEnumerator AdOff() //ТАЙМЕР С ВЫКЛЮЧЕНИЕМ РЕКЛАМЫ
        {
            canAd = false;
            yield return new WaitForSeconds(180);
            canAd = true;
        }

        IEnumerator AdOn() //ТАЙМЕР БЕЗ ВЫКЛЮЧЕНИЯ РЕКЛАМЫ
        {
            yield return new WaitForSeconds(180);
            canAd = true;
        }
    #endregion

    #region ForLeaderboard
        public void SetToLeaderboard(string leaderboardName, float value) //ЗАНЕСТИ В ЛИДЕРБОРД
        {

        }
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
            Debug.Log("SAVE: " + jsonString);
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
            return;
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
        public void StopMusAndGame()
        {
            adOpen = true;
            AudioListener.volume = 0;
            Time.timeScale = 0;
        }

        public void ResumeMusAndGame()
        {
            adOpen = false;
            AudioListener.volume = 1;
            Time.timeScale = 1;
            if (GameIsReady)
            {
                GameStart();
            }
        }

        //ФОКУС И ЗВУК
        void OnApplicationFocus(bool hasFocus)
        {
            Silence(!hasFocus);
        }

        void OnApplicationPause(bool isPaused)
        {
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
            }

            if (GameStoped)
            {
                Time.timeScale = 0;
            }
        }

        public void CloseRateGame()
        {
            AudioListener.volume = 1;
            Time.timeScale = 1;

            if (adOpen)
            {
                Time.timeScale = 0;
                AudioListener.volume = 0;
            }

            if (GameStoped)
            {
                Time.timeScale = 0;
            }
        }

        public void ItIsMobile()
        {
            mobile = true;
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
    #endregion
}
