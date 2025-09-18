public class DamageUpgrade : Upgrade
{
    private float damageBonus = 0.2f;
    private float damagePenetration = 0.2f;

    public void OnValidate()
    {
        type = UpgradeType.damage;
    }

    public override void ApplyBoostEffect(Player player)
    {
        if (player != null)
        {
            UpgradesManager.Instance.PickUpUpgrade(id);
            player.Character.MainWeapon.damage = player.Character.MainWeapon.damage + player.Character.MainWeapon.damage * damageBonus;
            player.Character.MainWeapon.armorPenetration = damagePenetration;
        }
    }
    
    public override void RemoveBoostEffect(Player player)
    {
        if (player != null)
        {
            player.Character.MainWeapon.damage = player.Character.MainWeapon.InitMaxDamage - player.Character.MainWeapon.InitMaxDamage * damageBonus;
            player.Character.MainWeapon.armorPenetration = damagePenetration;
        }
    }

    public override bool CanPickUp(Player player)
    {
        return player != null && !HaveUpgrade(player);
    }
}
