using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponList : MonoBehaviour
{
    [Header("References")]
    // [SerializeField] private CharacterControllerCustom controller;
    [SerializeField] private Character character;
    // [SerializeField] private AimingCone aimingCone;

    [SerializeField] private List<Weapon> weapons;
    private int currentWeaponIndex = 0;

    private void OnValidate()
    {
        weapons = GetComponentsInChildren<Weapon>(true).ToList();
    }

    private void Start()
    {
        // aimingCone.Init(weapons[0]);
        // character.InitWeapon(weapons[0]);
    }

    private void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Alpha1))
        // {
        //     SwitchWeapon(0);
        // }
        // else if (Input.GetKeyDown(KeyCode.Alpha2))
        // {
        //     SwitchWeapon(1);
        // }
    }

    public void SwitchWeapon(int newIndex)
    {
        if (newIndex < 0 || newIndex >= weapons.Count || newIndex == currentWeaponIndex)
            return;

        weapons[currentWeaponIndex].gameObject.SetActive(false);
    
        weapons[newIndex].gameObject.SetActive(true);
        currentWeaponIndex = newIndex;

        // aimingCone.Init(weapons[newIndex]);
        // character.InitWeapon(weapons[newIndex]);
        
        Debug.Log($"Switched to weapon: {weapons[currentWeaponIndex].name}");
    }
}