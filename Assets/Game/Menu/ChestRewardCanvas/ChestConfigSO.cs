// ChestConfigSO.cs
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewChestConfig", menuName = "Chests/Chest Config")]
public class ChestConfigSO : ScriptableObject
{
    public GameObject chestPrefab;
    public Sprite chestSprite;
    public List<RewardConfig> rewards;
}

[System.Serializable]
public class RewardConfig
{
    public RewardInChestSO rewardPrefab;

    [ShowIf("IsHeroSelected")]
    public RewardInChestSO cardReward;
    [ShowIf("IsHeroSelected")]
    public RewardInChestSO skinReward;

    // Amount показывается только если:
    // 1) Не герой И inRange = false, ЛИБО
    // 2) Герой И нет карты
    [ShowIf("ShouldShowAmount")]
    public int amount;

    // Min/Max показываются если:
    // 1) Не герой И inRange = true, ЛИБО
    // 2) Герой И есть карта
    [ShowIf("ShouldShowMinMax")]
    public int minAmount;

    [ShowIf("ShouldShowMinMax")]
    public int maxAmount;

    // inRange скрывается только если герой И нет карты
    [HideIf("ShouldHideInRange")]
    public bool inRange;

    [ShowIf("ShowIdHero")]
    public int idHero;
    [ShowIf("ShowRandomHero")]
    public bool randomHero;

    [ShowIf("ShowIdSkin")]
    public int idSkin;

    [ShowIf("ShowRandomSkin")]
    public bool randomSkin;

    [HideIf("IsRandomSkin")]
    public bool allSkin;

    public bool isRandomReward;

    [Range(0, 100), ShowIf("isRandomReward")]
    public int chance;

    // === Условия видимости ===
    private bool IsRewardEmpty => rewardPrefab == null;
    private bool IsSkinEmpty => skinReward == null;
    private bool IsRandomSkinSelected => rewardPrefab != null &&
                                      rewardPrefab.rewardType == RewardInChestType.RandomSkin;
    private bool IsHeroSelected => rewardPrefab != null &&
                                 rewardPrefab.rewardType == RewardInChestType.Hero;

    // Условия для полей героя
    private bool ShowIdHero => (IsHeroSelected && skinReward != null && !randomHero) ||
                                (rewardPrefab != null && rewardPrefab.rewardType == RewardInChestType.RandomSkin && !randomHero) ||
                                !allSkin && !randomHero;
    private bool ShowRandomHero => (IsHeroSelected && skinReward != null && !allSkin) ||
                                (rewardPrefab != null && rewardPrefab.rewardType == RewardInChestType.RandomSkin) ||
                                !allSkin;

    // Условия для полей скина
    private bool ShowIdSkin => (IsHeroSelected && skinReward != null && !randomSkin && !allSkin) ||
                                (rewardPrefab != null && rewardPrefab.rewardType == RewardInChestType.RandomSkin && !randomSkin && !allSkin) ||
                                !allSkin && !randomSkin;
    private bool ShowRandomSkin => IsHeroSelected && skinReward != null && !allSkin ||
                                    rewardPrefab != null && rewardPrefab.rewardType == RewardInChestType.RandomSkin && !allSkin ||
                                    !allSkin;

    private bool ShouldHideInRange => IsHeroSelected && cardReward == null || IsRandomSkinSelected;

    private bool ShouldShowAmount => (!inRange && cardReward != null) || (!inRange && !IsHeroSelected && !IsRandomSkinSelected);

    private bool ShouldShowMinMax =>
        (!IsHeroSelected && inRange) ||
        (IsHeroSelected && cardReward != null && inRange) ||
        (!IsRandomSkinSelected && inRange);

    private bool IsRandomSkin => randomSkin;
}