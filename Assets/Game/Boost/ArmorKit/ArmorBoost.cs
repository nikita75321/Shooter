public class ArmorBoost : Boost
{
    public int armorAmount = 1000;

    public override void ApplyBoostEffect(Player player)
    {
        // Находим игрока и добавляем ему броню
        // var player = FindObjectOfType<PlayerHealth>(); // Замените на ваш скрипт
        if (player != null)
        {
            player.Character.Armor.ArmorIncrease(armorAmount);
            // Character.Health.ArmorIncrease(armorAmount);
        }
    }

    public override bool CanPickUp(Player player)
    {
        return player != null && 
               player.Character.Armor.CurrentArmor < player.Character.Armor.MaxArmor;
    }
}