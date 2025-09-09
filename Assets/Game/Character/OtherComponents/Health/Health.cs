using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum HealthState
{
    live,
    death
}

[RequireComponent(typeof(Armor))]
public class Health : MonoBehaviour
{
    public HealthState state;

    [Header("References")]
    public AidKit aidKit;
    public Image useKitImage;
    public Image useReviveImage;
    [SerializeField] private Player player;
    [SerializeField] private Armor armor;
    [SerializeField] private AudioSource audioSource;

    [Header("Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool canRegenerate = false;
    [SerializeField] private float regenerationRate = 5f;
    [SerializeField] private float regenerationDelay = 3f;
    [SerializeField] private float invulnerabilityTimeAfterHit = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Events")]
    public UnityEvent OnDie;
    public UnityEvent OnTakeDamage;
    public UnityEvent OnHeal;
    public UnityEvent OnRevive;
    public event Action<float> OnHealthChanged;

    [SerializeField] private float currentHealth;
    private float timeSinceLastDamage;
    private bool isInvulnerable;

    public float MaxHealth
    {
        get
        {
            return maxHealth;
        }
        set
        {
            if (value > 0)
                maxHealth = value;
        }
    } 
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsFullHealth => currentHealth >= maxHealth;
    public bool IsDead => state == HealthState.death;

    public float CurrentHealth
    {
        get => currentHealth;
        private set
        {
            if (currentHealth != value)
            {
                currentHealth = Mathf.Clamp(value, 0, maxHealth);
                OnHealthChanged?.Invoke(currentHealth);

                if (currentHealth <= 0 && state == HealthState.live)
                {
                    if (player != null)
                    {
                        Geekplay.Instance.PlayerData.killsCount++;
                    }
                    Die();
                }
            }
        }
    }

    public bool isEnemy = false;
    public bool isAlly = false;

    private void OnValidate()
    {
        if (GetComponent<Enemy>())
        {
            isEnemy = true;
        }
        if (GetComponent<Ally>())
        {
            isAlly = true;
        }

        if (armor == null) armor = GetComponent<Armor>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (!isEnemy)
        {
            if (aidKit == null) aidKit = FindAnyObjectByType<AidKit>(FindObjectsInactive.Include);
            if (player == null) player = FindAnyObjectByType<Player>(FindObjectsInactive.Include);
            // Debug.Log(1);
        }
        else
        {
            aidKit = null;
            player = null;
            // Debug.Log(2);
        }
    }

    private void Start()
    {
        CurrentHealth = maxHealth;
        state = HealthState.live;

        // if (useKitImage != null)
        //     useKitImage.fillAmount = 0;
    }

    private void OnEnable()
    {
        if (!isEnemy && !isAlly)
        OnDie.AddListener(() =>
        {
            Debug.Log(101);
            player.Character.aimingCone.gameObject.SetActive(false);
            player.Character.Health.aidKit.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
        });
    }
    public void OnDisable()
    {
        if (!isEnemy && !isAlly)
        OnDie.RemoveListener(() =>
        {
            // Debug.Log(202);
            player.Character.aimingCone.gameObject.SetActive(false);
            player.Character.Health.aidKit.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
        });
    }

    private void Update()
    {
        if (isInvulnerable)
        {
            timeSinceLastDamage += Time.deltaTime;
            if (timeSinceLastDamage >= invulnerabilityTimeAfterHit)
            {
                isInvulnerable = false;
            }
        }

        if (canRegenerate && !IsFullHealth && !isInvulnerable)
        {
            timeSinceLastDamage += Time.deltaTime;
            if (timeSinceLastDamage >= regenerationDelay)
            {
                CurrentHealth += regenerationRate * Time.deltaTime;
            }
        }
    }

    public void TakeDamage(float damage, float armorPenetration = 0)
    {
        if (damage < 0 || IsDead)
        {
            Debug.Log("IsDead");
            return;
        }
        else
        {
            Debug.Log("Allive");
        }

        timeSinceLastDamage = 0f;
        OnTakeDamage?.Invoke();

        float damageToHealth = damage;

        if (armor != null && armor.CurrentArmor > 0)
        {
            damageToHealth = armor.TakeDamage(damage, armorPenetration);
        }

        if (damageToHealth > 0)
        {
            CurrentHealth -= damageToHealth;
            PlaySound(damageSound);
            Debug.Log($"{gameObject.name} took {damageToHealth} damage. Health: {currentHealth}");
        }

        if (player != null && damage < 100000)
        {
            if (damage > player.maxDamage)
            {
                player.maxDamage = (int)damage;
            }
        }
    }

    public void ChangeHp(float value)
    {
        Debug.Log($"ChangeHp: new Hp - {value}");

        if (value > 0)
        {
            OnTakeDamage?.Invoke();

            PlaySound(damageSound);

            CurrentHealth = value;
        }
        else
        {
            Die();
        }
    }

    public void AddKit()
    {
        aidKit?.AddKitCharge();
    }

    public bool UseKit()
    {
        if (IsDead || IsFullHealth) return false;
        return aidKit.UseKit();
    }
    public void StopUseKit()
    {
        aidKit.StopUseKit();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0) return;

        CurrentHealth += amount;
        PlaySound(healSound);
        Debug.Log($"{gameObject.name} healed {amount} hp. Health: {currentHealth}");
    }

    public void FullHeal()
    {
        if (IsDead) return;

        CurrentHealth = maxHealth;
        PlaySound(healSound);
    }

    public void Revive(float healthPercentage = 0.5f)
    {
        if (!IsDead) return;
        Debug.Log("Revive");

        player.reviveCount++;
        OnRevive?.Invoke();
        state = HealthState.live;
        CurrentHealth = maxHealth * Mathf.Clamp01(healthPercentage);
        isInvulnerable = false;
        timeSinceLastDamage = 0f;
    }

    private void Die()
    {
        if (IsDead) return;

        state = HealthState.death;
        CurrentHealth = 0;
        PlaySound(deathSound);
        OnDie?.Invoke();
        Geekplay.Instance.Save();
        Debug.Log($"{gameObject.name} died!");
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void SetInvulnerable(bool invulnerable, float duration = 0f)
    {
        isInvulnerable = invulnerable;
        if (duration > 0)
        {
            Invoke(nameof(ResetInvulnerability), duration);
        }
    }

    private void ResetInvulnerability()
    {
        isInvulnerable = false;
    }

    public void ModifyMaxHealth(float newMaxHealth, bool healToFull = false)
    {
        if (newMaxHealth <= 0) return;

        float healthPercentage = currentHealth / maxHealth;
        maxHealth = newMaxHealth;

        if (healToFull)
        {
            CurrentHealth = maxHealth;
        }
        else
        {
            CurrentHealth = maxHealth * healthPercentage;
        }
    }
}