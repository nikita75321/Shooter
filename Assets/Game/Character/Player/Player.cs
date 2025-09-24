using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public enum PlayerState
{
    Idle,       // Персонаж стоит
    Walking,    // Персонаж движется
    Shooting,   // Персонаж стреляет
    Reloading,  // Перезарядка оружия
    PickingUp,  // Подбирает предмет
    Revive,     // Поднимаем союзника
    Healing,    // Используем аптечку
    TakeDamage, // Получение урона
    Dead        // Персонаж мертв
}

public class Player : MonoBehaviour
{
    [Header("Referencess")]
    public GameEndCanvas gameEndCanvas;
    public SettingsMenu settings;

    [Header("Heroes")]
    public LayerMask enemyMask;
    [SerializeField] private GameObject[] heroes;

    [Header("State")]
    public PlayerState currentState;

    [Header("Components")]
    public CharacterControllerCustom Controller;
    public Character Character;
    // public Health Health;
    public NoiseEmitter noiseEmitter;
    public VisibilityZone visibilityZone;

    [Header("События состояний")]
    [HideInInspector] public UnityEvent OnIdle;
    [HideInInspector] public UnityEvent OnWalk;
    [HideInInspector] public UnityEvent OnShoot;
    [HideInInspector] public UnityEvent OnReload;
    [HideInInspector] public UnityEvent OnPickUp;
    [HideInInspector] public UnityEvent OnDeath;

    [Header("States")] // Расположены по приоритеты показа анимации
    [field: SerializeField] public bool IsRevive { get; set; }
    [field: SerializeField] public bool IsUseAidKit { get; set; }
    [field: SerializeField] public bool IsPickingUp { get; set; }
    [field: SerializeField] public bool IsReload { get; set; }
    [field: SerializeField] public bool IsShoot { get; set; }
    [field: SerializeField] public bool IsMoving { get; set; }

    [Header("Visibility")]
    [SerializeField] private Material hiddenMaterial; // Материал для скрытых объектов
    public Material HiddenMaterial => hiddenMaterial;
    [SerializeField] private float visionRadius = 10f;
    public float VisionRadius
    {
        get => visionRadius;
        set => visionRadius = Mathf.Clamp(value, 5f, 20f);
    }

    [Header("For save in player stats (InMatch)")]
    public int maxDamage;
    public void SetMaxDamage(float value)
    {
        if (value > maxDamage)
        {
            maxDamage = (int)value;
        }
    }
    public int overallKills;
    public int shotCount;
    public int reviveCount;

    private Coroutine respawnCoroutine;

    private void OnValidate()
    {
        if (Controller == null) Controller = GetComponentInChildren<CharacterControllerCustom>();
        if (Character == null) Character = GetComponentInChildren<Character>();
        // if (Health == null) Health = GetComponentInChildren<Health>();
    }

