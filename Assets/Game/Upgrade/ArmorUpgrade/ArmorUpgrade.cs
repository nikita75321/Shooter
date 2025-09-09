using UnityEngine;

public class ArmorUpgrade : Upgrade
{
    private float armorBonus = 0.5f;
    public override void ApplyBoostEffect(Player player)
    {
        Debug.Log("ApplyBoostEffect");
        if (player != null)
        {
            player.Character.Armor.MaxArmor = player.Character.Armor.initMaxArmor * (1 + armorBonus);
        }
    }
    
    public override void RemoveBoostEffect(Player player)
    {
        Debug.Log("RemoveBoostEffect");
        if (player != null)
        {
            player.Character.Armor.MaxArmor = player.Character.Armor.initMaxArmor / (1 + armorBonus);
        }
    }

    public override bool CanPickUp(Player player)
    {
        return player != null && !HaveUpgrade(player);
    }
}
