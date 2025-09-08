using System;
using Sirenix.OdinInspector;
using UnityEngine;

public enum RewardInChestType
{
    Money,
    DonatMoney,
    HeroCard,
    Hero,
    RandomSkin
}

[Serializable]
public class RandomSkin
{
    public Sprite[] sprites;
}

[CreateAssetMenu(fileName = "NewReward", menuName = "RewardInChest")]
public class RewardInChestSO : ScriptableObject
{
    [HideIf("IsRandomSkin")]
    public Sprite icon;
    public RewardInChestType rewardType;

    [ShowIf("IsShoodIdShow")]
    public int id;
    private bool IsShoodIdShow => (rewardType == RewardInChestType.HeroCard || rewardType == RewardInChestType.Hero)
                                    || (!IsRandomSkin);

    [ShowIf("IsRandomSkin")]
    public RandomSkin[] randomSkins;
    private bool IsRandomSkin => rewardType == RewardInChestType.RandomSkin;

    public int weight = 1;

    public void OnValidate()
    {
        switch (rewardType)
        {
            case RewardInChestType.Money: weight = 15; break;
            case RewardInChestType.DonatMoney: weight = 25; break;
            case RewardInChestType.HeroCard: weight = 40; break;
            case RewardInChestType.RandomSkin: weight = 60; break;
            case RewardInChestType.Hero: weight = 90; break;
        }
    }
}