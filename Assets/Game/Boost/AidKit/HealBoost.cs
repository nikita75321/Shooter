public class HealBoost : Boost
{
    public void OnValidate()
    {
        type = BoostType.aidkit;
    }

    public override bool CanPickUp(Player player)
    {
        return player != null &&
               player.Character.currentHealthKits < player.Character.maxHealthKits;
    }

    public override void ApplyBoostEffect(Player player)
    {
        if (player != null)
        {
            BoostsManager.Instance.PickUpBoost(id);
        }
    }
}