// Chest.cs
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Chest : MonoBehaviour
{
    public Image chestImage;
    public Button openButton;
    public TMP_Text openButtonTXT;

    [ShowInInspector] public Queue<RewardConfig> rewardsInChest = new(1);
    [SerializeField] private ChestConfigSO config;

    public void Initialize(ChestConfigSO config)
    {
        this.config = config;

        chestImage.sprite = config.chestSprite;

        chestImage.SetNativeSize();
        chestImage.rectTransform.sizeDelta = chestImage.rectTransform.sizeDelta * 3f;
        // chestImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, chestImage.rectTransform.anchoredPosition.x * 2);
        // chestImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, chestImage.rectTransform.anchoredPosition.y * 2);

        rewardsInChest.Clear();

        if (config.rewards == null || config.rewards.Count == 0)
        {
            return;
        }

        // Гарантированные награды добавляем сразу
        foreach (var rewardConfig in config.rewards.Where(r => !r.isRandomReward))
        {
            rewardsInChest.Enqueue(rewardConfig);
        }

        // Случайные награды выбираем по весам
        var randomRewards = config.rewards.Where(r => r.isRandomReward).ToList();
        if (randomRewards.Count == 0)
        {
            return;
        }

        int totalWeight = randomRewards.Sum(r => Mathf.Max(0, r.GetWeight()));
        if (totalWeight <= 0)
        {
            // Если веса некорректны, выбираем самый редкий предмет
            var fallback = randomRewards.OrderByDescending(r => r.GetWeight()).FirstOrDefault();
            if (fallback != null)
            {
                rewardsInChest.Enqueue(fallback);
            }
            return;
        }

        List<RewardConfig> selectedRewards = new();
        foreach (var rewardConfig in randomRewards)
        {
            int weight = Mathf.Max(0, rewardConfig.GetWeight());
            if (weight == 0)
            {
                continue;
            }

            int roll = Random.Range(0, totalWeight);
            if (roll < weight)
            {
                selectedRewards.Add(rewardConfig);
            }
        }

        if (selectedRewards.Count == 0)
        {
            var fallback = randomRewards
                .OrderByDescending(r => r.GetWeight())
                .FirstOrDefault();

            if (fallback != null)
            {
                selectedRewards.Add(fallback);
            }
        }

        foreach (var rewardConfig in selectedRewards)
        {
            rewardsInChest.Enqueue(rewardConfig);
        }
    }

    private void OnValidate()
    {
        if (openButton == null) openButton = GetComponent<Button>();
    }

    public virtual void Start()
    {
        openButton.onClick.AddListener(() =>
        {
            openButton.interactable = false;
            OpenChest();
        });
    }

    public virtual void OpenChest()
    {
        ChestRewardCanvas.Instance.OpenCurrentChest();
    }

    private void OnDestroy()
    {
        openButton.onClick.RemoveAllListeners();
    }
}