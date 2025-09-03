using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject model;

    private float damage;
    private Weapon ownerWeapon;
    private Rigidbody rb;
    private bool isHit;

    private Tween tween;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, lifetime);

        Physics.IgnoreLayerCollision(gameObject.layer, gameObject.layer, true);
    }

    private void Update()
    {
        transform.Translate(transform.forward * Time.deltaTime);
    }

    public void Initialize(float bulletDamage, Weapon weapon = null)
    {
        damage = bulletDamage;
        ownerWeapon = weapon;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Если уже попали или это другая пуля - игнорируем
        if (isHit || other.gameObject.layer != LayerMask.NameToLayer("Bullet") || other.gameObject.layer != LayerMask.NameToLayer("Ally"))
        {
            // Debug.Log(1);

            model.SetActive(false);
            // rb.velocity = Vector3.zero;
            // rb.isKinematic = true;
            // rb.constraints = RigidbodyConstraints.FreezeAll;
            tween = DOVirtual.DelayedCall(1, () =>
            {
                if (gameObject != null)
                    Destroy(gameObject);
            });
            return;
        }
        else
        {
            // Debug.Log(2);
        }

        // Проверяем, что объект находится на слое Enemy
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Health damageable = other.GetComponent<Health>();
            if (damageable != null)
            {
                // damageable.TakeDamage(damage);
                isHit = true;

                // Создаем эффект попадания
                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                }

                // Немедленно уничтожаем пулю

            }
            
            tween?.Kill();
            Destroy(gameObject);
        }
    }
}