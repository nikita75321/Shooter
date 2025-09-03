using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TopClanSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private TMP_Text clanNameTXT;
    [SerializeField] private TMP_Text overralClanPointsTXT;
    [SerializeField] private TMP_Text membersTXT;
    [SerializeField] private TMP_Text clanPlaceTXT;

    [Header("UI Elements")]
    [SerializeField] private Image clanPlaceIcon;
    [SerializeField] private TMP_Text clanPlaceText;
    [SerializeField] private Image clanLevelIcon;
    [SerializeField] private TMP_Text clanLevelText;

    [field:ShowInInspector] public ClanSearch.ClanData ClanData { get;  set; }

    public void InitSlot(ClanSearch.ClanData clanData)
    {
        // Debug.Log(clanData +" zxczczcxzxczxc");
        // Debug.Log($"ClanData: {clanData.clan_name}, Place: {clanData.clan_place}, Points: {clanData.clan_points}");
        ClanData = clanData;

        if (clanData.clan_place > 0 && clanData.clan_place <= ClanCanvas.Instance.placeSprites.Length)
            clanPlaceIcon.sprite = ClanCanvas.Instance.placeSprites[clanData.clan_place - 1];
        else
            clanPlaceIcon.enabled = false;

        if (clanData.clan_level >= 0 && clanData.clan_level < ClanCanvas.Instance.clanSprites.Length)
            clanLevelIcon.sprite = ClanCanvas.Instance.clanSprites[clanData.clan_level];

        clanLevelText.text = clanData.clan_level.ToString();

        clanPlaceText.text = clanData.clan_place.ToString();
        clanNameTXT.text = clanData.clan_name;
        overralClanPointsTXT.text = $"{clanData.clan_points}";
        membersTXT.text = $"{clanData.player_count}/{clanData.max_players}";
        clanPlaceTXT.text = clanData.clan_place.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Jmay?");
        ClanCanvas.Instance.clan.SelectBestClan(this);
    }

    private void OnDestroy()
    {
        // joinButton.onClick.RemoveListener(OnJoinClicked);
    }
}
