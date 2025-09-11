using UnityEngine;

public class AmmoBoost : Boost
{
    public void OnValidate()
    {
        type = BoostType.ammo;
    }

    public override void ApplyBoostEffect(Player player)
    {
        // Находим игрока и добавляем ему патроны
        if (player != null)
        {
            // Debug.Log(player);
            BoostsManager.Instance.PickUpBoost(id);
        }
    }

    public override bool CanPickUp(Player player)
    {
        return true;
    }
}