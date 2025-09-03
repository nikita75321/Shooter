using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssaultRifle : Weapon
{
    public override void OnValidate()
    {
        base.OnValidate();

        ammoAmountToPickUp = 60;

        ammoOverall = 0;
        magazineSize = 24;
        reloadTime = 1.5f;
        fireRate = 0.2f;
        range = 6f;
        noiseShoot = 1.5f;

        currentAimAngle = 15;
        maxAimAngle = 45;
    }

    protected override void Awake()
    {
        base.Awake();

        ammoAmountToPickUp = 60;

        ammoOverall = 0;
        magazineSize = 24;
        reloadTime = 1.5f;
        fireRate = 0.2f;
        range = 6f;
        noiseShoot = 1.5f;

        currentAimAngle = 15;
        maxAimAngle = 45;

        weaponClass = WeaponClass.main;
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
