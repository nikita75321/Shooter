using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class InstanceSoundUI : MonoBehaviour
{
    public static InstanceSoundUI Instance;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;

    [Header("BackMusic")]
    [SerializeField] private AudioSource audioSourceBackMenu;
    [SerializeField] private AudioSource audioSourceBackGame;

    private void OnValidate()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource.clip == null) audioSource.clip = audioClip;
        audioSource.playOnAwake = false;
    }

    private void Awake()
    {
        Instance = this;
        PlayMenuBack();
    }

    public void PlaySound()
    {
        audioSource.Play();
    }

    public void PlayMenuBack()
    {
        audioSourceBackMenu.Play();
        audioSourceBackGame.Stop();
    }

    public void PlayGameBack()
    {
        audioSourceBackGame.Play();
        audioSourceBackMenu.Stop();
    }
}