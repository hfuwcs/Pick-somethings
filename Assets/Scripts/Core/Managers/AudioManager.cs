using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)]
    public float musicVolume = 0.3f;
    [Header("UI Sounds")]
    public AudioClip clickSound;
    public AudioClip hoverSound;

    [Header("Physics Sounds")]
    public AudioClip snapSound;
    public AudioClip unsnapSound;
    public AudioClip wireConnectSound;
    public AudioClip switchFlipSound;

    private AudioSource _sfxSource;
    private AudioSource _musicSource;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f;

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = true;
        _musicSource.loop = true;
        _musicSource.spatialBlend = 0f;
        _musicSource.volume = musicVolume;
        _musicSource.clip = backgroundMusic;
    }
    private void Start()
    {
        if (_musicSource.clip != null)
        {
            _musicSource.Play();
        }
    }
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            _sfxSource.PlayOneShot(clip, volume);
        }
    }
    public void ToggleMusic(bool isOn)
    {
        _musicSource.mute = !isOn;
    }
    // Hàm tiện ích
    public void PlaySnap() => PlaySound(snapSound);
    public void PlayUnsnap() => PlaySound(unsnapSound);
    public void PlayWire() => PlaySound(wireConnectSound);
    public void PlaySwitch() => PlaySound(switchFlipSound);
}