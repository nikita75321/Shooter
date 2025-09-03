using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RatingReward : MonoBehaviour
{
    public static RatingReward Instance;
    [SerializeField] private Slider slider;
    [SerializeField] private int currentRating;
    [SerializeField] private TMP_Text currentRatingTXT;
    [SerializeField] private RatingRewardCard[] ratingRewardCards;
    [SerializeField] private int[] claimedRewards;

    [SerializeField] private int maxRealRating; // Максимальное значение рейтинга (например, 200)
    [SerializeField] private int[] _cachedThresholds;

    private void OnValidate()
    {
        ratingRewardCards ??= GetComponentsInChildren<RatingRewardCard>();
        claimedRewards ??= new int[ratingRewardCards.Length];
        _cachedThresholds = ratingRewardCards.Select(c => c.needRating).ToArray();
    }

    private void Start()
    {
        // _cachedThresholds = ratingRewardCards.Select(c => c.needRating).ToArray();
        claimedRewards = Geekplay.Instance.PlayerData.ratingPathClaimReward;

        InitializeRewards();
        UpdateSlide();
    }

    private void OnEnable()
    {
        UpdateSlide();
    }

    private void InitializeRewards()
    {
        for (int i = 0; i < claimedRewards.Length; i++)
        {
            ratingRewardCards[i].DisableAllRaycastTargets();
            ratingRewardCards[i].id = i;
            ratingRewardCards[i].Init();

            if (claimedRewards[i] == 1)
            {
                Debug.Log("done");
                ratingRewardCards[i].ShowDone();
            }
            else
            {
                Debug.Log("cancel");
            }
        }

        // for (int i = 0; i < ratingRewardCards.Length; i++)
        // {
        //     RatingRewardCard card = ratingRewardCards[i];
        //     card.Init();
        //     ratingRewardCards[i].id = i;
        // }
        // Находим максимальный needRating среди наград
        maxRealRating = 0;
        foreach (var card in ratingRewardCards)
        {
            if (card.needRating > maxRealRating)
                maxRealRating = card.needRating;
        }

        // Настраиваем слайдер (визуально: 0..1)
        slider.minValue = 0;
        slider.maxValue = 1;

        // Распределяем награды равномерно
        for (int i = 0; i < ratingRewardCards.Length; i++)
        {
            float normalizedPos = (float)(i + 1) / (ratingRewardCards.Length + 1);
            SetCardPosition(ratingRewardCards[i], normalizedPos);
        }
    }

    private void SetCardPosition(RatingRewardCard card, float normalizedPos)
    {
        RectTransform cardRect = card.GetComponent<RectTransform>();
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        float sliderWidth = sliderRect.rect.width;
        float xPos = Mathf.Lerp(-sliderWidth / 2f, sliderWidth / 2f, normalizedPos);
        cardRect.anchoredPosition = new Vector2(xPos, cardRect.anchoredPosition.y);
    }

    public void UpdateSlide()
    {
        currentRating = Geekplay.Instance.PlayerData.rate;
        currentRatingTXT.text = $"{currentRating}";

        // Преобразуем реальный рейтинг в визуальный прогресс (0..1)
        float visualProgress = CalculateVisualProgress(currentRating);
        slider.value = visualProgress;

        CheckForRewards();
    }

    private float CalculateVisualProgress(int realRating)
    {
        if (_cachedThresholds.Length == 0 || realRating <= 0)
            return 0f;

        if (realRating >= _cachedThresholds.Last())
            return 1f;

        int prev = 0;
        for (int i = 0; i < _cachedThresholds.Length; i++)
        {
            if (realRating < _cachedThresholds[i])
            {
                float segmentRatio = (float)(realRating - prev) / (_cachedThresholds[i] - prev);
                return (i + segmentRatio) / _cachedThresholds.Length;
            }
            prev = _cachedThresholds[i];
        }
        return 1f;
    }


    private void CheckForRewards()
    {
        for (int i = 0; i < ratingRewardCards.Length; i++)
        {
            if (currentRating >= ratingRewardCards[i].needRating && claimedRewards[i] == 0)
            {
                ratingRewardCards[i].ShowClaim();
                ratingRewardCards[i].EnableAllRaycastTargets();
            }
        }
    }
}