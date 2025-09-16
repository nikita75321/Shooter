public class NoiseUpgrade : Upgrade
{
    private float noizeBonus = 0.3f;

    public void OnValidate()
    {
        type = UpgradeType.noize;
    }

    public override void ApplyBoostEffect(Player player)
    {
        if (player != null)
        {
            UpgradesManager.Instance.PickUpUpgrade(id);
            player.Character.MainWeapon.noiseShoot = player.Character.MainWeapon.InitNoiseShoot / (1 + noizeBonus);
        }
    }
    
    public override void RemoveBoostEffect(Player player)
    {
        if (player != null)
        {
            player.Character.MainWeapon.noiseShoot = player.Character.MainWeapon.InitNoiseShoot * (1 + noizeBonus);
        }
    }

    public override bool CanPickUp(Player player)
    {
        return player != null && !HaveUpgrade(player);
    }
}
