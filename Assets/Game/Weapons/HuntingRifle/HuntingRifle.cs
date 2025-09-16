using UnityEngine;

public class HuntingRifle : Weapon
{
    public override void OnValidate()
    {
        base.OnValidate();
        ammoOverall = 0;
        magazineSize = 8;
        ammoAmountToPickUp = 16;

        reloadTime = 1f;
        fireRate = 0.3f;
        range = 3.5f;
        noiseShoot = 8;
    }
    protected override void Awake()
    {
        base.Awake();

        magazineSize = 8;
        ammoAmountToPickUp = 24;

        reloadTime = 1f;
        fireRate = 0.3f;
        range = 3.5f;
        noiseShoot = 8;

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
