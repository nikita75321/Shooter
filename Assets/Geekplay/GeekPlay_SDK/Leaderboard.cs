using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Leaderboard : MonoBehaviour
{
	[Header("Общий лидерборд")]
	[SerializeField] private TextMeshProUGUI[] textLeaders;
    [SerializeField] private TextMeshProUGUI[] textLeaderScores;

    [Header("Место игрока")]
    public int score; //очки игрока в лидерборде
    public int place; //место игрока в лидерборде
    public TMP_Text textScore;
    public TMP_Text textRank;

    [Header("Text Timre Update")]
    [SerializeField] private float remainingTimeUntilUpdateLeaderboard;
    [SerializeField] private TextMeshProUGUI timerText;
    IEnumerator timer;

    IEnumerator Start()
    {
    	Geekplay.Instance.Leaderboard = this;
    	GetLeaders();
    	yield return new WaitForSeconds(5);
    	StartCoroutine(SetMainLeaderboardCor());
    }

    IEnumerator SetMainLeaderboardCor()
    {
    	while (true)
    	{
            if (timer != null)
                StopCoroutine(timer);
            timer = Timer();
            StartCoroutine(timer);
    		yield return new WaitForSeconds(60);
	    	GetLeaders();
    	}
    }

    IEnumerator Timer()
    {
        remainingTimeUntilUpdateLeaderboard = 60;
        timerText.text = $"До обновления лидерборда {remainingTimeUntilUpdateLeaderboard}";
        while (remainingTimeUntilUpdateLeaderboard > 0)
        {
            yield return new WaitForSeconds(1);
            remainingTimeUntilUpdateLeaderboard -= 1;
            timerText.text = $"До обновления лидерборда {remainingTimeUntilUpdateLeaderboard}";
        }
    }

    public void GetLeaders()//запросить топ игроков
    {
        remainingTimeUntilUpdateLeaderboard = 60;
        Geekplay.Instance.LeaderNumber = 0;
        Geekplay.Instance.LeaderNumberN = 0;
        Utils.GetLeaderboard("score", 0, "Points");
        Utils.GetLeaderboard("name", 0, "Points");
        GetPlayerPlace();
    }

    public void GetPlayerPlace()//запросить место игрока в лидерборде
    {
        Utils.GetMyValueLeaderboard();
    }

    public void SetPlace(int pl)
    {
    	place = pl;
    	textRank.text = place.ToString();
    }

    public void SetScore(int sc)
    {
    	score = sc;
    	textScore.text = score.ToString();
    }

    public void SetNameInMainboard(int i)
    {
        textLeaders[i].text = Geekplay.Instance.LN[i];
    }

    public void SetScoreInMainBoard(int i)
    {
        textLeaderScores[i].text = Geekplay.Instance.LS[i];
    }
}
