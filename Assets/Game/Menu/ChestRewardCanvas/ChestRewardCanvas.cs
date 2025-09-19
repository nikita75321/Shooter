// ChestRewardCanvas.cs
// using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// [Serializable]
// public class
public class ChestRewardCanvas : MonoBehaviour, IPointerClickHandler
{
    public static ChestRewardCanvas Instance;

    [Header("State reward")]
    public bool isNeededX2 = true;
    [SerializeField] private bool isInstantReward;
    [SerializeField] private int randomRewardCount;

    [Header("Default Rewards")]
    public RewardConfig ratingRewardDefault;
    public RewardConfig[] rewardsSkin;

    [Header("UI references")]
    [SerializeField] private TMP_Text yourRewardTXT;

    [Header("Back button")]
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject ChestPanel, RewardPanel;

    [SerializeField] private Chest currentChest;
    [ShowInInspector] public Queue<RewardConfig> rewardsInChest = new(1);
    [SerializeField] private GameObject currentReward;
    [SerializeField] private List<RewardInChest> allReward;

    [Header("Chest prefab")]
    [SerializeField] private Chest chestPrefab;
    [SerializeField] private RectTransform chestRect;

    [Header("Multiple Chests Settings")]
    [SerializeField] private Queue<ChestConfigSO> chestsQueue = new Queue<ChestConfigSO>();
    [SerializeField] private bool isProcessingMultipleChests = false;

    [Header("Reward prefab")]
    [SerializeField] private RewardInChest rewardPrefab;

    [Header("Claim all button")]
    [SerializeField] private Button claimAllButton;
    [SerializeField] private Button claimAllButtonAd;

    [Header("Rarity Colors")]
    [SerializeField] private Color common;
    [SerializeField] private Color rare;
    [SerializeField] private Color epic;

    [Header("Chest Animation Settings")]
    [SerializeField] private float chestDropDuration = 0.8f;
    [SerializeField] private float chestDropHeight = 500f;
    [SerializeField] private float chestShakeDuration = 0.5f;
    [SerializeField] private float chestShakeStrength = 10f;
    [SerializeField] private int chestShakeVibrato = 10;
    [SerializeField] private float chestOpenScale = 1.5f;
    [SerializeField] private float chestOpenDuration = 1f;

    [Header("Reward Animation Settings")]
    [SerializeField] private float rewardAppearDuration = 0.7f;
    [SerializeField] private float overlapPercentage = 0.5f; // 50% перекрытие анимаций

    [Header("Reward Gain Animation Settings")]
    [SerializeField] private float rewardGainScale = 1.5f;
    [SerializeField] private float rewardGainDuration = 1f;

    [Header("Idle Tilt Animation Settings")]
    [SerializeField] private float idleTiltAngle = 5f; // Максимальный угол наклона
    [SerializeField] private float idleJumpHeight = 10f; // Высота подпрыгивания
    [SerializeField] private float idleTiltDuration = 0.5f; // Длительность наклона
    [SerializeField] private float idleJumpDuration = 0.3f; // Длительность подпрыгивания
    [SerializeField] private float idleShakeRotationStrength = 3f; // Сила дрожания (в градусах)
    [SerializeField] private int idleShakeRotationVibrato = 5; // Интенсивность дрожания

    private bool isAnimating = false;
    private List<Tween> activeRewardTweens = new();
    private Sequence rewardsSequence;

    // [Header("Sounds")]
    // [SerializeField] private AudioSource audioSource;
    // [SerializeField] private AudioClip 

    private void Awake()
    {
        Instance = this;
    }

