public class VisionUpgrade : Upgrade 
{
    private float visionBonus = 0.2f; // 15% от максимального радиуса
    
    public void OnValidate()
    {
        type = UpgradeType.vision;
    }

    public override void ApplyBoostEffect(Player player)
    {
        if (player != null)
        {
            UpgradesManager.Instance.PickUpUpgrade(id);
            player.Controller.VisionRadius += player.Controller.MaxVisionRadius * visionBonus;
        }
    }
    
    public override void RemoveBoostEffect(Player player)
    {
        if (player != null)
        {
            player.Controller.VisionRadius -= player.Controller.MaxVisionRadius * visionBonus;
        }
    }

    public override bool CanPickUp(Player player)
    {
        return player != null && !HaveUpgrade(player);
    }
}
