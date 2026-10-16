using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;

    [Header("SFX")]
    [SerializeField, Min(1)] private int maxSfxSources = 64;

    private ObjectPool<AudioSource> _sfxPool;
    private readonly List<AudioSource> _activeSfx = new();
    private readonly Dictionary<AudioSource, float> _sfxBaseVolumes = new();
    private float _musicBaseVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Persistent services are grouped in the authored scene, then become roots at runtime.
        transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);

        if (musicSource == null || musicGroup == null || sfxGroup == null)
        {
            Debug.LogError("AudioManager is missing required references.");
            enabled = false;
            return;
        }

        musicSource.outputAudioMixerGroup = musicGroup;
        _musicBaseVolume = musicSource.volume;
        AudioPreferences.Changed += ApplyVolumes;
        ApplyVolumes();

        _sfxPool = new ObjectPool<AudioSource>(CreateSfxSource, GetSfxSource, ReleaseSfxSource, DestroySfxSource, true, 16, maxSfxSources);
    }

    private void Update()
    {
        for (int i = _activeSfx.Count - 1; i >= 0; i--)
        {
            AudioSource source = _activeSfx[i];

            if (source.isPlaying || (AudioListener.pause && !source.ignoreListenerPause))
            {
                continue;
            }

            _activeSfx.RemoveAt(i);
            _sfxBaseVolumes.Remove(source);
            _sfxPool.Release(source);
        }
    }

    public void PlaySfx(SoundSet sound)
    {
        PlaySfx(sound, transform.position, 0f);
    }

    public void PlaySfx(SoundSet sound, Vector3 position)
    {
        PlaySfx(sound, position, 1f);
    }

    private void PlaySfx(SoundSet sound, Vector3 position, float spatialBlend)
    {
        if (sound == null)
        {
            return;
        }

        AudioClip clip = sound.GetClip();

        if (clip == null)
        {
            return;
        }

        AudioSource source = _sfxPool.Get();

        source.transform.position = position;
        source.spatialBlend = spatialBlend;
        source.outputAudioMixerGroup = sound.OutputMixerGroup != null ? sound.OutputMixerGroup : sfxGroup;
        source.ignoreListenerPause = sound.IgnoreListenerPause;
        source.clip = clip;
        _sfxBaseVolumes[source] = sound.Volume;
        source.volume = sound.Volume * AudioPreferences.GetVolume(AudioVolumeChannel.Effects);
        source.pitch = sound.GetPitch();

        source.Play();
        _activeSfx.Add(source);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource.clip == clip)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
        musicSource.clip = null;
    }

    private AudioSource CreateSfxSource()
    {
        GameObject sourceObject = new("SFXSource");
        sourceObject.transform.SetParent(transform);

        AudioSource source = sourceObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.outputAudioMixerGroup = sfxGroup;

        return source;
    }

    private static void GetSfxSource(AudioSource source)
    {
        source.gameObject.SetActive(true);
    }

    private static void ReleaseSfxSource(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
    }

    private static void DestroySfxSource(AudioSource source)
    {
        if (source != null)
        {
            Destroy(source.gameObject);
        }
    }

    private void OnDestroy()
    {
        AudioPreferences.Changed -= ApplyVolumes;
        if (Instance == this)
        {
            Instance = null;
        }

        _sfxPool?.Clear();
        _activeSfx.Clear();
        _sfxBaseVolumes.Clear();
    }

    private void ApplyVolumes()
    {
        musicSource.volume = _musicBaseVolume * AudioPreferences.GetVolume(AudioVolumeChannel.Music);
        foreach (AudioSource source in _activeSfx)
        {
            source.volume = _sfxBaseVolumes[source] * AudioPreferences.GetVolume(AudioVolumeChannel.Effects);
        }
    }
}
