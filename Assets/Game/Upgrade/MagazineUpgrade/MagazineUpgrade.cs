using UnityEngine;

public class MagazineUpgrade : Upgrade
{
    private float magazineBonus = 0.25f;
    private float reloadBonus = 0.25f;
    public override void ApplyBoostEffect(Player player)
    {
        if (player != null)
        {
            player.Character.MainWeapon.magazineSize = Mathf.RoundToInt(player.Character.MainWeapon.maxMagazineSize * (1 + magazineBonus));
            player.Character.MainWeapon.reloadTime = player.Character.MainWeapon.InitReloadTime / (1 + reloadBonus);
            player.Character.ammoInfo.UpdateUI();
        }
    }
    
    public override void RemoveBoostEffect(Player player)
    {
        if (player != null)
        {
            player.Character.MainWeapon.magazineSize = Mathf.RoundToInt(player.Character.MainWeapon.maxMagazineSize / (1 + magazineBonus));
            player.Character.MainWeapon.reloadTime = player.Character.MainWeapon.InitReloadTime * (1 + reloadBonus);
            player.Character.ammoInfo.UpdateUI();
        }
    }

    public override bool CanPickUp(Player player)
    {
        return player != null && !HaveUpgrade(player);
    }
}
