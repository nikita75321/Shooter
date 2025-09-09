using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class InstanceSoundUI : MonoBehaviour
{
    public static InstanceSoundUI Instance;
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip openChestSound;
    [SerializeField] private AudioClip dropItemSound;

    [Header("BackMusic")]
    [SerializeField] private AudioSource audioSourceBackMenu;
    [SerializeField] private AudioSource audioSourceBackGame;

    private void OnValidate()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource.clip == null) audioSource.clip = clickSound;
        audioSource.playOnAwake = false;
    }

    private void Awake()
    {
        Instance = this;
        PlayMenuBack();
    }

    public void PlayClickSound()
    {
        audioSource.clip = clickSound;
        audioSource.Play();
    }
    public void PlayOpenChestSound()
    {
        audioSource.clip = openChestSound;
        audioSource.Play();
    }
    public void PlayGetItemSound()
    {
        audioSource.clip = dropItemSound;
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