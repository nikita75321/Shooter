using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Rendering;

public class ClanSearch : MonoBehaviour
{
    [Header("Referencess")]
    public ClanSearchAndInfo clanSearchAndInfo;

    [Header("UI References")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private ClanSlot clanPrefab;
    [SerializeField] private TMP_InputField searchInput;
    // [SerializeField] private GameObject loadingIndicator;

    private List<ClanSlot> clanSlots = new List<ClanSlot>();

    [System.Serializable]
    public class ClanData
    {
        public string clan_id;
        public string clan_name;
        public int clan_level;
        public string leader_id;
        public string leader_name;
        public int need_rating;
        public bool is_open;
        public int player_count;
        public int max_players;
        public int clan_points;
        public int clan_place;
    }

    private void OnEnable()
    {
        WebSocketBase.Instance.OnClanListReceived += HandleClanList;
    }

    private void OnDisable()
    {
        if (WebSocketBase.Instance != null)
        {
            WebSocketBase.Instance.OnClanListReceived -= HandleClanList;
        }
    }

    private void Start()
    {
        // RefreshClanList();
    }

    public void RefreshClanList()
    {
        // loadingIndicator.SetActive(true);
        ClearClanSlots();
        WebSocketBase.Instance.GetAllClans();
    }

    public void SearchClans()
    {
        string searchTerm = searchInput.text.Trim();
        // loadingIndicator.SetActive(true);
        ClearClanSlots();

        if (string.IsNullOrEmpty(searchTerm))
        {
            WebSocketBase.Instance.GetAllClans();
        }
        else
        {
            WebSocketBase.Instance.SearchClans(searchTerm);
        }
    }

    private void HandleClanList(List<ClanData> clanList)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            ClearClanSlots();

            foreach (var clanData in clanList)
            {
                ClanSlot newSlot = Instantiate(clanPrefab, gridParent);

                // Debug.Log("clanData.max_players - " + clanData.max_players);
                // Проверяем, заполнен ли клан
                bool isFull = clanData.player_count >= clanData.max_players;

                // Проверяем, соответствует ли рейтинг игрока требованиям клана
                bool meetsRatingRequirement = Geekplay.Instance.PlayerData.rate >= clanData.need_rating;

                // Определяем статус доступности
                bool isClosed = isFull || !clanData.is_open || !meetsRatingRequirement;

                newSlot.InitSlot(clanData);

                clanSlots.Add(newSlot);
            }
        });
    }

    private int CalculateRequiredRating(int clanRating)
    {
        return Mathf.Max(0, clanRating - 200);
    }

    private void ClearClanSlots()
    {
        foreach (var slot in clanSlots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }
        clanSlots.Clear();
    }

}