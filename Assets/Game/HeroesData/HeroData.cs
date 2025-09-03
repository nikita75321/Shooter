using UnityEngine;

public enum HeroClass
{
    Assault,
    Sniper,
    Tank
}
public enum Rarity
{
    common,
    uncommon,
    rare,
    mythical,
    legendary,
    god
}

[CreateAssetMenu(fileName = "New Hero", menuName = "Game/Hero Data")]
public class HeroData : ScriptableObject
{
    public int id;
    public string heroName;
    public HeroClass heroClass;
    public Rarity heroRarity;
    

    public int health;
    public int armor;
    public int damage;

    public int visionRadius;
}