    public void InitChest(ChestConfigSO chestConfig, bool needX2)
    {
        isInstantReward = false;

        isNeededX2 = needX2;

        gameObject.SetActive(true);
        yourRewardTXT.gameObject.SetActive(true);
        yourRewardTXT.color = new Color(yourRewardTXT.color.r, yourRewardTXT.color.g, yourRewardTXT.color.b, 0);

        allReward.Clear();

        claimAllButtonAd.interactable = true;
        claimAllButtonAd.gameObject.SetActive(false);

        backButton.SetActive(false);
        claimAllButton.gameObject.SetActive(false);

        // Очищаем предыдущий сундук, если есть
        if (currentChest != null)
        {
            Destroy(currentChest.gameObject);
        }

        // Создаем новый сундук
        var chestObj = Instantiate(chestConfig.chestPrefab, ChestPanel.transform);
        currentChest = chestObj.GetComponent<Chest>();
        currentChest.Initialize(chestConfig);

        currentChest.openButton.interactable = false;
        currentChest.openButton.gameObject.SetActive(true);

        // Начальная позиция за кадром сверху
        chestRect = currentChest.chestImage.GetComponent<RectTransform>();
        chestRect.anchoredPosition = new Vector2(0, chestDropHeight);
        currentChest.gameObject.SetActive(true);

        // Анимация падения сундука
        Sequence dropSequence = DOTween.Sequence();
        dropSequence.Append(currentChest.openButtonTXT.DOFade(0, 0f));

        // Падение с эффектом "пружины"
        dropSequence.Append(chestRect.DOAnchorPosY(0, chestDropDuration)
            .SetEase(Ease.OutBounce));

        // Анимация покачивания после падения
        dropSequence.Append(chestRect.DOShakeRotation(chestShakeDuration, chestShakeStrength, chestShakeVibrato, 90, false)
            .SetEase(Ease.OutQuad));

        dropSequence.Append(currentChest.openButtonTXT.DOFade(1, 0.3f));

        // Периодическая анимация "подпрыгивания и наклона" (пока сундук не открыт)
        dropSequence.AppendCallback(() =>
        {
            StartIdleTiltAnimation(chestRect);
            currentChest.openButton.interactable = true;
        });

        rewardsInChest = currentChest.rewardsInChest;
        Debug.Log("rewardsInChest.Count - " + rewardsInChest.Count);
    }

    public void InitMultipleChests(ChestConfigSO[] chestConfigs, bool needX2)
    {
        if (chestConfigs == null || chestConfigs.Length == 0) return;

        // Очищаем очередь и добавляем новые сундуки
        chestsQueue.Clear();
        foreach (var config in chestConfigs)
        {
            chestsQueue.Enqueue(config);
        }

        isNeededX2 = needX2;
        isProcessingMultipleChests = true;

        // Начинаем обработку первого сундука
        ProcessNextChest();
    }

    private void ProcessNextChest()
    {
        if (chestsQueue.Count == 0)
        {
            // Все сундуки обработаны
            isProcessingMultipleChests = false;
            MainMenu.Instance.OpenMenu();
            gameObject.SetActive(false);
            return;
        }

        var nextChest = chestsQueue.Dequeue();
        InitChest(nextChest, isNeededX2);

        // Делаем кнопку "Забрать всё" неактивной до открытия сундука
        claimAllButton.gameObject.SetActive(false);
        claimAllButtonAd.gameObject.SetActive(false);
    }

    public void InitInstaReward(RewardConfig rewardCard, bool needX2)
    {
        currentChest = null;

        isNeededX2 = needX2;
        isInstantReward = true;
        gameObject.SetActive(true);
        // Скрываем все элементы, связанные с сундуком
        yourRewardTXT.gameObject.SetActive(false);
        yourRewardTXT.color = new Color(yourRewardTXT.color.r, yourRewardTXT.color.g, yourRewardTXT.color.b, 0);

        backButton.SetActive(false);
        claimAllButton.gameObject.SetActive(false);
        claimAllButtonAd.gameObject.SetActive(false);

        // Очищаем предыдущие награды, если есть
        allReward.Clear();

        // Показываем награду сразу
        ShowReward(rewardCard);

        // Устанавливаем флаг, что это мгновенная награда
        rewardsInChest = new Queue<RewardConfig>(0); // Пустая очередь

        // Показываем текст "Ваша награда"
        yourRewardTXT.DOFade(1, 0.3f);
    }

