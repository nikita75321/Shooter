public class AimUpgrade : Upgrade
{
    private float AimBonusAngle = 0.25f;
    private float AimBonusLenght = 0.25f;

    public void OnValidate()
    {
        type = UpgradeType.aim;
    }

    public override void ApplyBoostEffect(Player player)
    {
        if (player != null)
        {
            UpgradesManager.Instance.PickUpUpgrade(id);
            player.Character.MainWeapon.maxAimAngle = player.Character.MainWeapon.InitMaxAngle - player.Character.MainWeapon.InitMaxAngle * AimBonusAngle;
            player.Character.MainWeapon.minAimAngle = player.Character.MainWeapon.minAimAngle - player.Character.MainWeapon.minAimAngle * AimBonusAngle;
            player.Character.MainWeapon.range = player.Character.MainWeapon.InitMaxRange + player.Character.MainWeapon.InitMaxRange * AimBonusLenght;
            // player.Controller.VisionRadius += player.Controller.MaxVisionRadius * visionBonus;
        }
    }
    
    public override void RemoveBoostEffect(Player player)
    {
        if (player != null)
        {
            // UpgradesManager.Instance.(id);
            player.Character.MainWeapon.maxAimAngle = player.Character.MainWeapon.InitMaxAngle + player.Character.MainWeapon.InitMaxAngle * AimBonusAngle;
            player.Character.MainWeapon.minAimAngle = player.Character.MainWeapon.minAimAngle + player.Character.MainWeapon.minAimAngle * AimBonusAngle;
            player.Character.MainWeapon.range = player.Character.MainWeapon.InitMaxRange - player.Character.MainWeapon.InitMaxRange * AimBonusLenght;
            // player.Controller.VisionRadius -= player.Controller.MaxVisionRadius * visionBonus;
        }
    }

    public override bool CanPickUp(Player player)
    {
        return player != null && !HaveUpgrade(player);
    }
}
