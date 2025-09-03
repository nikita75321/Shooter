using System.Collections.Generic;
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
    Dead        // Персонаж мертв
}

public class Player : MonoBehaviour
{
    [Header("Referencess")]
    public GameEndCanvas gameEndCanvas;

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

    [Header("States")]
    [field: SerializeField] public bool IsPickingUp { get; set; }
    [field: SerializeField] public bool IsReload { get; set; }
    [field: SerializeField] public bool IsShoot { get; set; }
    [field: SerializeField] public bool IsMoving { get; set; }
    [field: SerializeField] public bool IsUseAidKit { get; set; }
    [field: SerializeField] public bool IsRevive { get; set; }

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

    private void OnValidate()
    {
        if (Controller == null) Controller = GetComponentInChildren<CharacterControllerCustom>();
        if (Character == null) Character = GetComponentInChildren<Character>();
        // if (Health == null) Health = GetComponentInChildren<Health>();
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
        if (GameStateManager.Instance.GameState == GameState.game)
        {
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
                    if (Character.Health.UseKit())
                    {
                        Debug.Log("Start use Aidkit");
                    }
                    else
                    {
                        Character.CurrentWeapon.StopReload();
                    }
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
        
        // Geekplay.Instance.PlayerData.currentHeroHeadSkin = Geekplay.Instance.PlayerData.persons[idCurrentHero].currentBody;
        // Geekplay.Instance.PlayerData.currentHeroBodySkin = Geekplay.Instance.PlayerData.persons[idCurrentHero].currentBody;
        // Geekplay.Instance.Save();

        Character.Health.aidKit.player = this;
        Controller.topDownCamera.target = Controller.transform;

        InitStats(heroData);

        Character.ammoInfo.gameObject.SetActive(true);
        Character.Health.aidKit.gameObject.SetActive(true);

        // visibilityZone.
        OnlineRoom.Instance.StartServerUpdate();

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

        // Apply rank multiplier (50% per rank)
        health *= Mathf.Pow(1.5f, rank);
        armor *= Mathf.Pow(1.5f, rank);
        damage *= Mathf.Pow(1.5f, rank);

        // Apply level multiplier (10% per level)
        health *= Mathf.Pow(1.1f, level);
        armor *= Mathf.Pow(1.1f, level);
        damage *= Mathf.Pow(1.1f, level);

        // Set character stats
        Character.Armor.MaxArmor = armor;
        Character.Health.MaxHealth = health;

        // Set weapon damage (main - full damage, secondary - 1/3)
        Character.MainWeapon.damage = damage;
        Character.SecondaryWeapon.damage = damage / 3f;

        // Restore current health and armor values
        Character.Armor.MaxArmor = armor;
        Character.Health.MaxHealth = health;
    }

    private void StartPlay()
    {
        if (hiddenMaterial == null)
        {
            hiddenMaterial = new Material(Shader.Find("Standard"));
            hiddenMaterial.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }

        Character.Health.OnDie.AddListener(Die);
        Character.Health.OnDie.AddListener(() => gameEndCanvas.ShowLosePanel());

        Character.Health.OnTakeDamage.AddListener(TakeDamageAnim);

        SetSecondaryWeapon();
        Controller.mainWeaponModel.SetActive(false);
    }

    private void OnDestroy()
    {
        Character.Health.OnDie.RemoveListener(Die);
        Character.Health.OnDie.RemoveListener(() => gameEndCanvas.ShowLosePanel());

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
        GameStateManager.Instance.GameDeath();
    }

    public void TakeDamageAnim()
    {
        int randomAnimation = Random.Range(0, 2); // 0 или 1
        Controller.animator.SetInteger("RandomHit", randomAnimation);
        Controller.animator.SetTrigger("Hit");
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
}