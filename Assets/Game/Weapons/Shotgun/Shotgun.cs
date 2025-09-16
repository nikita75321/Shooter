using UnityEngine;

public class Shotgun : Weapon
{
    public override void OnValidate()
    {
        base.OnValidate();
        ammoOverall = 0;
        magazineSize = 20;
        ammoAmountToPickUp = 40;

        reloadTime = 1.5f;
        fireRate = 0.5f;
        range = 3f;
        noiseShoot = 7;
    }
    protected override void Awake()
    {
        base.Awake();

        magazineSize = 8;
        ammoAmountToPickUp = 24;

        reloadTime = 1.5f;
        fireRate = 0.5f;
        range = 3f;
        noiseShoot = 7;

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
