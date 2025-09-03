using UnityEngine;

public class AmmoBoost : Boost
{
    public override void ApplyBoostEffect(Player player)
    {
        // Находим игрока и добавляем ему патроны
        if (player != null)
        {
            Debug.Log(player);
            player.Character.AddAmmo();
        }
    }

    public override bool CanPickUp(Player player)
    {
        return true;
    }
}