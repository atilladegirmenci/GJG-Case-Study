using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip backgroundMusic;
    public AudioClip blastSound;

    private bool _isMuted = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PlayMusic();
    }

    private void PlayMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlayBlastSound()
    {
        if (sfxSource != null && blastSound != null)
        {
            sfxSource.PlayOneShot(blastSound);
        }
    }

    public void ToggleMute()
    {
        _isMuted = !_isMuted;

        musicSource.mute = _isMuted;
        sfxSource.mute = _isMuted;

        Debug.Log($"Audio Muted: {_isMuted}");
    }
}