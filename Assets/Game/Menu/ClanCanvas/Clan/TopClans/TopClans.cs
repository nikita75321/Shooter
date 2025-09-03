using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopClans : MonoBehaviour
{
    [Header("My Clan Panel")]
    public TopClanSlot mySlot;
    [SerializeField] private TMP_Text myClanPlaceTXT;
    [SerializeField] private Image myClanIcon;
    [SerializeField] private TMP_Text myClanLevelTXT;
    [SerializeField] private TMP_Text myClanNameTXT;
    [SerializeField] private TMP_Text myClanMembersTXT;
    [SerializeField] private TMP_Text myClanPointsTXT;

    [Header("Top Clans List")]
    [SerializeField] private Transform clansContainer;
    [SerializeField] private GameObject clanSlotPrefab;

    private List<TopClanSlot> topClanSlots = new List<TopClanSlot>();

    private void Awake()
    {
        // Debug.Log("?");
    }
    public void Init()
    {
        WebSocketBase.Instance.OnClanTopWithCurrentReceived += InitAll;
    }

    private void OnDestroy()
    {
        WebSocketBase.Instance.OnClanTopWithCurrentReceived -= InitAll;
    }

    public void InitBestClans()
    {
        WebSocketBase.Instance.GetClanTopWithCurrent(
            clanId: Geekplay.Instance.PlayerData.clanId,
            playerId: Geekplay.Instance.PlayerData.id
        );
    }

    private void InitAll(ClanTopWithCurrentResponse clansInfo)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Debug.Log("InitTop");
            // Debug.Log($"clansInfo - {clansInfo.top_clans.Count}");
            // Инициализация текущего клана игрока
            if (clansInfo.current_clan != null)
            {
                // Debug.Log("??????????????????????????????");
                InitMyClan(clansInfo.current_clan);
            }

            
            // Инициализация списка топ кланов
            InitTopClansList(clansInfo.top_clans);
        });
    }

    private void InitMyClan(ClanShortInfo myClanInfo)
    {
        Debug.Log("InitMyClan");
        Debug.Log("clanLevel - " + myClanInfo.clan_level);
        Debug.Log("clan_points - " + myClanInfo.clan_points);
        mySlot.ClanData = new ClanSearch.ClanData
        {
            clan_id = myClanInfo.clan_id,
            clan_name = myClanInfo.clan_name,
            clan_level = myClanInfo.clan_level,
            clan_points = myClanInfo.clan_points,
            player_count = myClanInfo.player_count,
            max_players = myClanInfo.max_players,
            clan_place = myClanInfo.clan_place,
            leader_name = myClanInfo.leader_name,
            is_open = myClanInfo.is_open,
            need_rating = myClanInfo.need_rating
        };

        mySlot.InitSlot(mySlot.ClanData);
    }

    private void InitTopClansList(List<ClanShortInfo> topClans)
    {
        Debug.Log("InitTopList");
        foreach (Transform child in clansContainer)
        {
            Destroy(child.gameObject);
        }
        topClanSlots.Clear();

        // Сортировка по месту в рейтинге
        var sortedClans = topClans.OrderBy(c => c.clan_place).ToList();

        // Создание новых слотов
        foreach (var clanInfo in sortedClans)
        {
            Debug.Log("Top spawn");
            Debug.Log("clanInfo.clan_level - "+clanInfo.clan_level);
            Debug.Log("clanInfo.max_players - "+clanInfo.max_players);
            Debug.Log("clanInfo.clan_place - "+clanInfo.clan_place);

            var slot = Instantiate(clanSlotPrefab, clansContainer);
            var slotComponent = slot.GetComponent<TopClanSlot>();

            // Преобразование ClanShortInfo в ClanData
            var clanData = new ClanSearch.ClanData
            {
                clan_id = clanInfo.clan_id,
                clan_name = clanInfo.clan_name,
                leader_name = clanInfo.leader_name,
                need_rating = clanInfo.need_rating,
                is_open = clanInfo.is_open,
                clan_points = clanInfo.clan_points,
                clan_level = clanInfo.clan_level,
                player_count = clanInfo.player_count,
                max_players = clanInfo.max_players,
                clan_place = clanInfo.clan_place
            };
            slotComponent.InitSlot(clanData);
            topClanSlots.Add(slotComponent);
        }
    }
}