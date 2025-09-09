using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Armor))]
public class Enemy : MonoBehaviour
{
    [Header("Referencess")]
    [SerializeField] private LevelPrefab level;
    [SerializeField] private Player player;

    [Header("Main component")]
    public Health Health;
    public Armor Armor;
    public Animator animator;
    [SerializeField] private Collider col;
    [SerializeField] private CharacterController controller;

    [Header("Stat bars")]
    [SerializeField] private Healthbar healthbar;
    [SerializeField] private ArmorBar armorBar;

    [Header("Hero info")]
    [SerializeField] private HeroDummy heroDummy;
    [SerializeField] private GameObject[] heroes;

    [Header("Visual")]
    [SerializeField] private Renderer[] traces;
    [SerializeField] private bool isAudible;
    [SerializeField] private float noizeVolume;
    [SerializeField] private GameObject[] tracesGo;
    [SerializeField] private GameObject[] tracesTarget;

    private struct State
    {
        public Vector3 position;
        public Quaternion rotation;
        public float time;
    }

    private Queue<State> stateBuffer = new Queue<State>();
    private float interpolationDelay = 0.1f; // задержка 100мс
    private float extrapolationLimit = 0.2f; // максимум 200мс "угадывания"
    private Vector3 lastVelocity = Vector3.zero;

    private void OnValidate()
    {
        if (Health == null) Health = GetComponentInChildren<Health>();
        // if (animator == null) animator = GetComponentInChildren<Animator>();
        if (col == null) col = GetComponentInChildren<Collider>();
        if (controller == null) controller = GetComponentInChildren<CharacterController>();
        if (healthbar == null) healthbar = GetComponentInChildren<Healthbar>(false);
        if (armorBar == null) armorBar = GetComponentInChildren<ArmorBar>();
        if (level == null) level = GetComponentInParent<LevelPrefab>();
        if (level != null && player == null) player = level.player;
        if (heroDummy == null) heroDummy = GetComponent<HeroDummy>();
    }

    private void Update()
    {
        // твой код с traces
        for (int i = 0; i < tracesGo.Length; i++)
        {
            tracesGo[i].transform.position = tracesTarget[i].transform.position;
        }

        if (stateBuffer.Count == 0) return;

        float renderTime = Time.time - interpolationDelay;

        // Найти две ближайшие точки для интерполяции
        State prev = stateBuffer.Peek();
        State next = prev;
        foreach (var s in stateBuffer)
        {
            if (s.time <= renderTime)
                prev = s;
            if (s.time > renderTime)
            {
                next = s;
                break;
            }
        }

        if (next.time > prev.time)
        {
            // Интерполяция
            float t = Mathf.InverseLerp(prev.time, next.time, renderTime);
            Vector3 newPos = Vector3.Lerp(prev.position, next.position, t);
            transform.position = newPos;
            transform.rotation = Quaternion.Slerp(prev.rotation, next.rotation, t);

            lastVelocity = (next.position - prev.position) / (next.time - prev.time);
        }
        else
        {
            // Экстраполяция
            float delta = Time.time - prev.time;
            if (delta < extrapolationLimit)
            {
                Vector3 newPos = prev.position + lastVelocity * delta;
                transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * 5f);
            }
        }

        // Чистим устаревшие состояния
        while (stateBuffer.Count > 0 && stateBuffer.Peek().time < renderTime - 1f)
            stateBuffer.Dequeue();

