using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixerGroup _musicGroup;
    [SerializeField] private AudioMixerGroup _sfxGroup;

    [Header("Music")]
    [SerializeField] private AudioSource _musicSource;

    [Header("SFX")]
    [SerializeField, Min(1)] private int _maxSFXSources = 64;

    private ObjectPool<AudioSource> _sfxPool;
    private readonly List<AudioSource> _activeSFX = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_musicSource == null || _musicGroup == null || _sfxGroup == null)
        {
            Debug.LogError("AudioManager is missing required references.");
            enabled = false;
            return;
        }

        _musicSource.outputAudioMixerGroup = _musicGroup;

        _sfxPool = new ObjectPool<AudioSource>(
            CreateSFXSource,
            GetSFXSource,
            ReleaseSFXSource,
            DestroySFXSource,
            true,
            16,
            _maxSFXSources);
    }

    private void Update()
    {
        for (int i = _activeSFX.Count - 1; i >= 0; i--)
        {
            AudioSource source = _activeSFX[i];

            if (source.isPlaying)
                continue;

            _activeSFX.RemoveAt(i);
            _sfxPool.Release(source);
        }
    }

    public void PlaySFX(SoundSet sound)
    {
        PlaySFX(sound, transform.position, 0f);
    }

    public void PlaySFX(SoundSet sound, Vector3 position)
    {
        PlaySFX(sound, position, 1f);
    }

    private void PlaySFX(SoundSet sound, Vector3 position, float spatialBlend)
    {
        if (sound == null)
            return;

        AudioClip clip = sound.GetClip();

        if (clip == null)
            return;

        AudioSource source = _sfxPool.Get();

        source.transform.position = position;
        source.spatialBlend = spatialBlend;
        source.clip = clip;
        source.volume = sound.Volume;
        source.pitch = sound.GetPitch();

        source.Play();
        _activeSFX.Add(source);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || _musicSource.clip == clip)
            return;

        _musicSource.clip = clip;
        _musicSource.loop = true;
        _musicSource.Play();
    }

    public void StopMusic()
    {
        _musicSource.Stop();
        _musicSource.clip = null;
    }

    private AudioSource CreateSFXSource()
    {
        GameObject gameObject = new("SFXSource");
        gameObject.transform.SetParent(transform);

        AudioSource source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.outputAudioMixerGroup = _sfxGroup;

        return source;
    }

    private static void GetSFXSource(AudioSource source)
    {
        source.gameObject.SetActive(true);
    }

    private static void ReleaseSFXSource(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
    }

    private static void DestroySFXSource(AudioSource source)
    {
        if (source != null)
            Destroy(source.gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        _sfxPool?.Clear();
        _activeSFX.Clear();
    }
}