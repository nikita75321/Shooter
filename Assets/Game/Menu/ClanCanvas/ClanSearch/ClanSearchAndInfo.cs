using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClanSearchAndInfo : MonoBehaviour
{
    [Header("Search Panel")]
    public GameObject searchPanel;
    [SerializeField] private TMP_InputField inputFieldSearch;
    [SerializeField] private Button buttonSearch;

    [Header("Clan Info Panel")]
    public GameObject clanInfoPanel;
    [SerializeField] private Image clanIcon;
    [SerializeField] private TMP_Text clanLevelText;
    [SerializeField] private TMP_Text clanNameText;
    [SerializeField] private TMP_Text clanRatingText;
    [SerializeField] private TMP_Text clanNeedRatingText;
    [SerializeField] private TMP_Text membersCountText;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button createButton;

    private ClanSlot selectedClan;

    private void OnEnable()
    {
        WebSocketBase.Instance.OnClanJoined += HandleClanJoined;
        buttonSearch.onClick.AddListener(SearchClans);
        joinButton.onClick.AddListener(JoinSelectedClan);
    }

    private void OnDisable()
    {
        WebSocketBase.Instance.OnClanJoined -= HandleClanJoined;
        buttonSearch.onClick.RemoveListener(SearchClans);
        joinButton.onClick.RemoveListener(JoinSelectedClan);
    }

    public void SelectSlot(ClanSlot clanSlot)
    {
        selectedClan = clanSlot;
        clanInfoPanel.SetActive(true);

        // Обновление информации о клане
        clanIcon.sprite = ClanCanvas.Instance.clanSprites[clanSlot.ClanData.clan_level];
        clanLevelText.text = clanSlot.ClanData.clan_level.ToString();
        clanNameText.text = clanSlot.ClanData.clan_name;
        clanRatingText.text = $"{clanSlot.ClanData.clan_points}";
        clanNeedRatingText.text = $"Для вступления нужно : {clanSlot.ClanData.need_rating}";
        membersCountText.text = $"{clanSlot.ClanData.player_count}/{clanSlot.ClanData.max_players}";
        // requiredRatingText.text = $"Required Rating: {CalculateRequiredRating(clanSlot.ClanData.rating)}";

        // Проверка возможности вступления
        bool canJoin = !IsClanFull(clanSlot.ClanData) && 
                      HasEnoughRating(clanSlot.ClanData) &&
                      !IsAlreadyInClan();

        // Debug.Log(canJoin);
        joinButton.interactable = canJoin;
        // clanStatusText.text = canJoin ? "Status: Can Join" : "Status: Cannot Join";
    }

    private bool IsClanFull(ClanSearch.ClanData clanData)
    {
        // Debug.Log("IsClanFull - " + (clanData.player_count >= 25));
        return clanData.player_count >= 25;
    }
    private bool HasEnoughRating(ClanSearch.ClanData clanData)
    {
        // Debug.Log("HasEnoughRating - " + (Geekplay.Instance.PlayerData.rate >= CalculateRequiredRating(clanData.need_rating)));
        return Geekplay.Instance.PlayerData.rate >= clanData.need_rating;
    }
    private bool IsAlreadyInClan()
    {
        // Debug.Log("IsAlreadyInClan - " + (Geekplay.Instance.PlayerData.clanId > 0));
        return !string.IsNullOrEmpty(Geekplay.Instance.PlayerData.clanId);
    }

    public void SearchClans()
    {
        string searchTerm = inputFieldSearch.text.Trim();
        // loadingIndicator.SetActive(true);

        if (string.IsNullOrEmpty(searchTerm))
        {
            // Debug.Log(1);
            ClanCanvas.Instance.clanSearch.RefreshClanList();
            clanInfoPanel.SetActive(false);
        }
        else
        {
            // Debug.Log(2);
            ClanCanvas.Instance.clanSearch.SearchClans();
        }
    }

    public void JoinSelectedClan()
    {
        if (selectedClan != null)
        {
            WebSocketBase.Instance.JoinClan(selectedClan.ClanData.clan_id);
        }
    }
    
    private void HandleClanJoined(string clanId, string clanName)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            // Обновляем данные игрока
            Geekplay.Instance.PlayerData.clanId = clanId;
            Geekplay.Instance.PlayerData.clanName = clanName;
            Geekplay.Instance.Save();

            // Закрываем панель

            // Debug.Log(gameObject.name);

            ClanCanvas.Instance.clan.gameObject.SetActive(true);
            ClanCanvas.Instance.clanSearch.gameObject.SetActive(false);
            ClanCanvas.Instance.clan.InitClan(clanId, clanName);
            // Debug.Log(clanId);
            // gameObject.SetActive(false);
        });
    }

    public void ShowSearchPanel()
    {
        searchPanel.SetActive(true);
        clanInfoPanel.SetActive(false);
        ClanCanvas.Instance.clanSearch.RefreshClanList();
    }
}