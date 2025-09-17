using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MatchStats
{
    public int kills;
    public int revives;
    public int damageDealt;
    public int shotsFired;
    public int clanPoints;
}

public class GameEndCanvas : MonoBehaviour
{
    [Header("Referencess")]
    [SerializeField] private Player player;
    [SerializeField] private Level level;
    [SerializeField] private RewardSO rewardSO;
    [SerializeField] private GameEndStats gameEndStats;

    [Header("Stats")]
    [SerializeField] private int place;

    [Header("Win panel")]
    [SerializeField] private TMP_Text winPlaceTXT;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Button winClaim;
    [SerializeField] private Button winStats;
    [SerializeField] private Button rewardWin;
    [SerializeField] private float winMoney = 250, winDonatMoney = 1;
    [SerializeField] private int winRating;
    [SerializeField] private TMP_Text winRatingTXT, winMoneyTXT, winDonatMoneyTXT;

    [Header("Lose panel")]
    [SerializeField] private TMP_Text losePlaceTXT;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Button loseClaim;
    [SerializeField] private Button loseStats;
    [SerializeField] private Button rewardLose;
    [SerializeField] private float loseRatingMin = 5, loseRatingMax = 5, loseMoney = 50, loseDonatMoney = 0;
    [SerializeField] private int loseRating;
    [SerializeField] private TMP_Text loseRatingTXT, loseMoneyTXT, loseDonatMoneyTXT;

