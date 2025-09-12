using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private Transform noiseIndicator;

    [Header("Noise Settings")]
    [SerializeField] private float minNoiseRadius = 0f;
    [SerializeField] private float maxNoiseRadius = 3f;
    [SerializeField] private float noiseFadeSpeed = 2f;

    public float currentNoiseRadius;
    
    private float targetNoiseRadius;
    private float noiseDelayTimer = 0f;
    private bool isWaitingToReset = false;

    private void Start()
    {
        if (player == null) player = Level.Instance.currentLevel.player;
    }

    private void Update()
    {
        // Определяем целевой радиус шума
        if (player.IsMoving && !player.IsShoot)
        {
            // Нормализуем скорость от 0 до 1 и применяем к радиусу шума
            float speedNormalized = Mathf.Clamp01(player.Controller.currentSpeed / player.Controller.MaxSpeed) / 2;
            targetNoiseRadius = Mathf.Lerp(minNoiseRadius, maxNoiseRadius, speedNormalized);
            ResetNoiseDelay();

            if (player.IsReload)
            {
                targetNoiseRadius = 1;
                ResetNoiseDelay(); // Сброс задержки, если был выстрел
            }
        }

        // if (player.IsReload)
        // {
        //     targetNoiseRadius = 1;
        //     ResetNoiseDelay(); // Сброс задержки, если был выстрел
        //     if (player.IsMoving)
        //     {
        //         // Нормализуем скорость от 0 до 1 и применяем к радиусу шума
        //         float speedNormalized = Mathf.Clamp01(player.Controller.currentSpeed / player.Controller.MaxSpeed) / 3;
        //         targetNoiseRadius = Mathf.Lerp(minNoiseRadius, maxNoiseRadius, speedNormalized);
        //         ResetNoiseDelay();
        //     }
        // else if (player.IsMoving)
        //     {
        //         // Нормализуем скорость от 0 до 1 и применяем к радиусу шума
        //         float speedNormalized = Mathf.Clamp01(player.Controller.currentSpeed / player.Controller.MaxSpeed) / 3;
        //         targetNoiseRadius = Mathf.Lerp(minNoiseRadius, maxNoiseRadius, speedNormalized);
        //         ResetNoiseDelay();

        //         // targetNoiseRadius = player.IsMoving ? player.Controller.MoveSpeed : 0;
        //         // ResetNoiseDelay(); // Сброс задержки, если игрок движется
        //     }

        else if (!isWaitingToReset)
        {
            // Запускаем таймер задержки, только если ещё не ждём
            noiseDelayTimer += Time.deltaTime;

            if (noiseDelayTimer >= 0.1f)
            {
                targetNoiseRadius = 0f;
                isWaitingToReset = false;
            }
        }

        // Плавное изменение радиуса
        currentNoiseRadius = Mathf.Lerp(currentNoiseRadius, targetNoiseRadius, 0.1f);
        // currentNoiseRadius = targetNoiseRadius;

        // Обновляем визуализацию
        if (noiseIndicator != null)
        {
            noiseIndicator.localScale = Vector3.one * currentNoiseRadius * 2;
        }
    }

    // Сброс таймера задержки (при движении или выстреле)
    private void ResetNoiseDelay()
    {
        noiseDelayTimer = 0.1f;
        isWaitingToReset = false;
    }

    // Вызывается при выстреле
    public void TriggerShootNoise()
    {
        targetNoiseRadius = player.Character.CurrentWeapon.noiseShoot;
        ResetNoiseDelay();
    }
}