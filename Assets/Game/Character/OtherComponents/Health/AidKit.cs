using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AidKit : MonoBehaviour
{
    [Header("References")]
    public Player player;
    [SerializeField] private Health health;
    [SerializeField] private Image loadImage;
    
    [SerializeField] private TMP_Text kitTxt;

    [Header("Settings")]
    public float timeToAddKit = 1f;
    public float healDuration = 5f;

    [Header("Event")]
    public Action OnHealBegin;
    public Action OnHealComplete;
    public Action OnHealInterrupted;

    private Tween fillTween;
    public bool isFilling = false;
    private float currentFillTime;

    private void OnValidate()
    {
        if (player == null) player = FindAnyObjectByType<Player>();
        // if (health == null && player != null) health = player.Character.Health;
    }

    private void Start()
    {
        GameStateManager.Instance.GameStart += StartFilling;

        health = player.Character.Health;
        health.useKitImage.gameObject.SetActive(false);

        timeToAddKit = 25f;
        player.Character.currentHealthKits = 0;

        loadImage.fillAmount = 0f;
        UpdateKitText();
        // StartFilling();

        OnHealBegin += () =>
        {
            player.Controller.MoveSpeed = player.Controller.MaxSpeed / 2;
        };
        OnHealComplete += () =>
        {
            player.Controller.MoveSpeed = player.Controller.MaxSpeed;
        };
    }

    public void StartFilling()
    {
        if (CanStartFilling())
        {
            Debug.Log("StartFilling");
            isFilling = true;
            currentFillTime = 0f;

            // Отменяем предыдущий твин если был
            fillTween?.Kill();

            // Создаем новый твин для плавного заполнения
            fillTween = DOVirtual.Float(0, 1, timeToAddKit, value =>
            {
                loadImage.fillAmount = value;
                currentFillTime = value * timeToAddKit;
            })
            .SetEase(Ease.Linear)
            .OnComplete(AddKitCharge);
        }
        else
        {
            Debug.Log("Cant start");
        }
    }

    public void StopFilling()
    {
        if (isFilling)
        {
            isFilling = false;
            fillTween?.Kill();
            loadImage.fillAmount = 0f;
            currentFillTime = 0f;
        }
    }

    private bool CanStartFilling()
    {
        return !isFilling &&
               player.Character.currentHealthKits < player.Character.maxHealthKits &&
               GameStateManager.Instance.GameState == GameState.game;
    }

    public void AddKitCharge()
    {
        StopFilling();
        player.Character.currentHealthKits++;
        isFilling = false;
        UpdateKitText();

        if (player.Character.currentHealthKits < player.Character.maxHealthKits)
        {
            StartFilling();
        }
        else
        {
            loadImage.fillAmount = 0f;
        }
    }

    private Tween healTween;
    public bool UseKit()
    {
        Debug.Log("UseKitAid");

        if (player.Character.currentHealthKits <= 0 || player.IsUseAidKit)
            return false;

        player.IsUseAidKit = true;
        health.useKitImage.gameObject.SetActive(true);
        OnHealBegin?.Invoke();

        // Запускаем анимацию использования
        health.useKitImage.fillAmount = 0f;
        healTween = DOVirtual.Float(0, 1, healDuration, value =>
        {
            health.useKitImage.fillAmount = value;
        })
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            OnHealComplete?.Invoke();
            health.Heal(health.MaxHealth);
            health.useKitImage.fillAmount = 0f;
            player.IsUseAidKit = false;
            player.Character.currentHealthKits--;
            UpdateKitText();
            StartFilling();
        });

        return true;
    }
    public void StopUseKit()
    {
        if (!player.IsUseAidKit) return;
        
        healTween?.Kill();
        OnHealInterrupted?.Invoke();

        player.IsUseAidKit = false;
        player.Controller.MoveSpeed = player.Controller.MaxSpeed;
        
        // Сброс анимации
        health.useKitImage.fillAmount = 0f;
    }

    public void UpdateKitText()
    {
        kitTxt.text = $"{player.Character.currentHealthKits}/{player.Character.maxHealthKits}";
    }

    private void OnDisable()
    {
        fillTween?.Kill();
    }

    private void OnDestroy()
    {
        GameStateManager.Instance.GameStart -= StartFilling;
        
        OnHealBegin += () =>
        {
            player.Controller.MoveSpeed = player.Controller.MaxSpeed / 2;
        };
        OnHealComplete += () =>
        {
            player.Controller.MoveSpeed = player.Controller.MaxSpeed;
        };
    }
}