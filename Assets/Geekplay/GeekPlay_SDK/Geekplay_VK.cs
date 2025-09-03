using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

public class Geekplay_VK : MonoBehaviour, IGeekplay
{
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
    public string yanValueType = "голосов"; //тип валюты
    public string YanValueType
    {
        get => yanValueType;
        set => yanValueType = value;
    }
    [HideInInspector]
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

    public bool NeedCursor;
    public bool needCursor
    {
        get => NeedCursor;
        set => NeedCursor = value;
    }

    public static Geekplay_VK Instance;
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
    
    int TimeInGame; //Устройство игрока мобильное?
    public int timeInGame
    {
        get => TimeInGame;
        set => TimeInGame = value;
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
        language = lang;
        mobile = mob;
        Platform = pl;
        yanValueType = yan;
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
        timeInGame = 0;
        StartCoroutine(TimerGame());
        if (Platform != Platform.Editor)
        {
            canShowAd = true;
            SceneManager.LoadScene(1);
            StartCoroutine(BannerLoad());
            StartCoroutine(RewardLoad());
            StartCoroutine(InterLoad());
        }
        else
        {
            if (PlayerPrefs.HasKey("PlayerData"))
            {
                string jsonString = PlayerPrefs.GetString("PlayerData");
                PlayerData = JsonUtility.FromJson<PlayerData>(jsonString);
            }
            else
            {
                PlayerData = new PlayerData();
            }
            SceneManager.LoadScene(1);
        }
    }
    
