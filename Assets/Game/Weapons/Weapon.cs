
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum WeaponClass
{
    main,
    secondary
}
public abstract class Weapon : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Player player;

    [Header("AmmoUI")]
    [SerializeField] private AmmoInfo ammoInfo;

    [Header("Base Settings")]
    [SerializeField] private LayerMask enemyLayers;
    public Animator animator;
    public float damage = 10;
    public float armorPenetration;
    [SerializeField] protected float fireRate = 0.5f;
    public float range = 5f;
    public int magazineSize = 30;
    public int maxMagazineSize;
    public int ammoOverall;
    public WeaponClass weaponClass;
    public int ammoAmountToPickUp = 30;
    public float noiseShoot = 1f;

    private float fireRateCooldown = 0.5f;
    public float reloadTime = 2f;
    [SerializeField] private Image reloadImage;

    public int bulletsPerShot = 3;
    [SerializeField] protected ParticleSystem muzzleFlash;
    [SerializeField] protected AudioClip shootSound;
    [SerializeField] protected GameObject bulletPrefab;

    [Header("Aiming Settings")]
    public Transform muzzle;
    public float minAimAngle = 3f;
    public float maxAimAngle = 25f;
    [SerializeField] protected float aimTransitionSpeed = 5f;

    [Header("Init Stats")]
    public float InitMinAngle;
    public float InitMaxAngle;
    public float InitMaxRange;
    public float InitMaxDamage;
    public float InitReloadTime;
    public float InitNoiseShoot;

    [Header("Stats")]
    public int currentAmmo;
    [SerializeField] protected bool isMoving;
    public float currentAimAngle;
    [SerializeField] protected AudioSource audioSource;

    public UnityEvent OnStartMoving;
    public UnityEvent OnStopMoving;
    public UnityEvent<float> OnAimAngleChanged;

    [Header("DOTween Settings")]
    [SerializeField] private float reloadShakeDuration = 0.5f; // Длительность "тряски" при перезарядке
    [SerializeField] private Ease reloadEase = Ease.OutBack;   // Стиль анимации

    [Header("Bullet Settings")]
    [SerializeField] private float bulletVisualSpeed = 50f; // Скорость визуальной пули

    private Tween fireRateTween;
    private Tween reloadTween; // Для контроля перезарядки

    public virtual void OnValidate()
    {
        if (player == null) player = GetComponentInParent<Player>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (ammoInfo == null) ammoInfo = FindAnyObjectByType<AmmoInfo>(FindObjectsInactive.Include);
    }

    protected virtual void Awake()
    {
        currentAimAngle = maxAimAngle;
        if (reloadImage != null)
            reloadImage.fillAmount = 0;

        fireRateCooldown = fireRate;
        InitMinAngle = minAimAngle;
        InitMaxAngle = maxAimAngle;
        InitMaxRange = range;
        InitMaxDamage = damage;
        maxMagazineSize = magazineSize;
        InitReloadTime = reloadTime;
        InitNoiseShoot = noiseShoot;
    }

    private void Start()
    {
        //Временно, надо переделать 
        if (player != null)
        //Временно, надо переделать
        {
            enemyLayers = player.enemyMask;
        }
        else
        {
            enabled = false;
        }
    }

    public void Update()
    {
        if (GameStateManager.Instance.GameState == GameState.game)
        {
            UpdateAimAngle();
        }
    }

    protected void UpdateAimAngle()
    {
        // Debug.Log("currentAimAngle "+currentAimAngle);
        isMoving = player.IsMoving;
        float targetAngle = isMoving ? maxAimAngle : minAimAngle;
        currentAimAngle = Mathf.Lerp(currentAimAngle, targetAngle, Time.deltaTime * aimTransitionSpeed);
        OnAimAngleChanged?.Invoke(currentAimAngle);
    }

    public void SetMovingState(bool moving)
    {
        if (moving == isMoving) return;

        isMoving = moving;

        if (moving)
            OnStartMoving?.Invoke();
        else
            OnStopMoving?.Invoke();
    }

    public void TryShoot()
    {
        // Если перезарядка, нет патронов или уже стреляем - выходим
        if (player.IsReload || currentAmmo <= 0 || Time.time < fireRateCooldown)
            return;

        // Устанавливаем время следующего возможного выстрела
        fireRateCooldown = Time.time + fireRate;

        // Начинаем стрельбу
        // player.Controller.animator.SetTrigger("StartShoot");
        player.Controller.animator.SetBool("IsShoot", true);

        if (weaponClass == WeaponClass.secondary)
        {
            // player.Controller.animator.speed = 1;
            // Debug.Log(1);
        }
        else
        {
            // Debug.Log(2);
        }
        FireBullet();

        // Запускаем цикл стрельбы с интервалом fireRate
        fireRateTween = DOVirtual.DelayedCall(fireRate, () =>
        {
            player.IsShoot = false;

        }, false);
    }

    private void FireBullet()
    {
        if (currentAmmo <= 0)
        {
            StartReload();
            return;
        }

        currentAmmo--;
        ammoInfo.UpdateUI();

        player.noiseEmitter.TriggerShootNoise();
        player.shotCount++;

        // --- считаем направление выстрела ---
        Vector3 shootOrigin = player.Character.transform.position + Vector3.up * 1f;
        Vector3 baseDirection = player.Character.transform.forward;
        float maxSpreadRadians = currentAimAngle * Mathf.Deg2Rad;
        float randomSpread = Mathf.Tan(Random.Range(-maxSpreadRadians / 3, maxSpreadRadians / 3));
        Vector3 spreadDirection = (baseDirection + player.Character.transform.right * randomSpread).normalized;

        // 🚀 отправляем на сервер
        WebSocketBase.Instance.SendDealDamage(shootOrigin, spreadDirection, (int)damage);

        // --- визуальный фидбек ---
        // SpawnVisualBullet(shootOrigin, spreadDirection);
        // PlayMuzzleFlash();
        // PlayShootSound();
        PlayShotEffects(shootOrigin, spreadDirection);

        if (currentAmmo <= 0)
        {
            StartReload();
        }
    }

    public void PlayShotEffects(Vector3 origin, Vector3 direction)
    {
        Vector3 shotDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : (muzzle != null ? muzzle.forward : transform.forward);

        SpawnVisualBullet(origin, shotDirection);
        PlayMuzzleFlash();
        PlayShootSound();
    }

    public void PlayShotEffects(Vector3 direction)
    {
        Vector3 origin = muzzle != null ? muzzle.position : transform.position;
        PlayShotEffects(origin, direction);
    }

    protected virtual void SpawnVisualBullet(Vector3 origin, Vector3 direction)
    {
        // Debug.Log("SpawnVisualBullet");
        // GameObject bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
        Vector3 spawnPosition = muzzle != null ? muzzle.position : origin;
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        bullet.transform.forward = direction;

        // Двигаем пулю визуально
        bullet.GetComponent<Rigidbody>().velocity = direction * bulletVisualSpeed;

        // Уничтожаем через время
        Destroy(bullet, 3f);
    }

    public void StartReload()
    {
        // Проверяем возможность перезарядки
        if (player.IsReload || currentAmmo == magazineSize || (currentAmmo > 0 && ammoOverall == 0))
        {
            // Debug.Log("Reload not need");
            return;
        }

        if (player.Character.CurrentWeapon == player.Character.MainWeapon)
        {
            // Debug.Log(player.Character.MainWeapon.ammoOverall+" - 1");
            if (player.Character.MainWeapon.ammoOverall == 0 && player.Character.MainWeapon.currentAmmo == 0)
            {
                // Debug.Log("Меняем на пистолет");
                player.Character.CurrentWeapon.StopReload();
                player.SetSecondaryWeapon();
                return;
            }
        }
        Debug.Log("Starting reload...");
        // Debug.Log(player.Character.MainWeapon.ammoOverall+" - 2");

        if (animator != null)
        {
            animator.SetBool("IsShoot", false);
            animator.speed = reloadTime;
        }

        player.IsReload = true;
        player.IsShoot = false;

        player.Controller.animator.SetBool("IsReload", true);
        player.Controller.MoveSpeed = player.Controller.MaxSpeed / 2;

        // Отменяем предыдущие твины
        reloadTween?.Kill();
        fireRateTween?.Kill();

        // Сбрасываем и запускаем анимацию заполнения
        if (reloadImage != null)
        {
            reloadImage.fillAmount = 0f;
            reloadImage.DOFillAmount(1f, reloadTime)
                .SetEase(Ease.Linear)
                .OnComplete(() => reloadImage.fillAmount = 0f);
        }

        // Основная перезарядка
        reloadTween = DOVirtual.DelayedCall(reloadTime, () =>
        {
            // Рассчитываем сколько патронов можно добавить
            int neededAmmo = magazineSize - currentAmmo;
            int ammoToAdd = Mathf.Min(neededAmmo, ammoOverall);

            // Добавляем патроны
            currentAmmo += ammoToAdd;
            ammoOverall -= ammoToAdd;

            player.IsReload = false;
            player.Controller.animator.SetBool("IsReload", false);
            player.Controller.MoveSpeed = player.Controller.MaxSpeed;

            ammoInfo.UpdateUI(); // Обновляем UI

            Debug.Log($"Reload complete! Ammo: {currentAmmo}/{magazineSize} | Overall: {ammoOverall} {name}");
        });
    }

    public void StopReload()
    {
        // Если перезарядка не идет, ничего не делаем
        if (!player.IsReload) return;

        Debug.Log("Reload stopped!");

        // Останавливаем все анимации перезарядки
        reloadTween?.Kill();

        // Сбрасываем заполнение изображения перезарядки
        if (reloadImage != null)
        {
            reloadImage.DOKill(); // Останавливаем анимацию заполнения
            reloadImage.fillAmount = 0f;
        }

        // Сбрасываем состояние

        player.Controller.animator.SetBool("IsReload", false);
        player.IsReload = false;
        player.Controller.MoveSpeed = player.Controller.MaxSpeed;

        ammoInfo.UpdateUI(); // Обновляем UI
    }

    protected virtual void PlayMuzzleFlash()
    {
        if (muzzleFlash != null)
            muzzleFlash.Play();
    }

    protected virtual void PlayShootSound()
    {
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    public float GetCurrentAimAngle() => currentAimAngle;

    public void AddAmmo()
    {
        // Debug.Log("AddAmmo ?????");
        // StopReload();
        ammoOverall += ammoAmountToPickUp;
        ammoInfo.UpdateUI();

        Debug.Log(player.Character.MainWeapon.ammoOverall+" - 1");
        // Debug.Log(player.Character);
        // Debug.Log(player);

        if (currentAmmo == 0)
        {
            StartReload();
        }
    }
    public void AddAmmo(int value)
    {
        // StopReload();
        ammoOverall += value;
        ammoInfo.UpdateUI();

        if (currentAmmo == 0)
            StartReload();
    }

    #region Fun
    
    private Tween _fireRateResetTween;

    public void HellFireRate(float _hellFireRate, float _hellFireDuration)
    {
        _fireRateResetTween?.Kill();
        float originalFireRate = fireRate;
        fireRate = _hellFireRate;

        _fireRateResetTween = DOVirtual.DelayedCall(_hellFireDuration, () =>
        {
            fireRate = originalFireRate;
            _fireRateResetTween = null;
        });
    }
    public void SnailFireRate(float _snailFireRate, float _hellFireDuration)
    {
        _fireRateResetTween?.Kill();
        float originalFireRate = fireRate;
        fireRate = _snailFireRate;

        _fireRateResetTween = DOVirtual.DelayedCall(_hellFireDuration, () =>
        {
            fireRate = originalFireRate;
            _fireRateResetTween = null;
        });
    }
#endregion
}