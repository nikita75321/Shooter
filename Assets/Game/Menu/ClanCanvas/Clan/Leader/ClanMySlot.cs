using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClanMySlot : MonoBehaviour
{
    [SerializeField] private Image leaderIcon;
    [SerializeField] private TMP_Text leaderTXT;
    [SerializeField] private TMP_Text nameTXT;
    [SerializeField] private TMP_Text ratingTXT;
    [SerializeField] private TMP_Text clanRatingTXT;
    [SerializeField] private Button leaveButton;

    private void Start()
    {
        leaveButton.onClick.AddListener(ClanCanvas.Instance.clan.LeaveClan);
        // Debug.Log("sub");
    }
    private void OnDestroy()
    {
        leaveButton.onClick.RemoveListener(ClanCanvas.Instance.clan.LeaveClan);
    }

    public void Init(string myName, int myRating, int clanPoints, bool isLeader)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            // Debug.Log($"Init My Slot myRating - {myRating}, clanPoints - {clanPoints}");
            nameTXT.text = myName;
            ratingTXT.text = myRating.ToString();
            clanRatingTXT.text = clanPoints.ToString();

            if (myName == Geekplay.Instance.PlayerData.name)
            {
                leaveButton.gameObject.SetActive(true);
                // leaderPlace.gameObject.SetActive(false);
            }
            else
            {
                // leaderPlace.gameObject.SetActive(true);
                leaveButton.gameObject.SetActive(false);
            }

            if (isLeader)
            {
                leaderIcon.gameObject.SetActive(true);
                leaderTXT.gameObject.SetActive(true);
                leaderTXT.text = "LEADER";
            }
            else
            {
                leaderIcon.gameObject.SetActive(false);
                leaderTXT.gameObject.SetActive(false);
            }
        });

        // Здесь можно добавить загрузку аватарки
        // StartCoroutine(LoadAvatar(leaderId));
    }
}
