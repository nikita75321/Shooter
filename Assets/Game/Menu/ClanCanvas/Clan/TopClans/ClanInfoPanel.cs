using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClanInfoPanel : MonoBehaviour
{
    [Header("Clan Info Panel")]
    public GameObject clanInfoPanel;
    [SerializeField] private Image clanIcon;
    [SerializeField] private TMP_Text clanLevelText;
    [SerializeField] private TMP_Text clanNameText;
    [SerializeField] private TMP_Text clanRatingText;
    [SerializeField] private TMP_Text membersCountText;

    private ClanSlot selectedClan;

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

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
        membersCountText.text = $"{clanSlot.ClanData.player_count}/{clanSlot.ClanData.max_players}";
        // requiredRatingText.text = $"Required Rating: {CalculateRequiredRating(clanSlot.ClanData.rating)}";
    }
}