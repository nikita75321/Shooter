using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

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
        masterSlider.value = playerData.masterVolume;
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.value = playerData.musicVolume;
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.value = playerData.sfxVolume;
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // Настройка чувствительности
        sensitivitySlider.value = playerData.sensativity;
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);

        // Настройка графики
        InitGraphicsSettings();
    }
    private void OnDestroy()
    {
        masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);

        lowGraphicsToggle.onValueChanged.RemoveAllListeners();
        mediumGraphicsToggle.onValueChanged.RemoveAllListeners();
        highGraphicsToggle.onValueChanged.RemoveAllListeners();
    }

    private void InitGraphicsSettings()
    {
        lowGraphicsToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetGraphicsQuality(0); });
        mediumGraphicsToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetGraphicsQuality(1); });
        highGraphicsToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetGraphicsQuality(2); });

        switch (playerData.graphics)
        {
            case 0: lowGraphicsToggle.isOn = true; break;
            case 1: mediumGraphicsToggle.isOn = true; break;
            case 2: highGraphicsToggle.isOn = true; break;
        }
    }

    public void SetMasterVolume(float value)
    {
        playerData.masterVolume = value;
        AudioManager.Instance.SetMasterVolume(value);
        Geekplay.Instance.Save();
    }
    public void SetMusicVolume(float value)
    {
        playerData.musicVolume = value;
        AudioManager.Instance.SetMusicVolume(value);
        Geekplay.Instance.Save();
    }
    public void SetSFXVolume(float value)
    {
        playerData.sfxVolume = value;
        AudioManager.Instance.SetSFXVolume(value);
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