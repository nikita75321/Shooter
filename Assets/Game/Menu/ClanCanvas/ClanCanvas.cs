using UnityEngine;

public class ClanCanvas : MonoBehaviour
{
    public static ClanCanvas Instance;

    [Header("Main Components")]
    public ClanSearch clanSearch;
    public ClanCreate clanCreate;
    public Clan clan;
    public ClanSearchAndInfo clanSearchAndInfo;
    public TopClans topClans;

    [Header("Clan Icons")]
    public Sprite[] clanSprites = new Sprite[10];

    [Header("Clan Member PlaceIcons")]
    public Sprite[] placeSprites = new Sprite[3];

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeAllComponents();
    }

    private void InitializeAllComponents()
    {
        clanSearch.gameObject.SetActive(false);
        clanCreate.gameObject.SetActive(false);
        clan.gameObject.SetActive(false);

        topClans.gameObject.SetActive(false);
        topClans.Init();
    }

    public void OpenClanCanvas()
    {
        gameObject.SetActive(true);
        
        // Debug.Log(0);
        if (HasClan())
        {
            // Debug.Log(1);
            ShowClanPanel();
        }
        else
        {
            // Debug.Log(2);
            ShowSearchPanel();
        }
    }

    public void CloseClanCanvas()
    {
        InitializeAllComponents();
        gameObject.SetActive(false);
    }

    public void ShowSearchPanel()
    {
        ResetAllPanels();
        clanSearch.gameObject.SetActive(true);
        clanSearch.RefreshClanList();
    }

    public void ShowCreationPanel()
    {
        ResetAllPanels();
        clanCreate.gameObject.SetActive(true);
    }

    public void ShowClanPanel()
    {
        // Debug.Log("00");
        ResetAllPanels();
        clan.gameObject.SetActive(true);
        clan.InitClan(Geekplay.Instance.PlayerData.clanId, Geekplay.Instance.PlayerData.clanName);
    }

    private bool HasClan()
    {
        return !string.IsNullOrEmpty(Geekplay.Instance.PlayerData.clanName) ||
               !string.IsNullOrEmpty(Geekplay.Instance.PlayerData.clanId);
    }

    private void ResetAllPanels()
    {
        clanSearch.gameObject.SetActive(false);
        clanCreate.gameObject.SetActive(false);
        clan.gameObject.SetActive(false);
    }

    // Callback для успешного вступления в клан
    public void OnClanJoined(string clanId, string clanName)
    {
        Debug.Log("OnClanJoined");
        Geekplay.Instance.PlayerData.clanId = clanId;
        Geekplay.Instance.PlayerData.clanName = clanName;
        Geekplay.Instance.Save();
        
        // ShowLoading(false);
        ShowClanPanel();
    }
}