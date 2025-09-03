using UnityEngine;

public class Pistol : Weapon
{
    public override void OnValidate()
    {
        base.OnValidate();
        ammoOverall = 9999;
        magazineSize = 7;
        reloadTime = 1f;
        fireRate = 0.5f;
        range = 5f;
        noiseShoot = 2f;

        currentAmmo = magazineSize;
    }

    protected override void Awake()
    {
        base.Awake();
        ammoOverall = 9999;
        magazineSize = 7;
        reloadTime = 1f;
        fireRate = 0.5f;
        range = 5f;
        noiseShoot = 2f;

        weaponClass = WeaponClass.secondary;
        currentAmmo = magazineSize;
    }
    
    protected override void SpawnVisualBullet(Vector3 origin, Vector3 direction)
    {
        // Стандартная визуализация пули
        base.SpawnVisualBullet(origin, direction);

        // Эффект попадания
        if (Physics.Raycast(origin, direction, out RaycastHit hit, range) && bulletPrefab != null)
        {
            // Debug.Log("z");
            // Instantiate(bulletPrefab, origin, Quaternion.LookRotation(hit.normal));
        }
    }
}