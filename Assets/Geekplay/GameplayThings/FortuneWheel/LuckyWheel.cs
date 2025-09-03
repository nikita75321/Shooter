using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GameAnalyticsSDK.Setup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LuckyWheel : MonoBehaviour
{
	[SerializeField] private RewardSO rewardSO;
	[SerializeField] private Button[] needToOffButtons;

	[Header("Chests")]
	[SerializeField] private ChestRewardCanvas chestRewardCanvas;
	[SerializeField] private ChestConfigSO uncommon;
	[SerializeField] private ChestConfigSO rare;

	[Header("Rewards config")]
	[SerializeField] private RewardConfig[] rewardConfigs;
	[Space(15)]

	[Header("Reward claaim anim")]
	[SerializeField] private Sequence rewardSequence;
	[SerializeField] private GameObject[] focusFrames;
	[SerializeField] private Image[] iconItemInSlot;
	[Space(15)]

	[SerializeField] private Button buttonSpin;
	[SerializeField] private Image wheel;
	[SerializeField] private float rotationSpeed;
	float slowDownTime;
	[SerializeField] private float rotationTimeMaxSpeed;
	[SerializeField] private float accelerationTime;
	[SerializeField] private int numberOfSpins;

	[SerializeField] private List<LuckyPrize> prizes = new();
	[SerializeField] private bool isLerp;

	float maxAngel;
    float minAngel;

	bool isSpin = false;
	int randomSector;

	[Header("Timer CD")]
	[SerializeField] private Button spinButton;
    [SerializeField] private Image adImage;
	[SerializeField] private TMP_Text spinTXT;
    [SerializeField] private TMP_Text timerTXT;
    [SerializeField] private float totalTime = 90;
    [SerializeField] private float tempTime = 90;
    [SerializeField] private bool isRunning;

	[SerializeField]private float remainingTime;
    [SerializeField]private DateTime? pauseTime;

	private void Start()
	{
		chestRewardCanvas = ChestRewardCanvas.Instance;
		timerTXT.gameObject.SetActive(false);
	}

    private void Update()
    {
        if (!isRunning) return;

        
        if (tempTime > 0)
        {
            timerTXT.gameObject.SetActive(true);
            spinButton.interactable = false;
			adImage.gameObject.SetActive(false);
			spinTXT.gameObject.SetActive(false);
			tempTime -= Time.deltaTime;

            int minutes = Mathf.FloorToInt(tempTime / 60);
            int seconds = Mathf.FloorToInt(tempTime % 60);

            timerTXT.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        else
        {
            tempTime = 0;
            timerTXT.gameObject.SetActive(false);
            spinButton.interactable = true;
			adImage.gameObject.SetActive(true);
			spinTXT.gameObject.SetActive(true);
			isRunning = false;
            Debug.Log("Время вышло!");
        }
    }
    private void InitializeRewardSequence()
	{
		rewardSequence = DOTween.Sequence()
			.SetAutoKill(false)
			.Pause();

		// Анимация мигания (3 раза)
		for (int i = 0; i < 5; i++)
		{
			// Debug.Log(1);
			rewardSequence.AppendCallback(() => iconItemInSlot[randomSector].gameObject.SetActive(false));
			rewardSequence.AppendInterval(0.07f);
			rewardSequence.AppendCallback(() => iconItemInSlot[randomSector].gameObject.SetActive(true));
			rewardSequence.AppendInterval(0.07f);
			// Debug.Log(2);
			// rewardSequence.Append(iconItemInSlot[randomSector].DOFade(0, 0.15f));
			// rewardSequence.Append(iconItemInSlot[randomSector].DOFade(1, 0.15f));
		}
		rewardSequence.AppendInterval(0.2f);
		rewardSequence.OnComplete(() =>
		{
			ShowFinalReward();
			GiveReward();
		});

		rewardSequence.AppendInterval(0.2f);
		rewardSequence.AppendCallback(() =>
		{
			buttonSpin.interactable = true;
			foreach (var b in needToOffButtons)
			{
				b.interactable = true;
			}
		});
	}

	private void OnEnable()
	{
		InitializeRewardSequence();

		rewardSO.Subscribe(Spin);
		prizes[0].reward += YourRewardFunction;
		prizes[1].reward += YourRewardFunction;
		prizes[2].reward += YourRewardFunction;
		prizes[3].reward += YourRewardFunction;
		prizes[4].reward += YourRewardFunction;
		prizes[5].reward += YourRewardFunction;
		prizes[6].reward += YourRewardFunction;
		prizes[7].reward += YourRewardFunction;
		
		// Если таймер был на паузе, пересчитываем оставшееся время
        if (pauseTime.HasValue)
        {
            TimeSpan pausedDuration = DateTime.UtcNow - pauseTime.Value;
            tempTime -= (float)pausedDuration.TotalSeconds;
            pauseTime = null;
        }
        else
        {
            remainingTime = tempTime;
        }
	}

	private void OnDisable()
	{
		rewardSO.Unsubscribe(Spin);
		prizes[0].reward -= YourRewardFunction;
		prizes[1].reward -= YourRewardFunction;
		prizes[2].reward -= YourRewardFunction;
		prizes[3].reward -= YourRewardFunction;
		prizes[4].reward -= YourRewardFunction;
		prizes[5].reward -= YourRewardFunction;
		prizes[6].reward -= YourRewardFunction;
		prizes[7].reward -= YourRewardFunction;

		rewardSequence?.Kill();

		// Запоминаем момент выключения объекта
        pauseTime = DateTime.UtcNow;
	}

	private void YourRewardFunction()
    {
        PrepareRewardAnimation();
    }

	private void PrepareRewardAnimation()
    {
        // Сбрасываем все анимации
        foreach (var frame in focusFrames) frame.SetActive(false);
        foreach (var icon in iconItemInSlot)
        {
            icon.gameObject.SetActive(false);
            icon.color = new Color(1, 1, 1, 1); // Сброс прозрачности
        }

        // Включаем только нужные элементы
        focusFrames[randomSector].SetActive(true);
        iconItemInSlot[randomSector].gameObject.SetActive(true);

        // Запускаем анимацию
        rewardSequence.Restart();
    }

	private void ShowFinalReward()
    {
        // Оставляем только выигранный элемент видимым
        for (int i = 0; i < focusFrames.Length; i++)
        {
            focusFrames[i].SetActive(i == randomSector);
            iconItemInSlot[i].gameObject.SetActive(i == randomSector);
            iconItemInSlot[i].color = Color.white;
        }
    }

	private void GiveReward()
	{
		switch (randomSector)
		{
			case 0:
				chestRewardCanvas.InitChest(rare, false);
				break;
			case 1:
				chestRewardCanvas.InitInstaReward(rewardConfigs[0], false);
				chestRewardCanvas.isNeededX2 = false;
				break;
			case 2:
				chestRewardCanvas.InitInstaReward(rewardConfigs[1], false);
				chestRewardCanvas.isNeededX2 = false;
				break;
			case 3:
				chestRewardCanvas.InitInstaReward(rewardConfigs[2], false);
				chestRewardCanvas.isNeededX2 = false;
				break;
			case 4:
				chestRewardCanvas.InitInstaReward(rewardConfigs[3], false);
				chestRewardCanvas.isNeededX2 = false;
				break;
			case 5:
				chestRewardCanvas.InitChest(uncommon, false);
				break;
			case 6:
				chestRewardCanvas.InitInstaReward(rewardConfigs[4], false);
				chestRewardCanvas.isNeededX2 = false;
				break;
			case 7:
				chestRewardCanvas.InitInstaReward(rewardConfigs[5], false);
				chestRewardCanvas.isNeededX2 = false;
				break;
		}

		foreach (var frame in focusFrames) frame.SetActive(false);
		foreach (var icon in iconItemInSlot)
		{
			icon.gameObject.SetActive(false);
			icon.color = new Color(1, 1, 1, 1); // Сброс прозрачности
		}
		Debug.Log($"Вы выиграли: {prizes[randomSector].name}");
		isRunning = true;
		tempTime = totalTime;
    }

    public void SpinBtn()
	{
		rewardSO.ShowRewardedAd();
	}

	public void Spin()
	{
		if (!isSpin)
		{
			buttonSpin.interactable = false;
			foreach (var b in needToOffButtons)
			{
				b.interactable = false;
			}

			StartCoroutine(SpinWheel());
			foreach (var frame in focusFrames) frame.SetActive(false);
			foreach (var icon in iconItemInSlot)
			{
				icon.gameObject.SetActive(false);
				icon.color = new Color(1, 1, 1, 1); // Сброс прозрачности
			}
		}
	}

    IEnumerator SpinWheel()
    {
    	SetWin();
    	isSpin = true;
    	float elapsedTime = 0f;
    	float rotSpeed;

    	while (elapsedTime < accelerationTime)
    	{
    		rotSpeed = Mathf.Lerp(0, rotationSpeed, elapsedTime / accelerationTime);
    		wheel.transform.rotation *= Quaternion.Euler(0,0, rotSpeed * Time.deltaTime);
    		elapsedTime += Time.deltaTime;
    		yield return null;
    	}

    	elapsedTime = 0f;

    	while (elapsedTime < rotationTimeMaxSpeed)
    	{
    		wheel.transform.rotation *= Quaternion.Euler(0,0, rotationSpeed * Time.deltaTime);
    		elapsedTime += Time.deltaTime;
    		yield return null;
    	}

    	float distance = (numberOfSpins * 360) + UnityEngine.Random.Range(minAngel + 5, maxAngel - 5) - wheel.transform.rotation.eulerAngles.z;
    	slowDownTime = (2 * distance) / rotationSpeed;
    	float slowdown = rotationSpeed / slowDownTime;
    	rotSpeed = rotationSpeed;

    	elapsedTime = 0f;

    	while (elapsedTime < slowDownTime)
    	{
    		if (isLerp)
    		{
    			rotSpeed = Mathf.Lerp(rotationSpeed, 0, elapsedTime / slowDownTime);
    		}
    		else
    		{
    			rotSpeed -= slowdown * Time.deltaTime;
    		}
    		wheel.transform.rotation *= Quaternion.Euler(0,0, rotSpeed * Time.deltaTime);
    		elapsedTime += Time.deltaTime;
    		yield return null;
    	}

    	isSpin = false;
    	prizes[randomSector].reward.Invoke();
    }

    void SetWin()
    {
    	randomSector = GetRandomPrize();
    	maxAngel = 360 / prizes.Count * (randomSector + 1);
    	minAngel = 360 / prizes.Count * randomSector;
    }

    int GetRandomPrize()
    {
    	randomSector = UnityEngine.Random.Range(0, prizes.Count);
    	if (prizes[randomSector].weight <= UnityEngine.Random.Range(0, 1))
    	{
    		return GetRandomPrize();
    	}
    	return randomSector;
    }
}
