using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClanMemberSlot : MonoBehaviour
{
    [Header("Stats")]
    public int rating;
    public int clanPoints;

    [Header("UI")]
    [SerializeField] private Image leaderMark;
    [SerializeField] private Image memberIcon;
    [SerializeField] private Image memberPlace;
    [SerializeField] private TMP_Text memberPlaceTXT;
    [SerializeField] private TMP_Text memberNameTXT;
    [SerializeField] private TMP_Text memberLeaderTXT;
    [SerializeField] private TMP_Text rateTXT;
    [SerializeField] private TMP_Text clanPointsTXT;

    private string _memberId;

    private void Start()
    {
        // leaveButton.onClick.AddListener(ClanCanvas.Instance.clan.LeaveClan);
        // Debug.Log("sub");
    }
    private void OnDestroy()
    {
        // leaveButton.onClick.RemoveListener(ClanCanvas.Instance.clan.LeaveClan);
    }

    public void Init(string playerId, string playerName, int rating, int points, bool isLeader = false, int index = 0)
    {
        if (index < 3)
            memberPlace.sprite = ClanCanvas.Instance.placeSprites[index];
        else
            memberPlace.enabled = false;
        memberPlaceTXT.text = (index + 1).ToString();

        // Debug.Log(points +" - points");
        _memberId = playerId;
        memberNameTXT.text = playerName;

        rateTXT.text = rating.ToString();
        this.rating = rating;

        clanPointsTXT.text = points.ToString();
        clanPoints = points;

        if (isLeader)
        {
            // memberNameTXT.text += "(leader)";
            memberLeaderTXT.gameObject.SetActive(true);
            memberLeaderTXT.text = "LEADER";
            leaderMark.gameObject.SetActive(true);
        }
        else
        {
            memberLeaderTXT.gameObject.SetActive(false);
            leaderMark.gameObject.SetActive(false);
        }

        if (playerName != Geekplay.Instance.PlayerData.name)
        {
            memberPlace.gameObject.SetActive(true);
        }
    }
    public void KickMember()
    {
        WebSocketBase.Instance.KickClanMember(Geekplay.Instance.PlayerData.clanId, _memberId);
    }
}
