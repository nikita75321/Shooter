using UnityEngine;

public class HMGDefault : Weapon
{
    public override void OnValidate()
    {
        base.OnValidate();
        ammoOverall = 0;
        magazineSize = 45;
        ammoAmountToPickUp = 90;

        reloadTime = 1.5f;
        fireRate = 0.15f;
        range = 6f;
        noiseShoot = 4;
    }
    protected override void Awake()
    {
        base.Awake();

        magazineSize = 45;
        ammoAmountToPickUp = 90;

        reloadTime = 1.5f;
        fireRate = 0.15f;
        range = 6f;
        noiseShoot = 4;

        weaponClass = WeaponClass.main;
        currentAmmo = 0;
    }
    
    protected override void SpawnVisualBullet(Vector3 origin, Vector3 direction)
    {
        // Стандартная визуализация пули
        base.SpawnVisualBullet(origin, direction);

        // Эффект попадания
        if (Physics.Raycast(origin, direction, out RaycastHit hit, range) &&
            bulletPrefab != null)
        {
            // Instantiate(bulletPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }
    }
}