    private void OnEnable()
    {
        WebSocketBase.Instance.OnMatchEnd += response =>
        {
            WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
            {
                if (respawnCoroutine != null)
                {
                    StopCoroutine(respawnCoroutine);
                    respawnCoroutine = null;
                }
            });
        };
    }
    private void OnDisable()
    {
        WebSocketBase.Instance.OnMatchEnd -= response =>
        {
            WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
            {
                if (respawnCoroutine != null)
                {
                    StopCoroutine(respawnCoroutine);
                    respawnCoroutine = null;
                }
            });
        };
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (GameStateManager.Instance.matchState == MatchState.ready)
            {
                if (settings.gameObject.activeSelf)
                    {
                        GameStateManager.Instance.GameStart?.Invoke();
                        settings.gameObject.SetActive(false);
                        Cursor.lockState = CursorLockMode.Locked;
                    }
                    else
                    {
                        GameStateManager.Instance.GamePause?.Invoke();
                        Cursor.lockState = CursorLockMode.None;
                        settings.gameObject.SetActive(true);
                        IsMoving = false;
                        IsShoot = false;

                    }
            }
        }

        if (GameStateManager.Instance.GameState == GameState.game)
        {
            //Animation state
            UpdateState();

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (IsUseAidKit)
                {
                    Character.CurrentWeapon.StopReload();
                    Character.Health.StopUseKit();
                    Debug.Log("Stop use Aidkit");
                }
                else
                {
                    Character.CurrentWeapon.StopReload();
                    if (Character.Health.UseKit())
                    {
                        Debug.Log("Start use Aidkit");
                    }
                    // else
                    // {
                    //     Character.CurrentWeapon.StopReload();
                    // }
                }
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                Character.Health.StopUseKit();
                if (!IsReload)
                {
                    Debug.Log("reload");
                    Character.CurrentWeapon.StartReload();
                }
                else
                {
                    Debug.Log("stop reload");
                    Character.CurrentWeapon.StopReload();
                }
            }
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (Controller.ally != null)
                {
                    if (IsRevive)
                    {
                        IsRevive = false;
                        Controller.revive.StopRevive();
                    }
                    else
                    {
                        IsRevive = true;
                        Controller.revive.StartRevive();
                    }
                }
            }
        }
    }

    public void Init(HeroData heroData)
    {
        Debug.Log("Init player");
        foreach (var hero in heroes)
        {
            hero.SetActive(false);
        }
        heroes[Geekplay.Instance.PlayerData.currentHero].SetActive(true);

        gameEndCanvas.HideLosePanel();
        gameEndCanvas.HideStatsPanel();
        gameEndCanvas.HideWinPanel();

        var idCurrentHero = Geekplay.Instance.PlayerData.currentHero;
        Controller = heroes[idCurrentHero].GetComponent<CharacterControllerCustom>();
        Character = heroes[idCurrentHero].GetComponent<Character>();
        noiseEmitter = heroes[idCurrentHero].GetComponentInChildren<NoiseEmitter>();
        visibilityZone = heroes[idCurrentHero].GetComponentInChildren<VisibilityZone>();

        heroes[idCurrentHero].GetComponent<HeroDummy>().SelectSkin(Geekplay.Instance.PlayerData.persons[idCurrentHero].currentBody);

        Character.Health.aidKit.player = this;
        Controller.topDownCamera.target = Controller.transform;

        InitStats(heroData);

        Character.ammoInfo.gameObject.SetActive(true);
        Character.Health.aidKit.gameObject.SetActive(true);

        // visibilityZone.
        // OnlineRoom.Instance.StartServerUpdate();

        Cheat.Instance.Init(this);
        StartPlay();
    }

    private void InitStats(HeroData heroData)
    {
        var person = Geekplay.Instance.PlayerData.persons[Geekplay.Instance.PlayerData.currentHero];
        int level = person.level;
        int rank = person.rank;

        // Calculate stats using the same formula as in HeroesCanvas
        float health = heroData.health;
        float armor = heroData.armor;
        float damage = heroData.damage;

        // Apply rank multiplier (15% per rank)
        health *= Mathf.Pow(1.15f, rank);
        armor *= Mathf.Pow(1.15f, rank);
        damage *= Mathf.Pow(1.15f, rank);

        // Apply level multiplier (5% per level)
        health *= Mathf.Pow(1.05f, level);
        armor *= Mathf.Pow(1.05f, level);
        damage *= Mathf.Pow(1.05f, level);

        // Set character stats
        Character.Armor.MaxArmor = armor;
        Character.Health.MaxHealth = health;

        // Set weapon damage (main - full damage, secondary - 1/3)
        Character.MainWeapon.damage = damage;
        Character.SecondaryWeapon.damage = damage / 3f;
    }

    private void StartPlay()
    {
        if (hiddenMaterial == null)
        {
            hiddenMaterial = new Material(Shader.Find("Standard"));
            hiddenMaterial.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }

        // Character.Health.OnDie.AddListener(Die);
        // Character.Health.OnDie.AddListener(() => gameEndCanvas.ShowLosePanel());

        Character.Health.OnTakeDamage.AddListener(TakeDamageAnim);

        SetSecondaryWeapon();
        Controller.mainWeaponModel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Character.Health.OnDie.RemoveListener(Die);
        // Character.Health.OnDie.RemoveListener(() => gameEndCanvas.ShowLosePanel());

        Character.Health.OnTakeDamage.RemoveListener(TakeDamageAnim);
    }

    public void SetMainWeapon()
    {
        Character.InitWeapon(Character.MainWeapon);
        Character.aimingCone.Init(Character.MainWeapon);
        Controller.mainWeaponModel.SetActive(true);
        Controller.secondaryWeaponModel.SetActive(false);
        Controller.animator.runtimeAnimatorController = Controller.animatorMain;
        Controller.animator.SetBool("IsReload", true);
        Character.MainWeapon.StartReload();
        // Debug.Log(Controller.animator.runtimeAnimatorController+ " aaaaaaaaaaaaaaaaaaaaaaaaaaa");
    }
    public void SetSecondaryWeapon()
    {
        Character.InitWeapon(Character.SecondaryWeapon);
        Character.aimingCone.Init(Character.SecondaryWeapon);
        Controller.secondaryWeaponModel.SetActive(true);
        Controller.mainWeaponModel.SetActive(false);
        Controller.animator.runtimeAnimatorController = Controller.animatorSecondary;
    }

    public void Die()
    {
        Controller.animator.SetTrigger("Die");
        Controller.animator.SetLayerWeight(1, 0);
        Controller.animator.SetLayerWeight(2, 0);
        GameStateManager.Instance.GameDeath?.Invoke();
        currentState = PlayerState.Dead;
        
        respawnCoroutine = StartCoroutine(RespawnRoutine());
    }

    [SerializeField] private float respawnDelay = 5f;
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        // выбрать точку спавна (см. SpawnManager ниже)
        Transform spawnPoint = SpawnPoints.Instance.GetRandomSpawnPoint();
        Vector3 spawnPos = spawnPoint.position;
        Quaternion spawnRot = spawnPoint.rotation;

        // телепорт на спавн
        Controller.transform.SetPositionAndRotation(spawnPos, spawnRot);
        Level.Instance.InitCharater();
        GameStateManager.Instance.GameState = GameState.game;

        // восстановление
        Character.Health.FullHeal();
        Character.Health.state = HealthState.live;

        Character.aimingCone.gameObject.SetActive(true);
        Character.Health.aidKit.gameObject.SetActive(true);

        Character.healthBar.gameObject.SetActive(true);
        Character.armorBar.gameObject.SetActive(true);

        NotifyServerAboutRespawn(spawnPos, spawnRot);
    }

    private void NotifyServerAboutRespawn(Vector3 spawnPos, Quaternion spawnRot)
    {
        if (WebSocketBase.Instance == null)
            return;

        var localInfo = OnlineRoom.Instance.GetLocalPlayerInfo();
        if (localInfo != null)
        {
            localInfo.isAlive = true;
            localInfo.hp = Character.Health.CurrentHealth;
            localInfo.armor = Character.Armor.CurrentArmor;
            localInfo.max_hp = Character.Health.MaxHealth;
            localInfo.max_armor = Character.Armor.MaxArmor;
        }

        WebSocketBase.Instance.SendPlayerRespawn(
            Character.Health.CurrentHealth,
            Character.Health.MaxHealth,
            Character.Armor.CurrentArmor,
            Character.Armor.MaxArmor,
            spawnPos,
            spawnRot
        );
    }

    public void TakeDamageAnim()
    {
        int randomAnimation = Random.Range(0, 2); // 0 или 1
        Controller.animator.SetInteger("RandomHit", randomAnimation);
        Controller.animator.SetTrigger("Hit");
        // Controller.animator.SetLayerWeight(1, 0);
        // Controller.animator.SetLayerWeight(2, 0);

        // DOVirtual.DelayedCall(0.5f, () =>
        // {
        //     if (currentState != PlayerState.Dead)
        //     {
        //         Controller.animator.SetLayerWeight(1, 1);
        //         Controller.animator.SetLayerWeight(2, 1);
        //     }
        // });
    }

    #region Upgrades
    [ShowInInspector] public Dictionary<System.Type, Upgrade> upgrades = new(1);

    public void AddUpgrade(Upgrade upgrade)
    {
        if (upgrade == null) return;

        var upgradeType = upgrade.GetType();
        if (upgrades.ContainsKey(upgradeType)) return;

        upgrades.Add(upgradeType, upgrade);
        upgrade.ApplyBoostEffect(this);

        Character.Health.OnDie.AddListener(() =>
        {
            upgrade.DropUpgrade(Controller.transform.position);
            RemoveUpgrade(upgrade);
        });
    }

    public void RemoveUpgrade(Upgrade upgrade)
    {
        if (upgrade == null) return;

        var upgradeType = upgrade.GetType();
        if (!upgrades.ContainsKey(upgradeType)) return;

        upgrade.RemoveBoostEffect(this);
        upgrades.Remove(upgradeType);

        Character.Health.OnDie.RemoveListener(() =>
        {
            upgrade.DropUpgrade(Controller.transform.position);
            RemoveUpgrade(upgrade);
        });
    }
    #endregion






    #region Amimation state
    private void UpdateState()
    {
        if (Character.Health.CurrentHealth <= 0)
        {
            SetState(PlayerState.Dead);
            return;
        }

        if (IsRevive)
        {
            SetState(PlayerState.Revive);
            return;
        }

        if (IsUseAidKit)
        {
            SetState(PlayerState.Healing);
            return;
        }

        if (IsPickingUp)
        {
            SetState(PlayerState.PickingUp);
            return;
        }

        if (IsReload)
        {
            SetState(PlayerState.Reloading);
            return;
        }

        if (IsShoot)
        {
            SetState(PlayerState.Shooting);
            return;
        }

        if (IsMoving)
        {
            SetState(PlayerState.Walking);
            return;
        }

        SetState(PlayerState.Idle);
    }

    private void SetState(PlayerState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case PlayerState.Idle: OnIdle?.Invoke(); break;
            case PlayerState.Walking: OnWalk?.Invoke(); break;
            case PlayerState.Shooting: OnShoot?.Invoke(); break;
            case PlayerState.Reloading: OnReload?.Invoke(); break;
            case PlayerState.PickingUp: OnPickUp?.Invoke(); break;
            case PlayerState.Dead: OnDeath?.Invoke(); break;
        }
    }

    private void UpdateAnimator()
    {
        var animator = Controller.animator;

        animator.SetBool("IsMoving", IsMoving);
        animator.SetBool("IsShooting", IsShoot);
        animator.SetBool("IsReloading", IsReload);
        animator.SetBool("IsHealing", IsUseAidKit);
        animator.SetBool("IsReviving", IsRevive);
        animator.SetBool("IsPickingUp", IsPickingUp);
        animator.SetBool("IsDead", currentState == PlayerState.Dead);
    }
    #endregion
}