    public void InitRewardsArray(RewardConfig[] rewardsArray, bool needX2)
    {
        // Очищаем предыдущие данные
        currentChest = null;
        allReward.Clear();
        rewardsInChest = new Queue<RewardConfig>(rewardsArray.Length);

        // Устанавливаем настройки X2
        isNeededX2 = needX2;
        isInstantReward = false;
        // Настраиваем UI
        gameObject.SetActive(true);
        yourRewardTXT.gameObject.SetActive(false);
        // yourRewardTXT.color = new Color(yourRewardTXT.color.r, yourRewardTXT.color.g, yourRewardTXT.color.b, 0);

        backButton.SetActive(false);
        claimAllButton.gameObject.SetActive(false);
        claimAllButtonAd.gameObject.SetActive(false);

        // Заполняем очередь наград
        foreach (var reward in rewardsArray)
        {
            Debug.Log("ss");
            rewardsInChest.Enqueue(reward);
        }

        // Показываем первую награду
        if (rewardsInChest.Count > 0)
        {
            var firstReward = rewardsInChest.Dequeue();
            ShowReward(firstReward);
        }

        // Показываем текст "Ваша награда"
        yourRewardTXT.DOFade(1, 0.3f);
    }

    private void StartIdleTiltAnimation(RectTransform chestRect)
    {
        if (chestRect == null) return;

        // Останавливаем предыдущие анимации, если они есть
        DOTween.Kill(chestRect);
        if (!chestRect.gameObject.activeInHierarchy) return;

        // Анимация подпрыгивания и наклона
        Sequence idleSequence = DOTween.Sequence();

        // Подпрыгивание вверх
        idleSequence.Append(chestRect.DOAnchorPosY(idleJumpHeight, idleJumpDuration).SetEase(Ease.OutQuad));

        // Падение обратно с легким наклоном
        idleSequence.Append(chestRect.DOAnchorPosY(0f, idleJumpDuration * 1.5f)
            .SetEase(Ease.OutBounce));

        // Наклон влево
        idleSequence.Join(chestRect.DORotate(new Vector3(0, 0, -idleTiltAngle), idleTiltDuration / 2f)
            .SetEase(Ease.InOutQuad));

        // Возврат в исходное положение с дрожанием
        idleSequence.Append(chestRect.DORotate(Vector3.zero, idleTiltDuration / 2f)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                // Легкое дрожание (вибрация)
                chestRect.DOShakeRotation(
                    idleTiltDuration,
                    idleShakeRotationStrength,
                    idleShakeRotationVibrato,
                    90,
                    false
                ).SetEase(Ease.OutQuad);
            }));

        // Повторяем всю последовательность каждую секунду
        idleSequence.SetLoops(-1, LoopType.Restart).SetDelay(2f);
    }

    public void OpenCurrentChest()
    {
        // Debug.Log("Open Chest");
        if (rewardsInChest.Count == 0)
        {
            Debug.Log("No chest or rewards left");
            ShowFinal();
            return;
        }

        if (currentChest == null)
        {
            var rewardCard = rewardsInChest.Dequeue();
            ShowReward(rewardCard);
            return;
        }

        if (currentChest != null)
        {
            DOTween.Kill(chestRect);
            chestRect.rotation = Quaternion.identity; // Сброс поворота
        }

        // Анимация открытия сундука
        // RectTransform chestRect = currentChest.GetComponent<RectTransform>();
        Image chestImage = currentChest.chestImage;

        Sequence openSequence = DOTween.Sequence();

        // Увеличение и исчезновение 
        openSequence.Join(chestRect.DOScale(chestOpenScale, chestOpenDuration));
        openSequence.Join(chestImage.DOFade(0, chestOpenDuration));

        openSequence.Join(currentChest.openButtonTXT.DOFade(0, 0f));

        // openSequence.Append(()=>currentChest.chestImage.gameObject.SetActive(false));
        // Вызываем показ награды после небольшой задержки
        openSequence.AppendCallback(() =>
        {
            currentChest.chestImage.gameObject.SetActive(false);
            var rewardCard = rewardsInChest.Dequeue();
            ShowReward(rewardCard);
        });
    }

    private void ShowReward(RewardConfig rewardCard)
    {
        if (currentReward != null)
            currentReward.SetActive(false);

        Debug.Log("PlaySound?");
        // InstanceSoundUI.Instance.PlayGetItemSound();

        if (rewardCard.rewardPrefab.rewardType == RewardInChestType.Money ||
            rewardCard.rewardPrefab.rewardType == RewardInChestType.DonatMoney ||
            rewardCard.rewardPrefab.rewardType == RewardInChestType.HeroCard)
        {
            if (rewardCard.inRange)
            {
                randomRewardCount = (int)Random.Range((float)rewardCard.minAmount, (float)rewardCard.maxAmount);
                if (randomRewardCount == 0)
                {
                    Debug.Log($"You win 0?? At random!!!");
                    OpenCurrentChest();
                    return;
                }
                else
                {
                    // Debug.Log($"Random value - {randomRewardCount}");
                }
            }
            else
            {
                if (rewardCard.amount == 0)
                {
                    Debug.Log($"You win 0??");
                    OpenCurrentChest();
                    return;
                }
                else
                {
                    // Debug.Log($"Amount value - {rewardCard.amount}");
                }
            }
        }

        if (rewardCard.rewardPrefab.rewardType == RewardInChestType.Hero)
        {
            Debug.Log(1);
            if (rewardCard.cardReward != null && rewardCard.skinReward == null)
            {
                Debug.Log(2);
                int indexHero = rewardCard.randomHero ? (int)Random.Range(0, 8f) : rewardCard.cardReward.id;
                rewardCard.idHero = indexHero;
                // Debug.Log(rewardCard.idHero);
            }
            if (rewardCard.cardReward == null && rewardCard.skinReward != null)
            {
                Debug.Log(3);
                int indexSkin = rewardCard.randomSkin ? (int)Random.Range(0, 7f) : rewardCard.cardReward.id;
                rewardCard.idSkin = indexSkin;
                // Debug.Log(indexSkin);

                if (Geekplay.Instance.PlayerData.persons[rewardCard.idHero].openSkinBody[indexSkin] == 1)
                {
                    Debug.Log($"1 Current Skin we have");
                    OpenCurrentChest();
                    return;
                }
            }
            if (rewardCard.cardReward != null && rewardCard.skinReward != null)
            {
                // Debug.Log(4);
                // Есть оба варианта - случайный выбор
                if (Random.Range(0f, 1f) > 0.5f)
                {
                    if (Geekplay.Instance.PlayerData.persons[rewardCard.idHero].openSkinBody[rewardCard.idSkin] == 1)
                    {
                        Debug.Log($"2 Current Skin we have, idHero - {rewardCard.idHero}, idSkin - {rewardCard.idSkin}");
                        OpenCurrentChest();
                        return;
                    }
                }
                else
                {
                    Debug.Log($"2 Current Skin close, idHero - {rewardCard.idHero}, idSkin - {rewardCard.idSkin}\nTunr into HeroCard");
                    
                    rewardCard.rewardPrefab.rewardType = RewardInChestType.HeroCard;
                    // Меняем тип на карточки
                }
            }
            if (rewardCard.cardReward == null && rewardCard.skinReward == null)
            {
                if (Geekplay.Instance.PlayerData.openHeroes[rewardCard.rewardPrefab.id] == 1)
                {
                    Debug.Log($"3 Current Hero already open, idHero - {rewardCard.rewardPrefab.id}");
                    OpenCurrentChest();
                    return;
                }
            }
        }

        if (rewardCard != null && rewardCard.allSkin)
        {
            var person = Geekplay.Instance.PlayerData.persons[rewardCard.idHero];

            // foreach (var person in Geekplay.Instance.PlayerData.persons)
            // {
            for (int i = 0; i < person.openSkinBody.Length; i++)
            {
                person.openSkinBody[i] = 1;
            }

            for (int i = 0; i < person.openSkinHead.Length; i++)
            {
                person.openSkinHead[i] = 1;
            }
            // }
        }

        if (rewardCard.rewardPrefab.rewardType == RewardInChestType.RandomSkin)
        {
            var person = Geekplay.Instance.PlayerData.persons[rewardCard.idHero];

            if (person.openSkinBody[rewardCard.idSkin] == 1)
            {
                Debug.Log("Current skin is already open");
                InitInstaReward(ratingRewardDefault, true);
                OpenCurrentChest();
                return;
            }
            else
            {
                Debug.Log($"Current skin closed skinId={rewardCard.idSkin}");
            }
        }

        // Показываем награду
        InstanceSoundUI.Instance.PlayOpenChestSound();
        var card = Instantiate(rewardPrefab, RewardPanel.transform);

        card.type = rewardCard.rewardPrefab.rewardType;

        if (isInstantReward)
        {
            card.button.interactable = false;
            claimAllButtonAd.interactable = true;

            // Показываем кнопки "Забрать все"
            claimAllButton.image.DOFade(0, 0f);
            claimAllButton.image.DOFade(1, 1f);
            Debug.Log("aaa");
            claimAllButton.gameObject.SetActive(true);

            // Debug.Log(card.type);
            if (card.type == RewardInChestType.Hero || card.type == RewardInChestType.HeroCard)
            {
                // Debug.Log(1);
                card.tempState.SetActive(true);
                card.finalState.SetActive(false);

                claimAllButtonAd.gameObject.SetActive(false);
            }
            else
            {
                // Debug.Log(2);
                card.finalState.SetActive(true);
                card.tempState.SetActive(false);
            }
        }
        else
        {
            // Debug.Log("aaa");
            claimAllButtonAd.gameObject.SetActive(false);
            // Обычный процесс с анимациями
            card.tempState.SetActive(true);
            card.finalState.SetActive(false);
        }

        if (isNeededX2)
        {
            // Debug.Log("now?");
            claimAllButtonAd.image.DOFade(0, 0f);
            claimAllButtonAd.image.DOFade(1, 1f);
            if (isInstantReward)
            {
                claimAllButtonAd.gameObject.SetActive(true);
            }
            else
            {
                claimAllButtonAd.gameObject.SetActive(false);
            }
        }
        else
        {
            claimAllButtonAd.gameObject.SetActive(false);
        }

        card.Init(rewardCard);

        if (card.rewardValue == 0 && card.type != RewardInChestType.RandomSkin && card.type != RewardInChestType.Hero)
        {
            Debug.Log("Удаляем не нужную награду");
            Destroy(card.gameObject);
            OpenCurrentChest();
            return;
        }

        switch (card.type)
        {
            case RewardInChestType.Money:
                card.shine.color = common;
                card.infoTXT.color = common;
                break;
            case RewardInChestType.DonatMoney:
                card.shine.color = rare;
                card.infoTXT.color = rare;
                break;
            case RewardInChestType.HeroCard:
                card.shine.color = rare;
                card.infoTXT.color = rare;
                break;
            case RewardInChestType.RandomSkin:
                card.shine.color = rare;
                card.infoTXT.color = rare;
                break;
            case RewardInChestType.Hero:
                card.shine.color = epic;
                card.infoTXT.color = epic;
                break;
            default:
                card.shine.color = Color.white;
                break;
        }

        currentReward = card.gameObject;
        allReward.Add(card);
        // Debug.Log("Card add");

        var button = card.button;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                // Debug.Log("????");
                // Отключаем кнопку на время анимации
                button.interactable = false;
                GainReward(card);
            });
        }
    }

    private void ShowFinal()
    {
        // Debug.Log("final");
        // Debug.Log("All rewards shown");
        // Destroy(currentChest.gameObject);

        // Скрываем кнопки и деактивируем все награды
        if (isNeededX2)
        {
            // Debug.Log(1);
            claimAllButtonAd.gameObject.SetActive(true);
        }
        else
        {
            // Debug.Log(2);
            claimAllButtonAd.gameObject.SetActive(false);
        }

        if (isInstantReward)
        {
            yourRewardTXT.DOFade(1, 0.3f);
        }
        else
        {
            claimAllButton.gameObject.SetActive(false);
            claimAllButtonAd.gameObject.SetActive(false);

            if (currentChest != null)
                currentChest.openButton.gameObject.SetActive(false);

            foreach (var reward in allReward)
            {
                if (reward.type == RewardInChestType.HeroCard)
                {
                    reward.SetNativeSizeWithMultiplier(1.2f);
                    // Debug.Log("ZZZZZ");
                }
                if (reward.type == RewardInChestType.RandomSkin)
                {
                    reward.SetNativeSizeWithMultiplier(1.2f);
                    // Debug.Log("ZZZZZ");
                }
                if (reward.type == RewardInChestType.Money || reward.type == RewardInChestType.DonatMoney)
                {
                    reward.SetNativeSizeWithMultiplier(1f);
                    // Debug.Log("aaaaaaaaaaaaaa");
                }
                if (reward.type == RewardInChestType.Hero)
                {
                    reward.SetNativeSizeWithMultiplier(1.2f);
                    // Debug.Log("ZZZZZ");
                }

                reward.gameObject.SetActive(false);
                reward.button.interactable = false;
            }

            CreateRewardsSequence();
            rewardsSequence.Play();
            yourRewardTXT.DOFade(1, 0.3f);
        }

        // if (isProcessingMultipleChests)
        // {
        //     // Небольшая задержка перед следующим сундуком
        //     DOVirtual.DelayedCall(1.5f, ProcessNextChest);
        // }
    }

    private void GainReward(RewardInChest card)
    {
        // Debug.Log("GainReward");
        // Анимация получения награды
        Sequence gainSequence = DOTween.Sequence();

        gainSequence.Append(card.itemImage.transform.DOScale(rewardGainScale, rewardGainDuration).SetEase(Ease.OutQuad));

        gainSequence.Join(card.itemImage.DOFade(0, rewardGainDuration * 2));
        gainSequence.Join(card.shine.DOFade(0, rewardGainDuration));
        gainSequence.Join(card.heroCardSlider.transform.DOScale(0, rewardGainDuration));

        gainSequence.Join(card.rewardAmountTXT.DOFade(0, rewardGainDuration));

        if (card.top.rectTransform != null)
        {
            gainSequence.Join(card.top.rectTransform.DOAnchorPos(new Vector2(2000, 60), 0.3f));
            gainSequence.Join(card.bottom.rectTransform.DOAnchorPos(new Vector2(-2000, -60), 0.3f));
        }

        if (rewardsInChest.Count > 0)
        {
            OpenCurrentChest();
        }
        else
        {
            ShowFinal();
        }
    }

    private void CreateRewardsSequence()
    {
        rewardsSequence?.Kill();
        activeRewardTweens.Clear();
        rewardsSequence = DOTween.Sequence();

        isAnimating = true;
        float delayBetween = rewardAppearDuration * (1f - overlapPercentage);

        foreach (var reward in allReward)
        {
            rewardsSequence.AppendCallback(() =>
            {
                reward.gameObject.SetActive(true);
                reward.button.gameObject.SetActive(false);

                reward.tempState.SetActive(false);
                reward.finalState.SetActive(true);

                // AnimateSingleReward(reward);
                AnimateSingleRewardFinal(reward);
            });
            rewardsSequence.AppendInterval(delayBetween);
        }

        rewardsSequence.OnComplete(OnAllAnimationsComplete);
    }

    private void AnimateSingleRewardFinal(RewardInChest reward)
    {
        if (reward == null || reward.transform == null || reward.itemFinalImage == null)
        {
            return;
        }
        // Очищаем старые твины для этой награды
        DOTween.Kill(reward.transform);
        DOTween.Kill(reward.itemImage);
        DOTween.Kill(reward.rewardAmountTXT);

        // Начальное состояние
        reward.transform.localScale = Vector3.one * 0.3f;
        reward.itemFinalImage.color = new Color(1, 1, 1, 0);
        reward.rewardFinalAmountTXT.alpha = 0;
        reward.rewardFinalAmountTXT.transform.localScale = Vector3.zero;

        // Запускаем анимации и сохраняем их
        var scaleTween = reward.transform.DOScale(Vector3.one, rewardAppearDuration)
            .SetEase(Ease.OutBack);

        var fadeImageTween = reward.itemFinalImage.DOFade(1, rewardAppearDuration * 0.7f);

        var fadeTextTween = reward.rewardFinalAmountTXT.DOFade(1, rewardAppearDuration * 0.5f)
            .SetDelay(rewardAppearDuration * 0.3f);

        var sizeTextTween = reward.rewardFinalAmountTXT.transform.DOScale(1, rewardAppearDuration * 0.1f)
            .SetDelay(rewardAppearDuration * 0.3f);

        // Сохраняем твины для управления
        activeRewardTweens.Add(scaleTween);
        activeRewardTweens.Add(fadeImageTween);
        activeRewardTweens.Add(fadeTextTween);
        activeRewardTweens.Add(sizeTextTween);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isAnimating) return;

        SkipAllAnimations();
    }

    private void SkipAllAnimations()
    {
        // Прерываем основную последовательность
        rewardsSequence?.Kill();

        // Прерываем все отдельные анимации наград
        foreach (var tween in activeRewardTweens)
        {
            if (tween.IsActive()) tween.Complete();
        }
        activeRewardTweens.Clear();

        // Активируем все награды и устанавливаем финальное состояние
        foreach (var reward in allReward)
        {
            if (!reward.gameObject.activeSelf)
            {
                reward.gameObject.SetActive(true);
                reward.button.gameObject.SetActive(true);

                reward.tempState.SetActive(false);
                reward.finalState.SetActive(true);
            }

            reward.transform.localScale = Vector3.one;
            reward.itemImage.color = Color.white;
            reward.rewardAmountTXT.alpha = 1;
            reward.rewardAmountTXT.transform.localScale = Vector3.one;

            reward.button.interactable = false;
        }

        OnAllAnimationsComplete();
    }

    private void OnAllAnimationsComplete()
    {
        isAnimating = false;
        activeRewardTweens.Clear();

        // Анимация появления кнопок
        claimAllButton.gameObject.SetActive(true);

        if (isNeededX2)
        {
            claimAllButtonAd.gameObject.SetActive(true);
        }
        else
        {
            claimAllButtonAd.gameObject.SetActive(false);
        }

        claimAllButton.transform.localScale = Vector3.zero;
        claimAllButtonAd.transform.localScale = Vector3.zero;

        claimAllButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        claimAllButtonAd.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    public void ClaimAllReward()
    {
        Debug.Log("Claim all");

        int totalMoney = 0;
        int totalDonat = 0;
        Dictionary<int, int> heroCards = new Dictionary<int, int>();

        foreach (var reward in allReward)
        {
            DOTween.Kill(reward.transform);
            DOTween.Kill(reward.itemImage);
            DOTween.Kill(reward.rewardAmountTXT);

            switch (reward.type)
            {
                case RewardInChestType.Money:
                    totalMoney += reward.rewardValue;
                    reward.shine.color = common;
                    break;
                case RewardInChestType.DonatMoney:
                    totalDonat += reward.rewardValue;
                    reward.shine.color = rare;
                    break;
                case RewardInChestType.HeroCard:
                    if (!heroCards.ContainsKey(reward.id))
                        heroCards[reward.id] = 0;
                    heroCards[reward.id] += reward.rewardValue;
                    reward.shine.color = rare;
                    break;
                case RewardInChestType.Hero:
                    Geekplay.Instance.PlayerData.openHeroes[reward.id] = 1;
                    reward.shine.color = epic;
                    break;
                case RewardInChestType.RandomSkin:
                    reward.shine.color = rare;
                    break;
            }

            ApplyReward(reward);
            Destroy(reward.gameObject);
        }

        WebSocketBase.Instance.ClaimRewards(totalMoney, totalDonat, heroCards);
        allReward.Clear();

        Geekplay.Instance.Save();

        if (!isProcessingMultipleChests)
        {
            MainMenu.Instance.OpenMenu();
            gameObject.SetActive(false);
        }
        else
        {
            // Вместо автоматического вызова ProcessNextChest, просто активируем кнопку "Открыть" для следующего сундука
            if (currentChest != null)
            {
                currentChest.openButton.interactable = true;
                currentChest.openButton.gameObject.SetActive(true);
            }

            // Очищаем текущий сундук
            if (currentChest != null)
            {
                Destroy(currentChest.gameObject);
                currentChest = null;
            }

            // Инициализируем следующий сундук
            ProcessNextChest();
        }
    }

    private void ApplyReward(RewardInChest card)
    {
        switch (card.type)
        {
            case RewardInChestType.Money:
                Currency.Instance.AddMoney(card.rewardValue);
                break;
            case RewardInChestType.DonatMoney:
                Currency.Instance.AddDonatMoney(card.rewardValue);
                break;
            case RewardInChestType.HeroCard:
                HeroCards.Instance.AddCardHero(card.id, card.rewardValue);
                break;
            case RewardInChestType.Hero:
                // Debug.Log("card.id - " + card.id);
                if (Geekplay.Instance.PlayerData.openHeroes[card.id] != 1)
                    Geekplay.Instance.PlayerData.openHeroes[card.id] = 1;
                break;
            case RewardInChestType.RandomSkin:
            //ya xz otkyda +1, no tak nado
                Geekplay.Instance.PlayerData.persons[card.idHero].openSkinBody[card.idSkin + 1] = 1;
                Geekplay.Instance.PlayerData.persons[card.idHero].openSkinHead[card.idSkin + 1] = 1;
                break;
        }
    }

    public void ClaimAllAd()
    {
        if (isInstantReward)
        {
            // Debug.Log(1);
            var reward = allReward[0];
            UpdateRewardsUI(reward);
            // claimAllButtonAd.interactable = false;
            claimAllButtonAd.gameObject.SetActive(false);
            // reward.rewardAmountTXT.text = (reward.rewardValue * 2).ToString();
        }
        else
        {
            // Debug.Log(2);
            isNeededX2 = false;
            overlapPercentage = 0.85f;
            foreach (var item in allReward)
            {
                GainRewardFinal(item);
                UpdateRewardsUI(item);
            }
            overlapPercentage = 0.55f;
            // claimAllButton.gameObject.SetActive(false);
            claimAllButtonAd.gameObject.SetActive(false);
        }
    }

    private void UpdateRewardsUI(RewardInChest item)
    {
        item.UpdateUI(item.rewardValue * 2);
    }
    private void GainRewardFinal(RewardInChest card)
    {
        // Анимация получения награды
        Sequence gainSequence = DOTween.Sequence();

        gainSequence.Append(card.transform.DOScale(rewardGainScale, rewardGainDuration).SetEase(Ease.OutQuad));
        // gainSequence.Join(card.itemImage.DOFade(0, rewardGainDuration));

        if (rewardsInChest.Count > 0)
        {
            OpenCurrentChest();
        }
        else
        {
            ShowFinal();
        }
    }

    private void OnDestroy()
    {
        rewardsSequence?.Kill();
        foreach (var tween in activeRewardTweens)
        {
            if (tween.IsActive()) tween.Kill();
        }
    }
}