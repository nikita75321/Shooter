using UnityEngine;

public class Armor : MonoBehaviour
{
    // [Header("Referencess")]
    public float maxArmor = 100f;
    public float MaxArmor
    {
        get => maxArmor;
        set
        {
            if (value > 0)
                maxArmor = value;
        }
    }
    public float initMaxArmor;
    [SerializeField] private float currentArmor;
    [SerializeField] private float initArmor;
    public float CurrentArmor => currentArmor;

    private void OnValidate()
    {

    }

    private void Start()
    {
        initMaxArmor = maxArmor;
        currentArmor = initArmor;
        if (currentArmor > maxArmor)
            currentArmor = maxArmor;
    }

    // public float TakeDamage(float damage, float armorPenetration)
    // {
    //     float remainingDamage = damage;
    //     float armorDamage = 0;
    //     if (currentArmor > 0)
    //     {
    //         if (armorPenetration > 0)
    //         {
    //             armorDamage = Mathf.Min(remainingDamage *( 1 + armorPenetration), currentArmor);
    //         }
    //         else
    //         {
    //             armorDamage = Mathf.Min(remainingDamage, currentArmor);
    //         }
    //         Debug.Log(armorDamage +" - armor damage");
    //         currentArmor -= armorDamage;
    //         remainingDamage -= armorDamage;
    //     }

    //     Debug.Log($"{gameObject.name} took {damage} damage. Armor left: {currentArmor} Final damage:{remainingDamage}");
    //     return remainingDamage;
    // }    

    public float TakeDamage(float damage, float armorPenetration)
    {
        float remainingDamage = damage * ( 1 + armorPenetration);
        if (currentArmor > 0)
        {
            var armorDamage = Mathf.Min(remainingDamage , currentArmor);

            Debug.Log(armorDamage +" - armor damage");
            currentArmor -= armorDamage;
            remainingDamage -= armorDamage;
        }

        Debug.Log($"{gameObject.name} took {damage} damage. Armor left: {currentArmor} Final damage:{remainingDamage}");
        return remainingDamage;
    }
    public void ChangeArmor(float value)
    {
        currentArmor = value;
    }

    public void ArmorIncrease(float value)
    {
        if (value < 0) return;

        currentArmor += value;

        if (currentArmor > MaxArmor)
        {
            currentArmor = MaxArmor;
        }
    }
}
