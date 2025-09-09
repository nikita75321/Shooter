using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioMixer))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioMixer audioMixer;
    private const string MASTER_VOLUME = "Master";
    private const string MUSIC_VOLUME = "Music";
    private const string SFX_VOLUME = "SFX";

    private void Awake()
    {
        Instance = this;
    }

    private void SetVolume(string parameterName, float volume)
    {
        // Преобразуем 0-100 в 0-1
        float linear = Mathf.Clamp01(volume / 100f);

        // Если линейное значение 0, чтобы избежать Log10(0), ставим -80 дБ
        float volumeDB = (linear > 0f) ? Mathf.Log10(linear) * 20f : -80f;

        audioMixer.SetFloat(parameterName, volumeDB);
    }

    public void SetMasterVolume(float volume)
    {
        SetVolume(MASTER_VOLUME, volume);
    }

    public void SetMusicVolume(float volume)
    {
        SetVolume(MUSIC_VOLUME, volume);
    }

    public void SetSFXVolume(float volume)
    {
        SetVolume(SFX_VOLUME, volume);
    }
    
    public float GetMasterVolume()
    {
        return GetVolume(MASTER_VOLUME);
    }

    public float GetMusicVolume()
    {
        return GetVolume(MUSIC_VOLUME);
    }

    public float GetSFXVolume()
    {
        return GetVolume(SFX_VOLUME);
    }

    private float GetVolume(string parameterName)
    {
        if (audioMixer.GetFloat(parameterName, out float volumeDB))
        {
            float linear = Mathf.Pow(10, volumeDB / 20f);
            return linear * 100f; // возвращаем 0–100
        }
        return 0f;
    }
}