    [Header("Stats panel")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private Button backButton;

    [Header("General")]
    [SerializeField] private Button back;

    [Header("To Save")]
    [ShowInInspector] public MatchStats matchStats;

    [Header("MatchFinalStats")]
    public MatchEndResponse Response = new();

    private void OnEnable()
    {
        winStats.onClick.AddListener(ShowStatsPanel);
        winClaim.onClick.AddListener(ClaimWin);
        rewardWin.onClick.AddListener(RewardWin);

        loseStats.onClick.AddListener(ShowStatsPanel);
        loseClaim.onClick.AddListener(ClaimLose);
        rewardLose.onClick.AddListener(RewardLose);

        WebSocketBase.Instance.OnMatchEnd += ShowFinal;
    }
    private void OnDisable()
    {
        winStats.onClick.RemoveAllListeners();
        winClaim.onClick.RemoveAllListeners();
        loseStats.onClick.RemoveAllListeners();
        rewardWin.onClick.RemoveAllListeners();
        rewardLose.onClick.RemoveAllListeners();

        WebSocketBase.Instance.OnMatchEnd -= ShowFinal;
    }

    private void Start()
    {
        level = GetComponentInParent<Level>();
        UpdateUI();
    }

    public void OpenMenu()
    {
        level.gameObject.SetActive(false);
        level.mainMenu.OpenMenu();
    }

    private void ShowFinal(MatchEndResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Response = response;

            for (int i = 0; i < response.results.Count; i++)
            {
                var result = response.results[i];
                if (result.player_id == Geekplay.Instance.PlayerData.id)
                {
                    if (result.is_winner)
                    {
                        ShowWinPanel();
                        winPlaceTXT.text = $"Топ - {result.place}";
                    }
                    else
                    {
                        ShowLosePanel();
                        losePlaceTXT.text = $"Ваше место - {result.place}";
                    }
                    place = result.place;
                }
            }

            UpdateUI();
        });
    }

    public void ShowWinPanel()
    {
        var playerData = Geekplay.Instance.PlayerData;
        // player.UpdatePlayerData();

        winPanel.SetActive(true);
        rewardWin.gameObject.SetActive(true);

        GameStateManager.Instance.GamePause();
        Cursor.lockState = CursorLockMode.None;

        playerData.winOverral++;
        playerData.winCaseValue++;

        int clanPoint = playerData.clanPoints;
        if (playerData.clanName != string.Empty)
        {
            if (playerData.isParty)
            {
                clanPoint = player.overallKills * 5 + player.reviveCount * 5 + 1 + 10;
            }
            else
            {
                clanPoint = player.overallKills * 5 + player.reviveCount * 5 + 3;
            }
        }

        matchStats = new MatchStats
        {
            kills = player.overallKills,
            revives = player.reviveCount,
            damageDealt = player.maxDamage,
            shotsFired = player.shotCount,
            clanPoints = clanPoint
        };

        playerData.clanPoints += clanPoint;
        playerData.totalShots += player.shotCount;
        if (playerData.maxDamageBattle < player.maxDamage)
            playerData.maxDamageBattle = player.maxDamage;
        playerData.reviveAlly += player.reviveCount;

        playerData.clanPoints += clanPoint;
        Geekplay.Instance.Save();
    }
    public void HideWinPanel()
    {
        winPanel.SetActive(false);
    }

    public void ShowLosePanel()
    {
        var playerData = Geekplay.Instance.PlayerData;
        // player.UpdatePlayerData();

        losePanel.SetActive(true);
        rewardLose.gameObject.SetActive(true);
        
        GameStateManager.Instance.GamePause();
        Cursor.lockState = CursorLockMode.None;

        playerData.loseOverral++;

        int clanPoint = playerData.clanPoints; 
        if (playerData.clanName != string.Empty)
        {
            // Debug.Log("111111");
            if (playerData.isParty)
            {
                clanPoint = player.overallKills * 5 + player.reviveCount * 5 + 1 + 2;
            }
            else
            {
                clanPoint = player.overallKills * 5 + player.reviveCount * 5 + 1;
            }
        }

        matchStats = new MatchStats
        {
            kills = player.overallKills,
            revives = player.reviveCount,
            damageDealt = player.maxDamage,
            shotsFired = player.shotCount,
            clanPoints = clanPoint
        };

        playerData.clanPoints += clanPoint;
        playerData.totalShots += player.shotCount;
        if (playerData.maxDamageBattle < player.maxDamage)
            playerData.maxDamageBattle = player.maxDamage;
        playerData.reviveAlly += player.reviveCount;

        Geekplay.Instance.Save();
    }
    public void HideLosePanel()
    {
        losePanel.SetActive(false);
    }

    public void ShowStatsPanel()
    {
        statsPanel.SetActive(true);
        gameEndStats.InitStats(Response);
    }
    public void HideStatsPanel()
    {
        statsPanel.SetActive(false);
    }

    private void ClaimWin()
    {
        // Локальное обновление
        switch (place)
        {
            case 1: winRating = (int)Random.Range(10, 15f); break;
            case 2: winRating = (int)Random.Range(7, 12f); break;
            case 3: winRating = (int)Random.Range(4, 7f); break;
        }

        Rating.Instance.AddRating(winRating);
        Currency.Instance.AddMoney((int)winMoney);
        Currency.Instance.AddDonatMoney((int)winDonatMoney);

        // Обновление на сервере
        WebSocketBase.Instance.UpdatePlayerStatsAfterBattle(
            playerId: Geekplay.Instance.PlayerData.id,
            ratingChange: winRating,
            moneyEarned: (int)winMoney,
            donatMoneyEarned: (int)winDonatMoney,
            kills: matchStats.kills,
            isWin: true,
            revives: matchStats.revives,
            damageDealt: matchStats.damageDealt,
            shotsFired: matchStats.shotsFired,
            favoriteHero: MainMenu.Instance.GetHeroNameById(Geekplay.Instance.PlayerData.favoriteHero),
            clanPointsChange: Geekplay.Instance.PlayerData.clanPoints
        );

        level.LevelFinish();
        InstanceSoundUI.Instance.PlayMenuBack();
    }

    private void ClaimLose()
    {
        // Локальное обновление
        loseRating = (int)Random.Range(loseRatingMin, loseRatingMax);

        Rating.Instance.SpendRating(loseRating);
        Currency.Instance.AddMoney((int)loseMoney);

        // Обновление на сервере
        WebSocketBase.Instance.UpdatePlayerStatsAfterBattle(
            playerId: Geekplay.Instance.PlayerData.id,
            ratingChange: -loseRating,
            moneyEarned: (int)loseMoney,
            donatMoneyEarned: (int)loseDonatMoney,
            kills: matchStats.kills,
            isWin: false,
            revives: matchStats.revives,
            damageDealt: matchStats.damageDealt,
            shotsFired: matchStats.shotsFired,
            favoriteHero: MainMenu.Instance.GetHeroNameById(Geekplay.Instance.PlayerData.favoriteHero),
            clanPointsChange: Geekplay.Instance.PlayerData.clanPoints
        );

        level.LevelFinish();
        InstanceSoundUI.Instance.PlayMenuBack();
    }

    private void RewardLose()
    {
        rewardSO.Subscribe(ClaimLose4);
        rewardSO.ShowRewardedAd();
    }
    private void RewardWin()
    {
        rewardSO.Subscribe(ClaimWin4);
        rewardSO.ShowRewardedAd();
    }

    private void ClaimWin4()
    {
        // winRating *= 4;
        winMoney *= 4;
        winDonatMoney *= 4;
        rewardWin.interactable = false;
        UpdateUI();
        rewardWin.gameObject.SetActive(false);
    }
    private void ClaimLose4()
    {
        loseMoney *= 4;
        rewardLose.interactable = false;
        UpdateUI();
        rewardLose.gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        winMoneyTXT.text = winMoney.ToString();
        winRatingTXT.text = $"+{winRating}";
        winDonatMoneyTXT.text = winDonatMoney.ToString();

        loseMoneyTXT.text = loseMoney.ToString();
        loseRatingTXT.text = $"-{loseRating}";
        loseDonatMoneyTXT.text = loseDonatMoney.ToString();
        rewardWin.gameObject.SetActive(false);
    }
}