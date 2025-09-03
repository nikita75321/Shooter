using UnityEngine;

public class Kycha : MonoBehaviour
{
    public static Kycha Instance;

    [Header("Hero Icons")]
    public Sprite[] heroIcons;

    [Header("Chest Icons")]
    public Sprite[] chestIcons;
    public Sprite Uncommon_chest;
    public Sprite Rare_chest;
    public Sprite Epic_chest;
    public Sprite Legendary_chest;

    [Header("Money Icons")]
    public Sprite money;
    public Sprite donatMoney;

    private void Awake()
    {
        Instance = this;
    }
}
