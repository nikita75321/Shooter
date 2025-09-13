using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Player player;

    [Header("Health")]
    public Health Health;

    [Header("Kits")]
    public int maxHealthKits = 3;
    public int currentHealthKits;

    [Header("Armor")]
    public Armor Armor;
    [SerializeField] protected float maxArmor = 50f;
    [SerializeField] protected float currentArmor;

    [Header("Ammo")]
    public AmmoInfo ammoInfo;

    [Header("Weapon")]
    public WeaponClass currentWeaponType;
    [field: SerializeField] public Weapon CurrentWeapon { get; protected set; }
    [field: SerializeField] public Weapon MainWeapon { get; protected set; }
    [field: SerializeField] public Weapon SecondaryWeapon { get; protected set; }
    public AimingCone aimingCone;

    private void OnValidate()
    {
        if (Health == null) Health = GetComponent<Health>();
        if (Armor == null) Armor = GetComponent<Armor>();
        if (ammoInfo == null) ammoInfo = GetComponentInChildren<AmmoInfo>();
        if (aimingCone == null) aimingCone = GetComponentInChildren<AimingCone>();
        if (player == null) player = GetComponentInParent<Player>();
    }

    protected virtual void Awake()
    {
        if (Health == null) Health = GetComponent<Health>();
        if (ammoInfo == null) ammoInfo = GetComponentInChildren<AmmoInfo>();

        currentArmor = maxArmor;
        currentHealthKits = 0;
        Health.useReviveImage.fillAmount = 0;
    }

    public void InitWeapon(Weapon weapon)
    {
        if (weapon == null)
            return;

        Debug.Log($"Initializing weapon: {weapon.name}");

        // Деактивируем текущее оружие, если оно есть
        if (CurrentWeapon != null)
        {
            CurrentWeapon.gameObject.SetActive(false);
            CurrentWeapon.enabled = false;
        }

        // Устанавливаем новое оружие
        CurrentWeapon = weapon;
        CurrentWeapon.gameObject.SetActive(true);
        CurrentWeapon.enabled = true;

        if (CurrentWeapon.weaponClass == WeaponClass.secondary)
        {
            currentWeaponType = WeaponClass.secondary;
        }
        else
        {
            currentWeaponType = WeaponClass.main;
        }

        // Обновляем UI и прицеливание
        if (aimingCone != null)
        {
            aimingCone.Init(weapon);
            // Debug.Log("aimingCone Init");
        }

        if (ammoInfo != null)
        {
            ammoInfo.UpdateUI();
            // Debug.Log("UpdateUI ammoInfo");
        }
    }

    public void AddAmmo()
    {
        // Debug.Log("AddAmmo 1");
        // CurrentWeapon.StopReload();
        MainWeapon.AddAmmo();
        // Debug.Log(MainWeapon.ammoOverall);
        // if (MainWeapon.currentAmmo == 0 && MainWeapon.ammoOverall > 0)
        // {
        if (CurrentWeapon != MainWeapon)
        {
            SecondaryWeapon.StopReload();
            player.SetMainWeapon();
            // CurrentWeapon.StartReload();
        }
        else
        {
            // MainWeapon.StartReload();
        }
    }
    public void AddAmmo(int value)
    {
        // Debug.Log("AddAmmo 2");
        MainWeapon.AddAmmo(value);
        if (CurrentWeapon != MainWeapon)
        {
            player.SetMainWeapon();
            Debug.Log(1);
        }
        else
        {
            Debug.Log(2);
        }
    }
}