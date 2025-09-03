using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health healthComponent;
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, -1f, 0); // Смещение под игроком

    [Header("Settings")]
    [SerializeField] private bool alwaysFaceCamera = true;

    private GameObject healthBarInstance;
    private Image healthFillImage;
    private Camera mainCamera;

    private void OnValidate()
    {
        if (healthComponent == null)
            healthComponent = GetComponent<Health>();
    }
    
    private void Awake()
    {
        mainCamera = Camera.main;
        // CreateHealthBar();
    }

    private void Update()
    {
        if (healthBarInstance == null) return;

        // Обновляем позицию HealthBar
        healthBarInstance.transform.position = transform.position + healthBarOffset;

        // Поворачиваем к камере (если нужно)
        if (alwaysFaceCamera && mainCamera != null)
            healthBarInstance.transform.rotation = mainCamera.transform.rotation;

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthFillImage != null && healthComponent != null)
        {
            float healthPercent = healthComponent.CurrentHealth / healthComponent.MaxHealth;
            healthFillImage.fillAmount = healthPercent;
        }
    }
    
}