        // // Лерп к сетевой позиции
        // if (targetPosition != Vector3.zero) // можно проверить что инициализировано
        // {
        //     Debug.Log($"Бежим к targetPosition - {targetPosition}");
        //     transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed / 3);
        //     transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed / 3);
        // }
        // else
        // {
        //     Debug.Log("Стоим ждем-с");
        // }
    }

    private void Start()
    {
        Health.OnDie.AddListener(HideUI);
        Health.OnDie.AddListener(healthbar.Hide);
        Health.OnDie.AddListener(armorBar.Hide);

        Health.OnTakeDamage.AddListener(TakeDamageAnim);
        HideTraces();
    }

    public void InitHero(PlayerInGameInfo playerInfo)
    {
        Debug.Log($"idHero-{playerInfo.hero_id}, idSkin-{playerInfo.hero_skin}");
        if (level == null) level = GetComponentInParent<LevelPrefab>();
        if (player == null) player = level.player;

        foreach (var hero in heroes)
        {
            hero.SetActive(false);
        }
        heroes[playerInfo.hero_id].SetActive(true);

        heroDummy = heroes[playerInfo.hero_id].GetComponent<HeroDummy>();
        heroDummy.SelectSkin(playerInfo.hero_skin);

        InitStats(playerInfo);
        InitHpBar();
        InitArmorBar();
    }

    private void InitStats(PlayerInGameInfo playerInfo)
    {
        float health = Level.Instance.heroDatas[playerInfo.hero_id].health;
        float armor = Level.Instance.heroDatas[playerInfo.hero_id].armor;
        // float damage = Level.Instance.heroDatas[playerInfo.hero_id].damage;

        // Apply rank multiplier (50% per rank)
        health *= Mathf.Pow(1.5f, playerInfo.hero_rank);
        armor *= Mathf.Pow(1.5f, playerInfo.hero_rank);
        // damage *= Mathf.Pow(1.5f, playerInfo.hero_rank);

        // Apply level multiplier (10% per level)
        health *= Mathf.Pow(1.1f, playerInfo.hero_level);
        armor *= Mathf.Pow(1.1f, playerInfo.hero_level);
        // damage *= Mathf.Pow(1.1f, playerInfo.hero_level);

        // Set character stats
        Armor.MaxArmor = armor;
        Health.MaxHealth = health;

        // Set weapon damage (main - full damage, secondary - 1/3)
        // MainWeapon.damage = damage;
        // SecondaryWeapon.damage = damage / 3f;
    }
    public void InitHpBar()
    {
        healthbar.Init();
    }
    public void InitArmorBar()
    {
        armorBar.Init();
    }

    public void Die()
    {
        Debug.Log("die");
        animator.SetTrigger("Die");
        animator.SetLayerWeight(1, 0);
        animator.SetLayerWeight(2, 0);
        col.enabled = false;
        controller.enabled = false;

        healthbar.Hide();
        armorBar.Hide();

        Geekplay.Instance.PlayerData.killCaseValue++;
        Geekplay.Instance.PlayerData.killOverral++;
        Geekplay.Instance.Save();
    }

    public void TakeDamageAnim()
    {
        Debug.Log("TakeDamageAnim - enemy");
        int randomAnimation = Random.Range(0, 2); // 0 или 1
        animator.SetInteger("RandomHit", randomAnimation);
        animator.SetTrigger("Hit");
    }

    private void HideUI()
    {

    }

    public void ShowTraces()
    {
        foreach (var trace in traces)
        {
            trace.enabled = true;
        }
        // Debug.Log("Show trace");
    }
    public void HideTraces()
    {
        foreach (var trace in traces)
        {
            trace.enabled = false;
        }
        // Debug.Log("Hide trace");
    }

    private void OnDestroy()
    {
        Health.OnDie.RemoveListener(HideUI);
        Health.OnDie.RemoveListener(healthbar.Hide);
        Health.OnDie.RemoveListener(armorBar.Hide);

        Health.OnTakeDamage.RemoveListener(TakeDamageAnim);
    }

    public void UpdateNoizeState(float value)
    {
        var distance = Vector3.Distance(player.Character.transform.position, transform.position);
        // Debug.Log($"distance - {distance}, value - {value}");
        if (distance <= value)
        {
            ShowTraces();
            animator.SetBool("IsMoving", true);
        }
        else
        {
            HideTraces();
            animator.SetBool("IsMoving", false);
        }
    }
    #region OnlineHandler
    public void SetNetworkState(Vector3 position, Quaternion rotation)
    {
        float now = Time.time;
        stateBuffer.Enqueue(new State
        {
            position = position,
            rotation = rotation,
            time = now
        });

        // Чистим слишком старые данные (на всякий случай)
        while (stateBuffer.Count > 20)
            stateBuffer.Dequeue();
    }
    #endregion
}