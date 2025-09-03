using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FreeChest : MonoBehaviour
{

    [SerializeField] private RewardSO rewardSO;
    [SerializeField] private ChestConfigSO chest;

    [SerializeField] private GameObject menuCanvas, chestCanvas;

    [SerializeField] private Button freeChestButton;

    [Header("Timer CD")]
    [SerializeField] private Image lockPanel;
    [SerializeField] private Image lockChestPanel;
    [SerializeField] private TMP_Text timerTXT;
    [SerializeField] private float totalTime = 90;
    [SerializeField] private float tempTime = 90;
    [SerializeField] private bool isRunning;

    [SerializeField]private float remainingTime;
    [SerializeField]private DateTime? pauseTime;

    private void Start()
    {
        rewardSO.Subscribe(GainReward);
        timerTXT.gameObject.SetActive(false);
        lockPanel.gameObject.SetActive(false);
        lockChestPanel.gameObject.SetActive(false);
    }

    private void OnEnable()
	{		
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
		// Запоминаем момент выключения объекта
        pauseTime = DateTime.UtcNow;
	}

    private void Update()
    {
        if (!isRunning) return;


        if (tempTime > 0)
        {
            timerTXT.gameObject.SetActive(true);
            freeChestButton.interactable = false;
            lockPanel.gameObject.SetActive(true);
            lockChestPanel.gameObject.SetActive(true);
            tempTime -= Time.deltaTime;

            int minutes = Mathf.FloorToInt(tempTime / 60);
            int seconds = Mathf.FloorToInt(tempTime % 60);

            timerTXT.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        else
        {
            tempTime = 0;
            timerTXT.gameObject.SetActive(false);
            freeChestButton.interactable = true;
            lockPanel.gameObject.SetActive(false);
            lockChestPanel.gameObject.SetActive(false);
            isRunning = false;
            Debug.Log("Время вышло!");
        }
    }

    public void WatchAd()
    {
        rewardSO.ShowRewardedAd();
        tempTime = totalTime;
    }
    private void GainReward()
    {
        InitChest();
        isRunning = true;
    }

    public void InitChest()
    {
        menuCanvas.SetActive(false);
        chestCanvas.SetActive(true);

        ChestRewardCanvas.Instance.InitChest(chest, true);
    }
}
