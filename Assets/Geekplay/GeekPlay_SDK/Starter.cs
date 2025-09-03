using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using CrazyGames;
using UnityEditor;

public enum Platform
{
	[HideInInspector]
    Editor,
    Yandex, 
    VK,
    CrazyGames,
    GameDistribution,
}
	
public class Starter : MonoBehaviour
{
	[Title("Build Platform")]
	[EnumToggleButtons, HideLabel]
	[SerializeField] Platform buildPlatform;

	[Title("Change In Inspector")]
    public bool haveInapp;
    [SerializeField] private bool emulateWeb;
    [SerializeField] private InAppSO[] purchasesList;
    [SerializeField] private RewardSO[] rewards;
    
    [Title("Automatic Variables")]
    public string YanValueType = "YAN"; //тип валюты
    public string language; //язык
    public bool mobile; //Устройство игрока мобильное?

    bool leaderboardLoad, playerLoad, paysLoad, languageLoad;
    PlayerData pD;

    [Title("Analytics")] 
    [SerializeField] private bool needGameAnalytics = true;

    [SerializeField] private GameObject gameAnalyticsObj;
    
    [Title("Extra")]
    [SerializeField] private GameObject interstitialCanvas;
    [SerializeField] private GameObject rewardedCanvas;
    [SerializeField] private Button closeInterstitialButton;
    [SerializeField] private GameObject gdObject;
    
    public void ChangeValute()
    {
        if (!haveInapp)
            return;
        Utils.GetValueCode();
    }
    
    public void ChangeYanType()
    {
        if (!haveInapp)
            return;
        YanValueType = "TST";
        if (Geekplay.Instance != null)
            Geekplay.Instance.YanValueType = YanValueType;
    }
    
    private void Start()
    {
        // mobile = EditorPrefs.GetBool("Geekplay_MobileState");
        
    	// Platform platform = buildPlatform;
    	// #if UNITY_EDITOR
        Platform platform = Platform.Editor;
        // #endif
        // #if !UNITY_EDITOR && UNITY_WEBGL
        //     platform = buildPlatform;
        // #endif

        if (!needGameAnalytics)
        {
            Destroy(gameAnalyticsObj);
        }
        switch (platform)
        {
        	case Platform.Editor:
        		GeekplayEditor editor = gameObject.AddComponent(typeof(GeekplayEditor)) as GeekplayEditor;
        		editor.CreateClass(haveInapp, purchasesList, rewards, YanValueType, language, mobile, pD, Platform.Editor);
                editor.interPanel = interstitialCanvas;
                editor.emulateWeb = emulateWeb;
                editor.closeInterstitialButton = closeInterstitialButton;
                editor.rewardPanel = rewardedCanvas;
        		Destroy(this);
        		break;
        	case Platform.Yandex:
        		StartCoroutine(LoadYandex());
        		break;
        	case Platform.VK:
        		StartCoroutine(LoadVK());
        		break;
            case Platform.CrazyGames:
                StartCoroutine(LoadCrazy());
                break;
            case Platform.GameDistribution:
                StartCoroutine(LoadGD());
                break;
        }
    }
    

    IEnumerator LoadYandex()
    {
    	Platform platform = buildPlatform;
        
        while (!leaderboardLoad || !playerLoad || !paysLoad || !languageLoad)
        { 
            yield return null;
        }
        Geekplay.platform = platform;
        GeekplayYandex yandex = gameObject.AddComponent(typeof(GeekplayYandex)) as GeekplayYandex;
        yandex.CreateClass(haveInapp, purchasesList, rewards, YanValueType, language, mobile, pD, platform);
        Destroy(interstitialCanvas);
        Destroy(gdObject);
        Destroy(this);
    }

    IEnumerator LoadVK()
    {
        Platform platform = buildPlatform;
        while (!playerLoad)
        {
            yield return null;
        }
        Geekplay.platform = platform;
        Geekplay_VK vk = gameObject.AddComponent(typeof(Geekplay_VK)) as Geekplay_VK;
        vk.CreateClass(haveInapp, purchasesList, rewards, YanValueType, "ru", mobile, pD, platform);
        Destroy(gdObject);
        Destroy(interstitialCanvas);
        Destroy(this);
    }

    IEnumerator LoadCrazy()
    {
        yield return null;
        Platform platform = buildPlatform;
        if (CrazySDK.IsAvailable)
        {
            CrazySDK.Init(() =>
            {
                Geekplay.platform = platform;
                Geekplay_Crazy crazy = gameObject.AddComponent(typeof(Geekplay_Crazy)) as Geekplay_Crazy;
                SceneManager.LoadScene(1);
                Destroy(gdObject);
                Destroy(interstitialCanvas);
                Destroy(this);
            });
        }
        else
        {
            Geekplay.platform = platform;
            Geekplay_Crazy crazy = gameObject.AddComponent(typeof(Geekplay_Crazy)) as Geekplay_Crazy;
            SceneManager.LoadScene(1); 
            Destroy(gdObject);
            Destroy(interstitialCanvas);
            Destroy(this);
        }
    }
    
    IEnumerator LoadGD()
    {
        yield return null;
        Platform platform = buildPlatform;
        Geekplay.platform = platform;
        Geekplay_GameDistribution gd = gameObject.AddComponent(typeof(Geekplay_GameDistribution)) as Geekplay_GameDistribution;
        Destroy(interstitialCanvas);
        Destroy(this);
    }

    public void SetPlayerData(string value) //ЗАГРУЗКА
    {
        pD = JsonUtility.FromJson<PlayerData>(value);
        playerLoad = true;
        Debug.Log("LOAD " + JsonUtility.ToJson(pD));
    }

    public void FirstLoadVK()
    {
        pD = new PlayerData();
        playerLoad = true;
    }

    public void LeadLoad()
    {
        leaderboardLoad = true;
    }
    public void PaysLoad()
    {
        paysLoad = true;
    }
    public void LanguageLoad()
    {
        languageLoad = true;
        language = Utils.GetLang();
    }

    public void ItIsMobile()
    {
        mobile = true;
    }

    public void StopMusAndGame()
    {
        AudioListener.volume = 0;
        Time.timeScale = 0;
        GeekplayYandex.Instance.adOpen = true;
    }
    
    public void ResumeMusAndGame()
    {
        AudioListener.volume = 1;
        Time.timeScale = 1;
    }
    
    public void LoadGDCheck()
    {
        StartCoroutine(LGD());
    }

    IEnumerator LGD()
    {
        while (Geekplay_GameDistribution.Instance == null)
            yield return null;
        Geekplay_GameDistribution.Instance.loadGd = true;
    }
}
