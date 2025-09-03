using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum RewardType
{
    money,
    donatMoney,
    heroCard
}
public class RatingRewardCard : MonoBehaviour
{
    [Header("Referencess")]
    [SerializeField] private Kycha kycha;

    public int id;
    public int needRating;
    [SerializeField] private TMP_Text needRatingTXT;

    [SerializeField] private Image background, iconReward;
    [SerializeField] private Image doneImage;


    [Space(15)]
    public bool isRewardChest = false;
    [HideIf("isRewardChest")]
    [SerializeField] private RewardConfig rewardConfig;
    // [Space(15)]

    // [Space(15)]
    [ShowIf("isRewardChest")]
    [SerializeField] private ChestConfigSO rewardChestConfig;
    [Space(15)]


    [SerializeField] private TMP_Text rewardCountTXT;

    public Button button;

    private void OnValidate()
    {
        if (kycha == null) kycha = FindAnyObjectByType<Kycha>();

        id = transform.GetSiblingIndex();
        needRatingTXT.text = $"{needRating}";
        needRatingTXT.raycastTarget = false;

        if (isRewardChest)
        {
            // iconReward.sprite = kycha.Uncommon_chest;
            // iconReward.rectTransform.anchoredPosition = new Vector2(0, 15);
            // iconReward.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 158);
            // iconReward.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 149);

            // iconReward.sprite = kycha.Rare_chest;
            // iconReward.rectTransform.anchoredPosition = new Vector2(0, 30);
            // iconReward.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 169);
            // iconReward.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 197);

            // iconReward.sprite = kycha.Epic_chest;
            // iconReward.rectTransform.anchoredPosition = new Vector2(0, 0);
            // iconReward.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 164);
            // iconReward.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 154);

            // iconReward.sprite = kycha.Legendary_chest;
            // iconReward.rectTransform.anchoredPosition = new Vector2(0, 5);
            // iconReward.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 177);
            // iconReward.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 160);

            rewardCountTXT.enabled = false;
            rewardConfig = null;
        }

        if (rewardConfig != null)
        {
            rewardCountTXT.enabled = true;
            rewardCountTXT.text = $"{rewardConfig.amount}";

            if (rewardConfig.rewardPrefab.rewardType == RewardInChestType.Hero)
            {
                rewardCountTXT.text = $"New";
                iconReward.sprite = kycha.heroIcons[rewardConfig.rewardPrefab.id];
                iconReward.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 107);
                iconReward.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 196);
            }
            if (rewardConfig.rewardPrefab.rewardType == RewardInChestType.HeroCard)
            {
                // rewardCountTXT.text = $"New";
                iconReward.sprite = kycha.heroIcons[rewardConfig.rewardPrefab.id];
                if (rewardConfig.rewardPrefab.id == 1) iconReward.rectTransform.anchoredPosition = new Vector2(-10, 15);
                iconReward.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 107);
                iconReward.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 196);
            }
            if (rewardConfig.rewardPrefab.rewardType == RewardInChestType.Money)
            {
                iconReward.sprite = kycha.money;
                iconReward.rectTransform.anchoredPosition = new Vector2(0, 15);
            }
            if (rewardConfig.rewardPrefab.rewardType == RewardInChestType.DonatMoney)
            {
                iconReward.sprite = kycha.donatMoney;
                iconReward.rectTransform.anchoredPosition = new Vector2(0, 15);
            }
        }
    }

    private void Start()
    {
        button.onClick.AddListener(ClaimReward);
    }

    public void ShowDone()
    {
        doneImage.gameObject.SetActive(true);
        button.interactable = false;
    }
    public void ShowClaim()
    {
        button.interactable = true;
    }

    public void Init()
    {
        doneImage.gameObject.SetActive(false);

        if (Geekplay.Instance.PlayerData.rate < needRating)
            button.interactable = false;
    }

    public void ClaimReward()
    {
        Geekplay.Instance.PlayerData.ratingPathClaimReward[id] = 1;
        // if (rewardType == RewardType.money)
        // {
        //     Currency.Instance.AddMoney(rewardCount);
        //     WebSocketBase.Instance.AddCurrency(rewardCount, 0);
        // }
        // if (rewardType == RewardType.donatMoney)
        // {
        //     Currency.Instance.AddDonatMoney(rewardCount);
        //     WebSocketBase.Instance.AddCurrency(0, rewardCount);
        // }
        if (isRewardChest)
        {
            ChestRewardCanvas.Instance.InitChest(rewardChestConfig, true);
        }
        else
        {
            if (rewardConfig.rewardPrefab.rewardType == RewardInChestType.Hero)
            {
                Debug.Log(1);
                if (Geekplay.Instance.PlayerData.openHeroes[rewardConfig.rewardPrefab.id] == 1)
                {
                    Debug.Log(2);
                    ChestRewardCanvas.Instance.InitInstaReward(ChestRewardCanvas.Instance.ratingRewardDefault, true);
                }
                else
                {
                    Debug.Log(3);
                    ChestRewardCanvas.Instance.InitInstaReward(rewardConfig, true);
                }
            }
            else
            {
                Debug.Log(4);
                ChestRewardCanvas.Instance.InitInstaReward(rewardConfig, true);
            }
        }

        // Debug.Log(rewardConfig);
        // Debug.Log(rewardConfig.rewardPrefab.rewardType);
        // Debug.Log(ChestRewardCanvas.Instance);
        DisableAllRaycastTargets();
        ShowDone();
        // RatingReward.Instance.ClaimRatingReward(id);
    }

    public void DisableAllRaycastTargets()
    {
        Debug.Log("DisableAllRaycastTargets");
        // Отключаем у всех UI-изображений
        foreach (var img in GetComponentsInChildren<Image>(true))
        {
            img.raycastTarget = false;
        }

        // Отключаем у всех TMP-текстов
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            tmp.raycastTarget = false;
        }
    }
    public void EnableAllRaycastTargets()
    {
        Debug.Log("DisableAllRaycastTargets");
        // Отключаем у всех UI-изображений
        foreach (var img in GetComponentsInChildren<Image>(true))
        {
            img.raycastTarget = true;
        }

        // Отключаем у всех TMP-текстов
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            tmp.raycastTarget = true;
        }
    }
}
