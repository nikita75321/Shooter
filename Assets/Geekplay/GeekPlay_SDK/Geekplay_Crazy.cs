using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using CrazyGames;

public class Geekplay_Crazy : MonoBehaviour, IGeekplay
{
    public bool adBlock;
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
    public string yanValueType; //тип валюты
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


    public static Geekplay_Crazy Instance;
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


    public void CreateClass(bool inapps, InAppSO[] purchases, RewardSO[] rewardsL, string yan, string lang, bool mob, PlayerData pd, Platform pl)
    {
        PlayerData = pd;
        haveInapp = inapps;
        purchasesList = purchases;
        rewards = rewardsL;
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
            if (CrazySDK.Data.HasKey("PlayerData"))
            {
                string jsonString = CrazySDK.Data.GetString("PlayerData");
                PlayerData = JsonUtility.FromJson<PlayerData>(jsonString);
            }
            else
            {
                PlayerData = new PlayerData();
            }
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {

    }

    #region Ad
        public void ShowInterstitialAd() //МЕЖСТРАНИЧНАЯ РЕКЛАМА - ПОКАЗАТЬ
        {
            switch (Platform)
            {
                case Platform.Editor:
                    Debug.Log($"<color={colorDebug}>INTERSTITIAL SHOW</color>");
                    break;
                case Platform.CrazyGames:
                    CrazySDK.Ad.RequestAd(CrazyAdType.Midgame, () => // or CrazyAdType.Rewarded
                    {
                        StopMusAndGame();
                    }, (error) =>
                    {
                        ResumeMusAndGame();
                    }, () =>
                    {
                        ResumeMusAndGame();
                    });
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
                Utils.GetLeaderboard("score", leaderNumber, "Points");
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
                Utils.GetLeaderboard("name", leaderNumberN, "Points");
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
                case Platform.CrazyGames:
                    jsonString = JsonUtility.ToJson(PlayerData);
                    CrazySDK.Data.SetString("PlayerData", jsonString);
                    break;
            }
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
            Utils.CheckBuyItem(idOrTag);
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
            Debug.Log("NOT");
        }
    #endregion

    #region CrazyStats
        public void GameStart()
        {
            GameIsReady = true;
            if (Platform == Platform.CrazyGames)
                CrazySDK.Game.GameplayStart();
        }
        public void GameStop()
        {
            GameIsReady = false;
            if (Platform == Platform.CrazyGames)
                CrazySDK.Game.GameplayStop();
        }

        public void CheckAdBlock()
        {
            var adBlockStatus = CrazySDK.Ad.AdblockStatus;
            if (adBlockStatus == AdblockStatus.Present)
                adBlock = true;
            else
                adBlock = false;
        }
    #endregion

    #region Social
        public void HappyTime()
        {
            CrazySDK.Game.HappyTime();
        }
    #endregion

    #region Base
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

    #region NotNeed
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

        public void GameReady()
        {

        }

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
        public void SetToLeaderboard(string leaderboardName, float value) //ЗАНЕСТИ В ЛИДЕРБОРД
        {

        }   
    #endregion
}
