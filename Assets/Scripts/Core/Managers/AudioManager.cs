using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("UI Sounds")]
    public AudioClip clickSound;
    public AudioClip hoverSound;

    [Header("Physics Sounds")]
    public AudioClip snapSound;
    public AudioClip unsnapSound;
    public AudioClip wireConnectSound;
    public AudioClip switchFlipSound;

    private AudioSource _sfxSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f;
    }

    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            _sfxSource.PlayOneShot(clip, volume);
        }
    }

    // Hàm tiện ích
    public void PlaySnap() => PlaySound(snapSound);
    public void PlayUnsnap() => PlaySound(unsnapSound);
    public void PlayWire() => PlaySound(wireConnectSound);
    public void PlaySwitch() => PlaySound(switchFlipSound);
}