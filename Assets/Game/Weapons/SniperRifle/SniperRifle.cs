using UnityEngine;

public class SniperRifle : Weapon
{
    public override void OnValidate()
    {
        base.OnValidate();
        ammoOverall = 0;
        magazineSize = 20;
        ammoAmountToPickUp = 20;

        reloadTime = 2.5f;
        fireRate = 0.8f;
        range = 7f;
        noiseShoot = 6;
    }
    protected override void Awake()
    {
        base.Awake();

        magazineSize = 20;
        ammoAmountToPickUp = 20;

        reloadTime = 2.5f;
        fireRate = 0.8f;
        range = 7f;
        noiseShoot = 6;

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
