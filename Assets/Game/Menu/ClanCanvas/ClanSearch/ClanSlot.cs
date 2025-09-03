using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClanSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public TMP_Text clanNameTXT;
    public TMP_Text overralRatingTXT;
    public TMP_Text membersTXT;
    public TMP_Text typeTXT;
    public TMP_Text requiredRatingTXT;

    [Header("UI Elements")]
    [SerializeField] private Image clanIcon;
    [SerializeField] private TMP_Text clanLevelText;
    [SerializeField] private TMP_Text clanNameText;
    [SerializeField] private TMP_Text clanRatingText;
    [SerializeField] private TMP_Text membersCountText;

    [field:ShowInInspector] public ClanSearch.ClanData ClanData { get; private set; }
    public bool IsClosed { get; private set; }
    public int RequiredRating { get; private set; }

    public void InitSlot(ClanSearch.ClanData clanData)
    {
        // Debug.Log(clanData.clan_level);
        ClanData = clanData;

        clanIcon.sprite = ClanCanvas.Instance.clanSprites[clanData.clan_level];
        clanLevelText.text = clanData.clan_level.ToString();
        clanNameTXT.text = clanData.clan_name;
        overralRatingTXT.text = $"{clanData.clan_points}";
        membersTXT.text = $"{clanData.player_count}/{clanData.max_players}";
        if (!clanData.is_open)
        {
            typeTXT.text = "Тип: закрытый";
        }
        else
        {
            typeTXT.text = "Тип: открытый";
        }
        requiredRatingTXT.text = $"Для вступления: {clanData.need_rating}";
    }

    private int CalculateRequiredRating(int rating)
    {
        return Mathf.Max(0, rating - 200);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ClanCanvas.Instance.clanSearchAndInfo.SelectSlot(this);
    }

    private void OnJoinClicked()
    {
        // ClanManager.Instance.RequestJoinClan(ClanData.clan_id);
    }

    private void OnDestroy()
    {
        // joinButton.onClick.RemoveListener(OnJoinClicked);
    }
}