using UnityEngine;

public class Minigun : Weapon
{
    public override void OnValidate()
    {
        base.OnValidate();
        ammoOverall = 0;
        magazineSize = 30;
        ammoAmountToPickUp = 90;

        reloadTime = 2f;
        fireRate = 0.1f;
        range = 5f;
        noiseShoot = 5;
    }
    protected override void Awake()
    {
        base.Awake();

        magazineSize = 30;
        ammoAmountToPickUp = 90;

        reloadTime = 2f;
        fireRate = 0.1f;
        range = 5f;
        noiseShoot = 5;

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