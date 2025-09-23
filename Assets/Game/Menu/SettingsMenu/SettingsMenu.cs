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

    public void Init()
    {
        // playerData = Geekplay.Instance.PlayerData;
    }

    private void Start()
    {
        playerData = Geekplay.Instance.PlayerData;
        Debug.Log("playerData.masterVolume "+playerData.masterVolume);

        // Настройка громкости
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        masterSlider.value = playerData.masterVolume;
        musicSlider.value = playerData.musicVolume;
        sfxSlider.value = playerData.sfxVolume;

        // Настройка чувствительности
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        sensitivitySlider.value = playerData.sensativity;

        // Настройка графики
        InitGraphicsSettings();
    }
    private void OnDestroy()
    {
        masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);

        if (lowGraphicsToggle == null) return;
        if (mediumGraphicsToggle == null) return;
        if (highGraphicsToggle == null) return;

        lowGraphicsToggle.onValueChanged.RemoveAllListeners();
        mediumGraphicsToggle.onValueChanged.RemoveAllListeners();
        highGraphicsToggle.onValueChanged.RemoveAllListeners();
    }

    private void InitGraphicsSettings()
    {
        if (lowGraphicsToggle == null) return;
        if (mediumGraphicsToggle == null) return;
        if (highGraphicsToggle == null) return;

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

    public void LeaveMatch()
    {
        Level.Instance.currentLevel.player.Character.Health.Die();
        Rating.Instance.SpendRating(5);
        WebSocketBase.Instance.UpdatePlayerStatsAfterBattle(
            playerId: Geekplay.Instance.PlayerData.id,
            ratingChange: -5
        );
        WebSocketBase.Instance.LeaveRoom();
        Level.Instance.LevelFinish();
    }
    public void CloseInGame()
    {
        GameStateManager.Instance.GameStart?.Invoke();
        Level.Instance.LevelFinish();
        Cursor.lockState = CursorLockMode.Locked;
    }
}