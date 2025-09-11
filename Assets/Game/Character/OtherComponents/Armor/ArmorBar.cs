using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArmorBar : MonoBehaviour
{
    [field: SerializeField] public Armor Armor { get; set; }
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text armorbarTxt;

    [SerializeField]
    [Header("Hide if zero value")]
    private bool hideEmpty = false;

    [SerializeField, Min(25f)]
    [Header("Strip change speed")]
    private float changeSpeed = 1000;

    private float currentValue;
    private Camera mainCamera;

    private void OnValidate()
    {
        if (Armor == null)
            Armor = GetComponentInParent<Armor>();

        if (canvas != null)
            canvas.worldCamera = Camera.main;
    }

    private void Start()
    {
        currentValue = Armor.CurrentArmor;
        mainCamera = Camera.main;
        UpdatePositionAndRotation();
        changeSpeed = 1000;
    }

    public void Init()
    {
        armorbarTxt.text = $"{Mathf.RoundToInt(currentValue)} / {Mathf.RoundToInt(Armor.MaxArmor)}";
    }

    private void Update()
    {
        UpdatePositionAndRotation();
        currentValue = Mathf.MoveTowards(currentValue, Armor.CurrentArmor, Time.deltaTime * changeSpeed);

        UpdateFillbar();
        UpdateVisibility();
    }

    private void UpdatePositionAndRotation()
    {
        if (Armor == null || mainCamera == null) return;

        // 1. Получаем ТОЛЬКО УГОЛ ПОВОРОТА камеры (по Y)
        float cameraYRotation = mainCamera.transform.eulerAngles.y;

        // 2. Позиция HP-bar'а (орбита вокруг персонажа)
        Vector3 orbitOffset = Quaternion.Euler(0, cameraYRotation, 0) * new Vector3(0, 0, -0.5f);
        transform.position = Armor.transform.position + orbitOffset;

        // 3. Поворот HP-bar'а (горизонтально, но с учетом поворота камеры)
        transform.rotation = Quaternion.Euler(0, cameraYRotation, 0);
    }

    private void UpdateFillbar()
    {
        if (fillImage == null || Armor == null || armorbarTxt == null) return;

        // Обновляем заполнение полоски здоровья
        float value = Mathf.InverseLerp(0, Armor.MaxArmor, currentValue);
        fillImage.fillAmount = value;

        // Обновляем текст (например: "125/200")
        armorbarTxt.text = $"{Mathf.RoundToInt(currentValue)} / {Mathf.RoundToInt(Armor.MaxArmor)}";
    }

    private void UpdateVisibility()
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
        // Debug.Log("hide armorbar");
        gameObject.SetActive(false);
    }
}
