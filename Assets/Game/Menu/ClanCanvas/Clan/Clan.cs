using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Clan : MonoBehaviour
{
    public string leader;
    public string clanId;
    public string clanName;
    public int level;
    public int rating;
    public int clanPoint;
    public int memberCount;
    public int maxMembers;
    public bool isClosed;
    public int requiredRating;

    public ClanMySlot mySlot;
    public List<ClanMemberSlot> members = new();

    [Header("UI Elements")]
    [SerializeField] private Transform membersContainer;
    [SerializeField] private GameObject memberSlotPrefab;

    [Header("MyClan RightPanel UI")]
    [SerializeField] private GameObject myClanPanel;
    [SerializeField] private Image clanImage;
    [SerializeField] private TMP_Text clanLevelTXT;
    [SerializeField] private TMP_Text clanNameInfoTXT;
    [SerializeField] private TMP_Text clanNameTXT;
    [SerializeField] private TMP_Text overallRatingTXT;
    [SerializeField] private TMP_Text membersInfo;
    [SerializeField] private TMP_Text clanLevelUpTXT;
    [SerializeField] private Slider sliderLevelUp;

    [Header("BestClans RightPanel UI")]
    [SerializeField] private GameObject bestClanPanel;
    [SerializeField] private Image bestClanImage;
    [SerializeField] private TMP_Text bestClanLevelTXT;
    [SerializeField] private TMP_Text bestClanNameTXT;
    [SerializeField] private TMP_Text bestOverallRatingTXT;
    [SerializeField] private TMP_Text bestMembersInfoTXT;

    private void OnEnable()
    {
        WebSocketBase.Instance.OnClanInfoReceived += InitAll;
        WebSocketBase.Instance.OnClanLeft += HandleClanLeft;
    }
    private void OnDisable()
    {
        WebSocketBase.Instance.OnClanInfoReceived -= InitAll;
        WebSocketBase.Instance.OnClanLeft -= HandleClanLeft;
    }

    public void InitClan(string clanId, string clanName)
    {
        WebSocketBase.Instance.GetClanInfo(clanId, clanName);
    }

    public void InitAll(ClanInfo clanInfo)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            // Debug.Log("InitClan");
            // Debug.Log(clanInfo.Stats.Clan_points + " clanInfo.Stats.Clan_points");

            myClanPanel.SetActive(true);
            bestClanPanel.SetActive(false);

            // Заполняем основные данные клана
            clanId = clanInfo.Id.ToString();
            clanName = clanInfo.Name;
            leader = clanInfo.Leader.Name;
            rating = clanInfo.Leader.Rating;
            memberCount = clanInfo.Stats.PlayerCount;
            maxMembers = clanInfo.Stats.MaxPlayers;
            isClosed = !clanInfo.Stats.IsOpen;
            requiredRating = clanInfo.Stats.NeedRating;
            clanPoint = clanInfo.Stats.Clan_points;
            level = clanInfo.Stats.Level;

            // Обновляем UI
            clanImage.sprite = ClanCanvas.Instance.clanSprites[level];
            clanLevelTXT.text = level.ToString();
            clanNameInfoTXT.text = clanName;
            clanNameTXT.text = clanName;
            overallRatingTXT.text = clanInfo.Stats.Clan_points.ToString();
            membersInfo.text = $"{memberCount}/{maxMembers}";

            // Инициализируем наш слот (предполагая, что текущий игрок есть в списке участников)
            var currentPlayer = clanInfo.Members.FirstOrDefault(m =>
                m.Name == Geekplay.Instance.PlayerData.name);

            // Debug.Log($"{currentPlayer.name} {currentPlayer.stats.clan_points} {currentPlayer.stats.rating}");

            if (currentPlayer != null)
            {
                // Debug.Log(1);
                mySlot.Init(
                    currentPlayer.Name,
                    currentPlayer.Stats.Rating,
                    currentPlayer.Stats.ClanPoints,
                    currentPlayer.is_leader
                );
            }
            else
            {
                // Debug.Log(2);
            }

            // Очищаем старых участников
            foreach (Transform child in membersContainer)
            {
                Destroy(child.gameObject);
            }
            members.Clear();
            int index = 0;
            // Создаем слоты для участников в отсортированном порядке
            foreach (var member in clanInfo.Members)
            {
                // Пропускаем текущего игрока (его слот уже обработан)
                // if (member.Name == Geekplay.Instance.PlayerData.name) continue;

                var memberSlot = Instantiate(memberSlotPrefab, membersContainer);
                var memberUI = memberSlot.GetComponent<ClanMemberSlot>();
                memberUI.Init(
                    member.Id,
                    member.Name,
                    member.Stats.Rating,
                    member.Stats.ClanPoints,
                    member.IsLeader,
                    index
                );
                members.Add(memberUI);
                index++;
            }

            AnimateSlider();
            // sliderLevelUp.value = clanPoint;
            
            // Сохраняем название клана у игрока
            Geekplay.Instance.PlayerData.clanName = clanName;
            Geekplay.Instance.Save();

            ClanCanvas.Instance.topClans.InitBestClans();
        });
    }
    //         0: 150,
    //         1: 500,
    //         2: 2000,
    //         3: 5000,
    //         4: 10000,
    //         5: 20000,
    //         6: 35000,
    //         7: 60000,
    //         8: 100000,
    //         9: 150000,
    private void AnimateSlider()
    {
        // Пороговые значения
        int[] caps = { 150, 500, 2000, 5000, 10000, 20000, 35000, 60000, 100000, 150000 };

        int cap = caps[caps.Length - 1];
        foreach (int c in caps)
        {
            if (clanPoint <= c)
            {
                cap = c;
                break;
            }
        }

        sliderLevelUp.maxValue = cap;
        sliderLevelUp.value = 0;

        // Анимация слайдера
        DOVirtual.Float(0, clanPoint, 1f, val =>
        {
            sliderLevelUp.value = val;
            clanLevelUpTXT.text = $"{Mathf.RoundToInt(val)} / {cap}";
        });
    }
    
    public void LeaveClan()
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            if (string.IsNullOrEmpty(Geekplay.Instance.PlayerData.clanName))
            {
                Debug.LogWarning("Player is not in a clan");
                return;
            }

            // Отправляем запрос на сервер
            WebSocketBase.Instance.LeaveClan();

            // Подписываемся на событие успешного выхода
            WebSocketBase.Instance.OnClanLeft += HandleClanLeftResponse;
        });
    }

    private void HandleClanLeftResponse()
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            // Отписываемся от события
            WebSocketBase.Instance.OnClanLeft -= HandleClanLeftResponse;


            // Обновляем данные игрока
            Geekplay.Instance.PlayerData.clanName = null;
            Geekplay.Instance.PlayerData.isClanLeader = false;
            Geekplay.Instance.Save();

            WebSocketBase.Instance.GetAllClans();

            // Обновляем UI
            Debug.Log("Successfully left the clan");
            ClanCanvas.Instance.clanSearch.gameObject.SetActive(true);
            gameObject.SetActive(false);
        });
    }

    private void HandleClanLeft()
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            // Обновляем UI независимо от того, кто инициировал выход
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        });
    }

    public void OpenMemberList()
    {
        myClanPanel.SetActive(true);
        bestClanPanel.SetActive(false);
    }
    public void OpenBestClans()
    {
        if (!bestClanPanel.activeSelf)
        {
            SelectBestClan(ClanCanvas.Instance.topClans.mySlot);
        }
        bestClanPanel.SetActive(true);
        myClanPanel.SetActive(false);
    }

    public void SelectBestClan(TopClanSlot clanSlot)
    {
        var clanData = clanSlot.ClanData;

        bestClanImage.sprite = ClanCanvas.Instance.clanSprites[clanData.clan_level];
        bestClanNameTXT.text = clanData.clan_name;
        bestClanLevelTXT.text = clanData.clan_level.ToString();
        bestOverallRatingTXT.text = clanData.clan_points.ToString();
        bestMembersInfoTXT.text = $"{clanData.player_count}/{clanData.max_players}";
    }
}