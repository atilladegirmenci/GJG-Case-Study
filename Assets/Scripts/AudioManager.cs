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
    public AudioClip dropSound;

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

    public void PlayBlastSound(int blastBlockCount)
    {
        if (sfxSource == null || blastSound == null) return;

        sfxSource.pitch = calculatePitch(blastBlockCount);
        sfxSource.PlayOneShot(blastSound);

    }
    public void PlayDropSound(int fallingBlockCount)
    {
        if (dropSound == null || sfxSource == null || _isMuted) return;


        sfxSource.pitch = calculatePitch(fallingBlockCount);
        sfxSource.PlayOneShot(dropSound);
    }
    private float calculatePitch(int blockCount)
    {
        float basePitch = 1.1f;
        float pitchReduction = blockCount * 0.05f;

        float targetPitch = Mathf.Clamp(basePitch - pitchReduction, 0.6f, 1.5f);
        targetPitch += Random.Range(-0.02f, 0.02f);
        return targetPitch;
    }

    public void ToggleMute()
    {
        _isMuted = !_isMuted;

        musicSource.mute = _isMuted;
        sfxSource.mute = _isMuted;

        Debug.Log($"Audio Muted: {_isMuted}");
    }
}