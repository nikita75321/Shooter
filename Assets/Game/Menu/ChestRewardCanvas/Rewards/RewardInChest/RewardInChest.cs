using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardInChest : MonoBehaviour
{
    public Button button;
    public RewardInChestType type;
    public int rewardValue;
    public int id;
    public int idHero;
    public int idSkin;
    public bool randomHero;
    public bool randomSkin;

    [Header("Animation Settings")]
    [SerializeField] private float scaleStart = 0.3f;
    [SerializeField] private float scaleDuration = 0.5f;
    [SerializeField] private float textFadeDelay = 0.2f;
    [SerializeField] private float textFadeDuration = 0.3f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Header("Temp State")]
    public GameObject tempState;
    public Image itemImage;
    public Image shine;
    public TMP_Text rewardAmountTXT;
    [Header("Reward Info")]
    public Image top;
    public TMP_Text infoTXT;
    public Image bottom;

    [Header("Final State")]
    public GameObject finalState;
    public Image itemFinalImage;
    public TMP_Text rewardFinalAmountTXT;

    private Vector3 originalScale = Vector3.one;
    private Sequence _appearSequence;

    [Header("HeroCard")]
    public Slider heroCardSlider;
    public TMP_Text sliderText;
    private string example = "9,999 <#AFD9E9>/ 9,999";

    [SerializeField] private float animationDuration = 1.5f;
    [SerializeField] private Ease easeType = Ease.OutQuad;
    private Tween _currentTween;
    private int _targetValue;
    private int _currentDisplayedValue;

    private void Awake()
    {
        originalScale = transform.localScale;
        if (infoTXT != null)
            infoTXT.color = new Color(infoTXT.color.r, infoTXT.color.g, infoTXT.color.b, 0);
    }

    private void OnDestroy()
    {
        StopCurrentAnimation();
        // Очищаем все твины при уничтожении объекта
        CleanupTweens();
    }

    private void OnDisable()
    {
        StopCurrentAnimation();
        // Очищаем твины при деактивации объекта
        CleanupTweens();
    }

    private void CleanupTweens()
    {
        if (_appearSequence != null && _appearSequence.IsActive())
        {
            _appearSequence.Kill();
            _appearSequence = null;
        }

        if (itemImage != null) itemImage.DOKill();
        if (rewardAmountTXT != null) rewardAmountTXT.DOKill();
        if (top != null) top.DOKill();
        if (bottom != null) bottom.DOKill();
        if (infoTXT != null) infoTXT.DOKill();
    }

    public void Init(RewardConfig rewardConfig)
    {
        if (rewardConfig == null || rewardConfig.rewardPrefab == null)
        {
            Debug.LogWarning("Reward config or prefab is null");
            return;
        }

        CleanupTweens();

        tempState.SetActive(true);
        finalState.SetActive(false);

        PrepareForAnimation();

        // Сохраняем оригинальный тип награды
        type = rewardConfig.rewardPrefab.rewardType;
        // Debug.Log($"Type before - {type}");

        // Обработка награды типа Hero
        if (type == RewardInChestType.Hero)
        {
            if (Geekplay.Instance.PlayerData.openHeroes[rewardConfig.rewardPrefab.id] == 1)
            {
                // Герой уже есть - выбираем между карточками и скином
                if (rewardConfig.skinReward != null && rewardConfig.cardReward != null)
                {
                    // Есть оба варианта - случайный выбор
                    if (Random.Range(0f, 1f) > 0.5f)
                    {
                        SetRewardVisuals(rewardConfig.skinReward);
                        type = RewardInChestType.RandomSkin; // Меняем тип на скин
                        HandleRandomSkinReward(rewardConfig);
                    }
                    else
                    {
                        SetRewardVisuals(rewardConfig.cardReward);
                        type = RewardInChestType.HeroCard; // Меняем тип на карточки
                    }
                }
                else if (rewardConfig.skinReward != null)
                {
                    // Только скин доступен
                    SetRewardVisuals(rewardConfig.skinReward);
                    type = RewardInChestType.RandomSkin;
                }
                else if (rewardConfig.cardReward != null)
                {
                    // Только карточки доступны
                    SetRewardVisuals(rewardConfig.cardReward);
                    type = RewardInChestType.HeroCard;
                }
                else
                {
                    // Нет альтернатив - оставляем героя
                    SetRewardVisuals(rewardConfig.rewardPrefab);
                }

            }
            else
            {
                idHero = rewardConfig.rewardPrefab.id;
                Debug.Log($"idHero={idHero}");
                SetRewardVisuals(rewardConfig.rewardPrefab);
            }
        }
        else
        {
            // Все остальные типы наград
            SetRewardVisuals(rewardConfig.rewardPrefab);
        }


        // Debug.Log($"Type after - {type}");
        if (type == RewardInChestType.HeroCard)
        {
            rewardValue = rewardConfig.amount;
            // Debug.Log($"InitSliderHeroCard rewardValue={rewardValue}");
            InitSliderHeroCard(rewardValue);
        }
        else
        {
            heroCardSlider.gameObject.SetActive(false);
        }

        // Оригинальные проверки для размеров изображений
        if (type == RewardInChestType.Hero || type == RewardInChestType.HeroCard)
        {
            SetNativeSizeWithMultiplier(2f);
        }

        if (type == RewardInChestType.RandomSkin)
        {
            HandleRandomSkinReward(rewardConfig);
            SetNativeSizeWithMultiplier(2f);
        }

        if (type == RewardInChestType.Money || type == RewardInChestType.DonatMoney)
        {
            // HandleRandomSkinReward(rewardConfig);
            SetNativeSizeWithMultiplier(2f);
        }

        SetRewardText(rewardConfig);
        infoTXT.text = GetTypeText(type);

        PlayAppearAnimation();
    }

    private void SetRewardVisuals(RewardInChestSO prefab)
    {
        if (prefab == null) return;

        // type = prefab.rewardType;
        id = prefab.id;

        if (type == RewardInChestType.RandomSkin)
        {
            if (itemImage != null)
            {
                itemImage.sprite = prefab.randomSkins[prefab.id].sprites[0];
                itemImage.SetNativeSize();
            }

            if (itemFinalImage != null)
            {
                itemFinalImage.sprite = prefab.randomSkins[idHero].sprites[idSkin];
                itemFinalImage.SetNativeSize();
            }
        }
        else
        {
            if (itemImage != null)
            {
                itemImage.sprite = prefab.icon;
                itemImage.SetNativeSize();
            }

            if (itemFinalImage != null)
            {
                itemFinalImage.sprite = prefab.icon;
                itemFinalImage.SetNativeSize();
            }
        }
    }

    private void InitSliderHeroCard(int value)
    {
        Debug.Log("Карточки");
        var maxValue = GetMaxValueHeroCard(id);
        var curHeroCard = Geekplay.Instance.PlayerData.persons[id].heroCard;
        // Debug.Log($"{Geekplay.Instance.PlayerData.persons[idHero].}");

        rewardAmountTXT.gameObject.SetActive(false);
        heroCardSlider.gameObject.SetActive(true);
        // heroCardSlider.transform.DOScale(1, 2.5f);
        heroCardSlider.maxValue = maxValue;
        heroCardSlider.value = 0;

        sliderText.text = $"0/{maxValue}";
        // Запускаем анимацию
        AnimateSlider(curHeroCard, value, maxValue);
    }
    
    private void AnimateSlider(int currentValue, int addValue, int maxValue)
    {
        // Debug.Log($"currentValue={currentValue}, addValue={addValue}, maxValue={maxValue}");
        // Останавливаем предыдущую анимацию, если она есть
        StopCurrentAnimation();

        _targetValue = currentValue + addValue;

        // Устанавливаем начальное значение
        heroCardSlider.value = 0;
        _currentDisplayedValue = 0;

        // Создаем анимацию
        _currentTween = DOTween.To(
                () => _currentDisplayedValue,
                x =>
                {
                    _currentDisplayedValue = x;
                    heroCardSlider.value = x;
                },
                _targetValue,
                animationDuration)
            .SetEase(easeType)
            .OnUpdate(() =>
            {
                // Дополнительная логика при обновлении, если нужно
                OnSliderValueUpdated(_currentDisplayedValue, maxValue);
            })
            .OnComplete(() =>
            {
                OnAnimationComplete();
                _currentTween = null;
            })
            .OnKill(() =>
            {
                _currentTween = null;
            });
    }

    // Метод для принудительной остановки анимации
    public void StopAnimation()
    {
        StopCurrentAnimation();
        
        // Устанавливаем финальное значение сразу
        if (heroCardSlider != null)
        {
            heroCardSlider.value = _targetValue;
            _currentDisplayedValue = _targetValue;
        }
    }
    // Метод для получения текущего анимированного значения
    public int GetCurrentAnimatedValue()
    {
        return _currentDisplayedValue;
    }

    // Метод для проверки, идет ли сейчас анимация
    public bool IsAnimating()
    {
        return _currentTween != null && _currentTween.IsActive() && _currentTween.IsPlaying();
    }

    private void StopCurrentAnimation()
    {
        if (_currentTween != null && _currentTween.IsActive())
        {
            _currentTween.Kill();
        }
        _currentTween = null;
    }
    private void OnSliderValueUpdated(int currentValue, int maxValue)
    {
        // Здесь можно добавить дополнительную логику при обновлении значения
        // Например, обновление текста, визуальных эффектов и т.д.
        sliderText.text = $"{currentValue}/{maxValue}";
        // Debug.Log($"Current slider value: {currentValue}");
    }

    private void OnAnimationComplete()
    {
        // Логика при завершении анимации
        // heroCardSlider.transform.DOScale(1.1f, 0.5f);
        // Debug.Log("Slider animation completed!");
    }
    
    private int GetMaxValueHeroCard(int id)
    {
        return Geekplay.Instance.PlayerData.persons[id].rank switch
        {
            1 => 25,
            2 => 50,
            3 => 75,
            4 => 100,
            5 => 150,
            _ => 0,
        };
    }

    public void SetNativeSizeWithMultiplier(float multiplier)
    {
        if (itemImage != null)
        {
            itemImage.SetNativeSize();
            itemImage.rectTransform.sizeDelta *= multiplier;
        }

        if (itemFinalImage != null)
        {
            itemFinalImage.SetNativeSize();
            itemFinalImage.rectTransform.sizeDelta *= multiplier;
        }
    }

    private void HandleRandomSkinReward(RewardConfig rewardConfig)
    {
        Debug.Log("HandleRandomSkinReward");

        int indexHero = rewardConfig.randomHero ? (int)Random.Range(0, 8f) : rewardConfig.idHero;
        int indexSkin = rewardConfig.randomSkin ? (int)Random.Range(0, 7f) : rewardConfig.idSkin;

        idHero = indexHero;
        idSkin = indexSkin;

        // if (rewardConfig.rewardPrefab.randomSkins != null) //&&
        // rewardConfig.rewardPrefab.randomSkins.Length > indexHero) &&
        // rewardConfig.rewardPrefab.randomSkins[indexHero].sprites.Length > indexSkin)
        if (type == RewardInChestType.RandomSkin)
        {
            Sprite sprite;
            Debug.Log(1);
            if (rewardConfig.skinReward != null)
            {
                sprite = rewardConfig.skinReward.randomSkins[idHero].sprites[idSkin];
            }
            else
            {
                sprite = rewardConfig.rewardPrefab.randomSkins[idHero].sprites[idSkin];
            }
            
            if (itemImage != null) itemImage.sprite = sprite;
            if (itemFinalImage != null) itemFinalImage.sprite = sprite;

            // SetNativeSizeWithMultiplier(1.5f);
        }
        else
        {
            Debug.Log(2);
        }

        if (rewardConfig.rewardPrefab.rewardType == RewardInChestType.HeroCard)
        {
            SetNativeSizeWithMultiplier(0.95f);
            Debug.Log("zxc");
        }

        if (itemFinalImage != null)
            itemFinalImage.rectTransform.sizeDelta *= 0.5f;
    }

    private void SetRewardText(RewardConfig rewardConfig)
    {
        // string newText = "Новый";
        string newTextSkin = "Новый скин";
        string valueText;

        if (type == RewardInChestType.RandomSkin)
        {
            // Debug.Log(1);
            valueText = newTextSkin;
            rewardValue = 1; // Для новых героев/скинов
            // rewardAmountTXT
        }
        else
        {
            // Debug.Log(2);
            if (rewardConfig.inRange)
            {
                rewardValue = type == RewardInChestType.Money
                    ? (int)Random.Range(rewardConfig.minAmount / 50, (float)((rewardConfig.maxAmount / 50) + 1)) * 50
                    : (int)Random.Range(rewardConfig.minAmount, (float)rewardConfig.maxAmount);
            }
            else
            {
                rewardValue = rewardConfig.amount;
            }
            valueText = rewardValue.ToString();
        }

        if (type == RewardInChestType.RandomSkin)
        {
            rewardAmountTXT.text = MainMenu.Instance.GetHeroNameById(idHero);
            rewardFinalAmountTXT.text = MainMenu.Instance.GetHeroNameById(idHero);
        }
        else if (type == RewardInChestType.Hero)
        {
            Debug.Log($"idHero = {idHero}, HeroName - {MainMenu.Instance.GetHeroNameById(idHero)}");
            rewardAmountTXT.text = MainMenu.Instance.GetHeroNameById(idHero);
            rewardFinalAmountTXT.text = MainMenu.Instance.GetHeroNameById(idHero);
        }
        else
        {
            if (rewardAmountTXT != null) rewardAmountTXT.text = valueText;
            if (rewardFinalAmountTXT != null) rewardFinalAmountTXT.text = valueText;
        }
    }

    private void PrepareForAnimation()
    {
        if (infoTXT != null) infoTXT.DOFade(1, 1f);
        
        if (top != null && top.rectTransform != null)
        {
            top.rectTransform.anchoredPosition -= new Vector2(2000, 0);
            top.DOFade(0, 0);
        }
        
        if (bottom != null && bottom.rectTransform != null)
        {
            bottom.rectTransform.anchoredPosition += new Vector2(2000, 0);
            bottom.DOFade(0, 0);
        }

        if (itemImage != null) 
        {
            itemImage.transform.localScale = Vector3.one * scaleStart;
        }

        if (rewardAmountTXT != null)
        {
            rewardAmountTXT.alpha = 1f;
            rewardAmountTXT.transform.localScale = Vector3.zero;
        }
    }

    private void PlayAppearAnimation()
    {
        CleanupTweens();

        _appearSequence = DOTween.Sequence();
        
        if (itemImage != null)
        {
            _appearSequence.Join(
                itemImage.transform.DOScale(originalScale, scaleDuration)
                    .SetEase(scaleEase)
            );
        }

        if (top != null && top.rectTransform != null)
        {
            _appearSequence.Join(
                top.rectTransform.DOAnchorPos(new Vector2(0, 60), 0.3f)
            );
            _appearSequence.Join(top.DOFade(1, 1));
        }

        if (bottom != null && bottom.rectTransform != null)
        {
            _appearSequence.Join(
                bottom.rectTransform.DOAnchorPos(new Vector2(0, -60), 0.3f)
            );
            _appearSequence.Join(bottom.DOFade(1, 1));
        }

        if (rewardAmountTXT != null)
        {
            _appearSequence.Join(
                rewardAmountTXT.transform.DOScale(Vector3.one, 0.2f)
            );
        }
    }

    public void UpdateUI(int count)
    {
        rewardValue = count;

        string text;

        if (type == RewardInChestType.Hero)
        {
            text = MainMenu.Instance.GetHeroNameById(idHero);
        }
        else if (type == RewardInChestType.RandomSkin)
        {
            text = MainMenu.Instance.GetHeroNameById(idHero);
        }
        else
        {
            text = count.ToString();
        }
        // string text = (type == RewardInChestType.Hero || type == RewardInChestType.RandomSkin)
        //     ? MainMenu.Instance.GetHeroNameById(idHero)
        //     : count.ToString();

        if (rewardAmountTXT != null) rewardAmountTXT.text = text;
        if (rewardFinalAmountTXT != null) rewardFinalAmountTXT.text = text;

        if (tempState != null)
        {
            tempState.transform.DOKill();
            tempState.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
        }
        // Debug.Log($"text - {text}, type - {type}");
    }

    private string GetTypeText(RewardInChestType type)
    {
        return type switch
        {
            RewardInChestType.Money => "Обычная валюта",
            RewardInChestType.DonatMoney => "Донат валюта",
            RewardInChestType.HeroCard => "Карточки персонажа",
            RewardInChestType.Hero => "Новый персонаж",
            RewardInChestType.RandomSkin => "Новый скин",
            _ => "",
        };
    }
}