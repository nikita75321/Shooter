using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    [field: SerializeField] public Health Health { get; set; }
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text hpbarTxt;

    [SerializeField]
    [Header("Скрывать при нулевом здоровье")]
    private bool hideEmpty = false;

    [SerializeField]
    [Header("Длительность анимации")]
    private float animationDuration = 0.3f;

    private Tween healthTween;
    private Camera mainCamera;

    private void OnValidate()
    {
        if (Health == null)
            Health = GetComponentInParent<Health>();

        if (canvas != null)
            canvas.worldCamera = Camera.main;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        // UpdatePositionAndRotation();

        // Инициализируем начальное значение
        fillImage.fillAmount = Health.CurrentHealth / Health.MaxHealth;
        hpbarTxt.text = $"{Mathf.RoundToInt(Health.CurrentHealth)} / {Mathf.RoundToInt(Health.MaxHealth)}";
    }

    public void Init()
    {
        hpbarTxt.text = $"{Mathf.RoundToInt(Health.CurrentHealth)} / {Mathf.RoundToInt(Health.MaxHealth)}";
    }

    private void Update()
    {
        UpdatePositionAndRotation();
        UpdateVisibility();
    }

    private void OnEnable()
    {
        if (Health != null)
        {
            Health.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (Health != null)
        {
            Health.OnHealthChanged -= HandleHealthChanged;
        }
        healthTween?.Kill();
    }

    private void HandleHealthChanged(float newHealth)
    {
        // Debug.Log($"HandleHealthChanged (slider) - {newHealth}");
        // Отменяем предыдущую анимацию, если она была
        healthTween?.Kill();

        // Анимация заполнения healthbar
        healthTween = DOVirtual.Float(
            fillImage.fillAmount,
            newHealth / Health.MaxHealth,
            animationDuration,
            value =>
            {
                fillImage.fillAmount = value;
                hpbarTxt.text = $"{Mathf.RoundToInt(value * Health.MaxHealth)} / {Mathf.RoundToInt(Health.MaxHealth)}";
            })
            .SetEase(Ease.OutQuad);
    }

    private void UpdatePositionAndRotation()
    {
        if (Health == null || mainCamera == null) return;

        float cameraYRotation = mainCamera.transform.eulerAngles.y;
        Vector3 orbitOffset = Quaternion.Euler(0, cameraYRotation, 0) * new Vector3(0, 0, -0.3f);
        transform.position = Health.transform.position + orbitOffset;
        transform.rotation = Quaternion.Euler(0, cameraYRotation, 0);
    }

    public void UpdateVisibility()
    {
        if (canvas == null) return;

        bool shouldShow = !(hideEmpty && Mathf.Approximately(fillImage.fillAmount, 0));
        if (canvas.gameObject.activeSelf != shouldShow)
        {
            canvas.gameObject.SetActive(shouldShow);
        }
    }

    public void Hide()
    {
        // Debug.Log("hide hpbar");
        gameObject.SetActive(false);
    }
}