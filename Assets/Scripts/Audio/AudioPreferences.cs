using System;
using UnityEngine;

public enum AudioVolumeChannel
{
    Master,
    Music,
    Effects
}

public static class AudioPreferences
{
    public static event Action Changed;

    public static float GetVolume(AudioVolumeChannel channel)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat($"Audio.Volume.{channel}", 1f));
    }

    public static void SetVolume(AudioVolumeChannel channel, float value)
    {
        PlayerPrefs.SetFloat($"Audio.Volume.{channel}", Mathf.Clamp01(value));
        AudioListener.volume = GetVolume(AudioVolumeChannel.Master);
        Changed?.Invoke();
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Changed = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        AudioListener.volume = GetVolume(AudioVolumeChannel.Master);
    }
}
