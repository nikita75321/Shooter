using DG.Tweening;
using UnityEngine;

public class Ally : MonoBehaviour
{
    [Header("Referencess")]
    [SerializeField] private LevelPrefab level;
    [SerializeField] private Player player;

    [Header("Main component")]
    public Health health;
    public Animator animator;
    [SerializeField] private Collider col;
    public CharacterController controller;

    [Header("Stat bars")]
    public Healthbar healthbar;
    public ArmorBar armorBar;

    [Header("Visual")]
    [SerializeField] private GameObject[] tracesGo;
    [SerializeField] private GameObject[] tracesTarget;

    [Header("Revive")]
    public Collider reviveCollider;

    private void OnValidate()
    {
        if (health == null) health = GetComponentInChildren<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (col == null) col = GetComponentInChildren<Collider>();
        if (controller == null) controller = GetComponentInChildren<CharacterController>();
        if (healthbar == null) healthbar = GetComponentInChildren<Healthbar>();
        if (armorBar == null) armorBar = GetComponentInChildren<ArmorBar>();
        if (level == null) level = GetComponentInParent<LevelPrefab>();
        if (level != null && player == null) player = level.player;
    }

    private void Update()
    {
        for (int i = 0; i < tracesGo.Length; i++)
        {
            tracesGo[i].transform.position = tracesTarget[i].transform.position;
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            Die();
        }
    }
    private void Start()
    {
        reviveCollider.enabled = false;

        health.OnDie.AddListener(HideUI);
        health.OnDie.AddListener(healthbar.Hide);
        health.OnDie.AddListener(armorBar.Hide);

        health.OnTakeDamage.AddListener(TakeDamageAnim);
    }

    public void Die()
    {
        Debug.Log("die");
        animator.SetTrigger("Die");
        animator.SetLayerWeight(1, 0);
        animator.SetLayerWeight(2, 0);

        controller.enabled = false;
        reviveCollider.enabled = true;

        healthbar.Hide();
        armorBar.Hide();

        health.TakeDamage(1000000, 0);
        Geekplay.Instance.Save();
    }

    public void TakeDamageAnim()
    {
        int randomAnimation = Random.Range(0, 2); // 0 или 1
        animator.SetInteger("RandomHit", randomAnimation);
        animator.SetTrigger("Hit");
    }

    private void HideUI()
    {

    }

    private void OnDestroy()
    {
        health.OnDie.RemoveListener(HideUI);
        health.OnDie.RemoveListener(healthbar.Hide);
        health.OnDie.RemoveListener(armorBar.Hide);

        health.OnTakeDamage.RemoveListener(TakeDamageAnim);
    }
}
