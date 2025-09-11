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
            player.Character.currentHealthKits++;
            // Обновляем UI через AidKit
            AidKit aidKit = player.Character.Health.aidKit;
            if (aidKit != null)
            {
                aidKit.UpdateKitText();
                // Если достигли максимума, останавливаем заполнение
                if (player.Character.currentHealthKits >= player.Character.maxHealthKits)
                {
                    aidKit.StopFilling();
                }
                else if (!aidKit.isFilling)
                {
                    aidKit.StartFilling();
                }
            }
        }
    }
}