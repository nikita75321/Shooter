public class ArmorUpgrade : Upgrade
{
    private float armorBonus = 0.5f;
    public override void ApplyBoostEffect(Player player)
    {
        if (player != null)
        {
            player.Character.Armor.MaxArmor = player.Character.Armor.initMaxArmor * (1 + armorBonus);
            // player.Controller.VisionRadius += player.Controller.MaxVisionRadius * visionBonus;
        }
    }
    
    public override void RemoveBoostEffect(Player player)
    {
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
