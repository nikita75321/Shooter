using System;
using System.Collections.Generic;
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
}