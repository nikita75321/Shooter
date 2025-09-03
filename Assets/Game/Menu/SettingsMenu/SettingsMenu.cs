using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] private Slider volumeSlider;

    [Header("Camera Sensitivity")]
    [SerializeField] private Slider sensitivitySlider;

    [Header("Graphics Settings")]
    [SerializeField] private ToggleGroup graphicsToggleGroup;
    [SerializeField] private Toggle lowGraphicsToggle;
    [SerializeField] private Toggle mediumGraphicsToggle;
    [SerializeField] private Toggle highGraphicsToggle;

    [Header("Buttons")]
    [SerializeField] private Button gameInfo;
    [SerializeField] private Button rules;
    [SerializeField] private GameObject gameInfoPanel, rulesPanel;

    private PlayerData playerData;

    private void Start()
    {
        playerData = Geekplay.Instance.PlayerData;

        // Настройка громкости
        volumeSlider.value = playerData.volume;
        volumeSlider.onValueChanged.AddListener(SetVolume);

        // Настройка чувствительности
        sensitivitySlider.value = playerData.sensativity;
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);

        // Настройка графики
        InitGraphicsSettings();
    }

    private void InitGraphicsSettings()
    {
        lowGraphicsToggle.onValueChanged.AddListener((isOn) => { if(isOn) SetGraphicsQuality(0); });
        mediumGraphicsToggle.onValueChanged.AddListener((isOn) => { if(isOn) SetGraphicsQuality(1); });
        highGraphicsToggle.onValueChanged.AddListener((isOn) => { if(isOn) SetGraphicsQuality(2); });

        switch(playerData.graphics)
        {
            case 0: lowGraphicsToggle.isOn = true; break;
            case 1: mediumGraphicsToggle.isOn = true; break;
            case 2: highGraphicsToggle.isOn = true; break;
        }
    }

    public void SetVolume(float value)
    {
        playerData.volume = value;
        AudioListener.volume = value;
        Geekplay.Instance.Save();
    }

    public void SetSensitivity(float value)
    {
        playerData.sensativity = value;
        Geekplay.Instance.Save();
    }

    public void SetGraphicsQuality(int qualityIndex)
    {
        // Debug.Log(qualityIndex+ " - qualityIndex");
        playerData.graphics = qualityIndex;
        QualitySettings.SetQualityLevel(qualityIndex);
        Geekplay.Instance.Save();
    }
}