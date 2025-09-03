// Chest.cs
using System.Collections.Generic;
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

        // Заполняем очередь наград
        foreach (var rewardConfig in config.rewards)
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