using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using TMPro;


public class PlaytimeReward : MonoBehaviour
{
	public static PlaytimeReward Instance;
	[InfoBox("Заполните время в SO.\nПри необходимости создайте еще SO наград")]
	[TabGroup("Настройка")]
	[SerializeField] private Playtime_SO[] playtime;
	[TabGroup("Настройка")]
	[SerializeField] private int timeInGame;
	int lastOpened = 0;
	int m;
	int s;
	string mS;
 	string sS;

 	int[] mR = new int[5];
	int[] sR = new int[5];
	string[] mSR = new string[5];
 	string[] sSR= new string[5];
	[ReadOnly]
	[TabGroup("UI")]
	[SerializeField] private TextMeshProUGUI playedText; 
	[ReadOnly]
	[TabGroup("UI")]
	[SerializeField] private TextMeshProUGUI[] timerText; 
	[ReadOnly]
	[TabGroup("UI")]
	[SerializeField] private Image[] icons;
	[ReadOnly]
	[TabGroup("UI")]
	[SerializeField] private Button[] btns;
	[ReadOnly]
	[TabGroup("UI")]
	[SerializeField] private GameObject[] showBtns; 
	[ReadOnly]
	[TabGroup("UI")]
	[SerializeField] private TextMeshProUGUI[] showTxts; 
	[ReadOnly]
	[TabGroup("UI")]
	[SerializeField] private GameObject[] galki; 

	void OnEnable()
	{
		//playtime[0].Subscribe(YourRewardFuction);
		//playtime[1].Subscribe(YourRewardFuction);
		//playtime[2].Subscribe(YourRewardFuction);
		//playtime[3].Subscribe(YourRewardFuction);
		//playtime[4].Subscribe(YourRewardFuction);
	}

	void OnDisable()
	{
		//playtime[0].Unsubscribe(YourRewardFuction);
		//playtime[1].Unsubscribe(YourRewardFuction);
		//playtime[2].Unsubscribe(YourRewardFuction);
		//playtime[3].Unsubscribe(YourRewardFuction);
		//playtime[4].Unsubscribe(YourRewardFuction);
	}

	void Start()
	{
		if (Instance == null)
		{
			DontDestroyOnLoad(gameObject);
			Instance = this;
		}
		else
		{
			Destroy(this.gameObject);
		}

		StartCoroutine(GameTimer());

		for (int i = 0; i < playtime.Length; i++)
		{
			icons[i].sprite = playtime[i].icon;
			SecToMinRewards(i);
		}
	}

	void SecToMin()
	{
		m = timeInGame/60;
		s = timeInGame - m*60;
		if (m < 10)
			mS = "0"+m.ToString();
		else
			mS = m.ToString();
		if (s < 10)
			sS = "0"+s.ToString();
		else
			sS = s.ToString();	
	}

	void SecToMinRewards(int index)
	{
		int t = playtime[index].needTime - timeInGame;
		mR[index] = t/60;
		sR[index] = t - mR[index]*60;
		if (mR[index] < 10)
			mSR[index] = "0"+mR[index].ToString();
		else
			mSR[index] = mR[index].ToString();
		if (sR[index] < 10)
			sSR[index] = "0"+sR[index].ToString();
		else
			sSR[index] = sR[index].ToString();	
		timerText[index].text = mSR[index] + ":" + sSR[index];
	}

	IEnumerator GameTimer()
	{
		while (true)
		{
			yield return new WaitForSeconds(1);
			timeInGame += 1;
			SecToMin();
			for (int i = 0; i < playtime.Length; i++)
			{
				SecToMinRewards(i);
			}
			playedText.text = $"Ты уже сыграл - {mS}:{sS}";
			if (timeInGame >= playtime[lastOpened].needTime)
			{
				showBtns[lastOpened].SetActive(true);
				btns[lastOpened].interactable = true;
				lastOpened += 1;
			}
		}
	}

	public void ClaimBtn(int index)
	{
		playtime[index].TakeReward();
		showTxts[index].text = "Получена";
		galki[index].SetActive(true);
		btns[index].interactable = false;
	}
}
