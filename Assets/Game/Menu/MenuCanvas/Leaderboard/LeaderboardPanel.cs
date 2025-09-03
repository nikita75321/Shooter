using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

[System.Serializable]
public class LeaderboardPlayer
{
    public int place;
    public string name;
    public int value;
}

public class LeaderboardPanel : MonoBehaviour
{
    [Header("Rating grid")]
    [SerializeField] private GameObject ratingContent;
    [SerializeField] private GameObject ratingSlotPrefab;

    [Header("My Slot")]
    [SerializeField] private LeaderboardSlot myRatingPanel;

    [Header("Overral Kills grid")]
    [SerializeField] private GameObject killsContent;
    [SerializeField] private GameObject killsSlotPrefab;

    [Header("My Slot")]
    [SerializeField] private KillsSlot myKillsPanel;

    [Header("Timer To End Month")]
    [SerializeField] private TMP_Text timerTXT;
    
    private DateTime endOfMonth;
    private Sequence timerSequence;

    private void OnEnable()
    {
        WebSocketBase.Instance.RequestRatingLeaderboard();
        WebSocketBase.Instance.RequestKillsLeaderboard();
        
        // Запрашиваем время только если еще не получили
        if (endOfMonth == default)
        {
            WebSocketBase.Instance.GetTimeUntilMonthEnd();
        }
        else
        {
            StartTimer();
        }
    }

    private void Start()
    {
        WebSocketBase.Instance.OnRatingLeaderboardReceived += UpdateRatingLeaderboard;
        WebSocketBase.Instance.OnKillsLeaderboardReceived += UpdateKillsLeaderboard;
        WebSocketBase.Instance.OnTimeUntilMonthEndReceived += OnTimeDataReceived;
    }

    private void OnDisable()
    {
        StopTimer();
    }
    
    private void OnDestroy()
    {
        WebSocketBase.Instance.OnRatingLeaderboardReceived -= UpdateRatingLeaderboard;
        WebSocketBase.Instance.OnKillsLeaderboardReceived -= UpdateKillsLeaderboard;
        WebSocketBase.Instance.OnTimeUntilMonthEndReceived -= OnTimeDataReceived;

        StopTimer();
    }

    private void OnTimeDataReceived(TimeUntilMonthEndResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            // Рассчитываем конечную дату на основе полученных данных
            var now = DateTime.Now;
            endOfMonth = new DateTime(now.Year, now.Month, 1)
                .AddMonths(1)
                .AddDays(-1)
                .AddHours(23)
                .AddMinutes(59)
                .AddSeconds(59);
            
            StartTimer();
        });
    }

    private void StartTimer()
    {
        StopTimer();
        
        timerSequence = DOTween.Sequence()
            .AppendInterval(1f) // Обновляем каждую секунду для большей точности
            .AppendCallback(UpdateTimer)
            .SetLoops(-1)
            .SetLink(gameObject);
    }

    private void StopTimer()
    {
        if (timerSequence != null)
        {
            timerSequence.Kill();
            timerSequence = null;
        }
    }

    private void UpdateTimer()
    {
        if (endOfMonth == default) return;

        var timeLeft = endOfMonth - DateTime.Now;
        
        // Если время вышло, обновляем конечную дату на следующий месяц
        if (timeLeft.TotalSeconds <= 0)
        {
            var now = DateTime.Now;
            endOfMonth = new DateTime(now.Year, now.Month, 1)
                .AddMonths(1)
                .AddDays(-1)
                .AddHours(23)
                .AddMinutes(59)
                .AddSeconds(59);
            
            timeLeft = endOfMonth - DateTime.Now;
        }

        timerTXT.text = $"Конец через: <#30fdff>{timeLeft.Days}д {timeLeft.Hours}ч {timeLeft.Minutes}м";
        // timerTXT.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.5f).SetAutoKill(true);
    }

    private void UpdateRatingLeaderboard(List<LeaderboardPlayer> topPlayers, LeaderboardPlayer myStats)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            foreach (Transform child in ratingContent.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (var player in topPlayers)
            {
                var slot = Instantiate(ratingSlotPrefab, ratingContent.transform);
                var slotScript = slot.GetComponent<LeaderboardSlot>();
                slotScript.Init(player.place.ToString(), player.name, player.value.ToString());
            }

            myRatingPanel.Init(myStats.place.ToString(), myStats.name, myStats.value.ToString());
        });
    }

    private void UpdateKillsLeaderboard(List<LeaderboardPlayer> topPlayers, LeaderboardPlayer myStats)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            foreach (Transform child in killsContent.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (var player in topPlayers)
            {
                var slot = Instantiate(killsSlotPrefab, killsContent.transform);
                var slotScript = slot.GetComponent<KillsSlot>();
                slotScript.Init(player.place.ToString(), player.name, player.value.ToString());
            }

            myKillsPanel.Init(myStats.place.ToString(), myStats.name, myStats.value.ToString());
        });
    }
}