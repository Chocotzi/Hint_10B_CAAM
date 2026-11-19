using UnityEngine;
using UnityEngine.Audio;

// AudioManager centralizado (patron Singleton).
// Persiste entre escenas (DontDestroyOnLoad) para que la musica no se corte
// al reiniciar el nivel y para que los SFX no se corten al destruir objetos.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Fuentes (cada una enrutada a su grupo del AudioMixer)")]
    public AudioSource musicSource;   // Grupo Music
    public AudioSource sfxSource;     // Grupo SFX

    [Header("Mixer (parametros expuestos)")]
    public AudioMixer mainMixer;
    public string masterVolumeParam = "MasterVolume";
    public string musicVolumeParam = "MusicVolume";
    public string sfxVolumeParam = "SFXVolume";

    [Header("BGM")]
    public AudioClip bgmNivel01;

    [Header("SFX")]
    public AudioClip sfxJump;
    public AudioClip sfxLand;
    public AudioClip sfxCoin;
    public AudioClip sfxDamage;
    public AudioClip sfxEnemyDefeat;
    public AudioClip sfxUISelect;

    [Header("Variacion de pitch (evita fatiga auditiva)")]
    public float minPitch = 0.92f;
    public float maxPitch = 1.08f;

    private void Awake()
    {
        // Singleton: si ya existe uno (p. ej. al recargar la escena) se destruye el duplicado
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayMusic(bgmNivel01);
    }

    // Reproduce un efecto con PlayOneShot: varios SFX pueden sonar a la vez
    // y no se cortan aunque el objeto que lo origino se desactive o destruya.
    public void PlaySFX(AudioClip clip, bool randomPitch = true)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = randomPitch ? Random.Range(minPitch, maxPitch) : 1f;
        sfxSource.PlayOneShot(clip);
    }

    // Reproduce la musica de fondo en bucle (no la reinicia si ya esta sonando).
    public void PlayMusic(AudioClip music)
    {
        if (music == null || musicSource == null) return;
        if (musicSource.clip == music && musicSource.isPlaying) return;

        musicSource.clip = music;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    // Volumenes lineales 0..1 -> decibeles del Mixer (para sliders de menu de pausa)
    public void SetMasterVolume(float value) { SetMixerVolume(masterVolumeParam, value); }
    public void SetMusicVolume(float value) { SetMixerVolume(musicVolumeParam, value); }
    public void SetSFXVolume(float value) { SetMixerVolume(sfxVolumeParam, value); }

    private void SetMixerVolume(string param, float linear)
    {
        if (mainMixer == null) return;
        float dB = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
        mainMixer.SetFloat(param, dB);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
