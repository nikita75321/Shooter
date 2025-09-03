using TMPro;
using UnityEngine;

public class AmmoInfo : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private TMP_Text ammoTxt, ammoOveralTxt;

    private void OnValidate()
    {
        if (player == null) player = FindAnyObjectByType<Player>();
    }

    public void UpdateUI()
    {
        // Debug.Log("UpdateUI ammo");
        // Debug.Log(player+" - player");
        // Debug.Log(player.Character+" - player.Character");
        // Debug.Log(player.Character.CurrentWeapon+" - player.Character.CurrentWeapon");
        // Debug.Log(player.Character.CurrentWeapon.currentAmmo+" - player.Character.CurrentWeapon.currentAmmo");

        ammoTxt.text = $"{player.Character.CurrentWeapon.currentAmmo}/{player.Character.CurrentWeapon.magazineSize}";
        ammoOveralTxt.text = $"{player.Character.CurrentWeapon.ammoOverall}";
    }
}