    IEnumerator TimerGame()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            timeInGame += 1;
        }
    }

    #region Ad
        IEnumerator CanAdShow()
        {
            yield return new WaitForSeconds(60);
            canShowAd = true;
        }

        public void ShowBannerAd() 
        {
            switch (Platform)
            {
                case Platform.Editor:
                    #if INIT_DEBUG
                        Debug.Log($"<color={colorDebug}>BANNER SHOW</color>");
                    #endif
                    break;
                case Platform.VK:
                    UtilsVK.VK_Banner();
                    break;
            }
        }

        IEnumerator RewardLoad()
        {
            yield return new WaitForSeconds(15);
            switch (Platform)
            {
                case Platform.Editor:
                    Debug.Log($"<color={colorDebug}>REWARD LOAD</color>");
                    break;
                case Platform.VK:
                    UtilsVK.VK_AdRewardCheck();
                    break;
            }
        }

        IEnumerator InterLoad()
        {
            while (true)
            {   
                yield return new WaitForSeconds(15);
                switch (Platform)
                {
                    case Platform.Editor:
                        Debug.Log($"<color={colorDebug}>INTERSTITIAL LOAD</color>");
                        break;
                    case Platform.VK:
                        UtilsVK.VK_AdInterCheck();
                        break;
                }
            }
        }

        IEnumerator BannerLoad()
        {
            yield return new WaitForSeconds(5);
            ShowBannerAd();
        }

        public void ShowInterstitialAd() //МЕЖСТРАНИЧНАЯ РЕКЛАМА - ПОКАЗАТЬ
        {
            switch (Platform)
            {
                case Platform.Editor:
                    Debug.Log($"<color=blue>INTERSTITIAL SHOW VK</color>");
                    break;
                case Platform.VK:
                    if (canShowAd)
                    {
                        canShowAd = false;
                        StartCoroutine(CanAdShow());
                        UtilsVK.VK_Interstitial();
                    }
                    break;
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

    //пока нерабочий функционал    
    #region ForLeaderboard
        public void SetMyScore(int score)
        {
            Leaderboard.SetScore(score);
        }

        public void SetMyPlace(int place)
        {
            Leaderboard.SetPlace(place);
        }

        public void GetLeadersScore(string value)
        {
            lS[leaderNumber] = value;

            if (leaderNumber < 10)
            {
                Leaderboard.SetScoreInMainBoard(leaderNumber);
                leaderNumber += 1;
                //UtilsVK.GetLeaderboard("score", leaderNumber, "Points");
                Debug.Log(lS[0]);
            }
        }

        public void GetLeadersName(string value)
        {
            lN[leaderNumberN] = value;

            if (leaderNumberN < 10)
            {
                Leaderboard.SetNameInMainboard(leaderNumberN);
                leaderNumberN += 1;
                //UtilsVK.GetLeaderboard("name", leaderNumberN, "Points");
            }
        }
        public void SetToLeaderboard(string leaderboardName, float value) //ЗАНЕСТИ В ЛИДЕРБОРД
        {
            switch (Geekplay.Instance.Platform)
            {
                case Platform.Editor:
                    Debug.Log($"<color=yellow>SET LEADERBOARD:</color> {value}");
                    break;
            }
        }
    #endregion

    #region SaveAndLoad
        public void Save() //СОХРАНЕНИЕ
        {
            string jsonString = "";

            switch (Platform)
            {
                case Platform.Editor:
                    jsonString = JsonUtility.ToJson(PlayerData);
                    PlayerPrefs.SetString("PlayerData", jsonString);
                    Debug.Log("SAVE: " + jsonString);
                    break;
                case Platform.VK:
                    jsonString = JsonUtility.ToJson(PlayerData);
                    UtilsVK.VK_Save(jsonString);
                    Debug.Log("SAVE: " + jsonString);
                    break;
            }
        }

        private void ChangedActiveScene(Scene current, Scene next)
        {
            if (!sceneLoad && next.buildIndex == 1)
            {
                sceneLoad = true;
                StartCoroutine(BannerVK());
                StartCoroutine(RewardLoad());
                StartCoroutine(InterLoad());
            }
        }

        IEnumerator BannerVK()
        {
            yield return new WaitForSeconds(5);
            ShowBannerAd();
        }
    #endregion   

    #region InApp
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
            purchasedTag = idOrTag;
            //нет такого функционала?
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

        public void NotSetPurchasedItem() //не начислить уже купленные предметы на старте
        {
            PlayerData.lastBuy = "";
            Save();
            Debug.Log("NOT");
        }
    #endregion

    #region Social
        public void GoToGroup()
        {
            switch (Platform)
            {
                case Platform.Editor:
                    #if INIT_DEBUG
                        Debug.Log($"<color={colorDebug}>Open Group</color>");
                    #endif
                    break;
                case Platform.VK:
                    rewardTag = "Group";
                    UtilsVK.VK_ToGroup();
                    break;
            }
        }

        public void RewardForGroup()
        {
            //вставьте сюда вознаграждение за подписку на группу
            //выключите кнопку "Вступить в группу"
        }

        public void ToStarGame()
        {
            switch (Platform)
            {
                case Platform.Editor:
                    Debug.Log($"<color={colorDebug}>GAME TO STAR</color>");
                    break;
                case Platform.VK:
                    UtilsVK.VK_Star();
                    break;
            }
        }

        public void ShareGame() //ПОДЕЛИТЬСЯ ИГРОЙ НА СТЕНЕ
        {
            switch (Platform)
            {
                case Platform.Editor:
                    Debug.Log($"<color={colorDebug}>SHARE</color>");
                    break;
                case Platform.VK:
                    UtilsVK.VK_Share();
                    break;
            }
        }

        public void InvitePlayers() //ПРИГЛАСИТЬ ДРУГА
        {
            switch (Platform)
            {
                case Platform.Editor:
                    Debug.Log($"<color={colorDebug}>INVITE</color>");
                    break;
                case Platform.VK:
                    UtilsVK.VK_Invite();
                    break;
            }
        }
    #endregion

    #region Base
    public void StopMusAndGame()
    {/*
        if (!mobile)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true; 
        }
            */
        adOpen = true;
        if (PlayerData.SoundOn)
        {
            AudioListener.volume = 0;
            AudioListener.pause = true;
        }

        Time.timeScale = 0;
    }

    public void ResumeMusAndGame()
    {
        /*
        if (!mobile && !needCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false; 
        }
        */
        
        adOpen = false;
        if (PlayerData.SoundOn)
        {
            AudioListener.volume = 1;
            AudioListener.pause = false;
        }
        Time.timeScale = 1;
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


    #region notNeed
        public void GameReady()
        {

        }
        public void GameStart()
        {

        }
        public void GameStop()
        {

        }
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

        public void HappyTime()
        {

        }
    #endregion
}
