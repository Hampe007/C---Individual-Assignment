using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundSet", menuName = "Audio/Sound Set")]
public sealed class SoundSet : ScriptableObject
{
    [SerializeField] private AudioClip[] clips;

    [Header("Variation")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new(0.95f, 1.05f);

    private int _lastClipIndex = -1;

    public float Volume => volume;

    public float GetPitch()
    {
        return Random.Range(pitchRange.x, pitchRange.y);
    }

    public AudioClip GetClip()
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        if (clips.Length == 1)
        {
            return clips[0];
        }

        int index;

        do
        {
            index = Random.Range(0, clips.Length);
        }
        while (index == _lastClipIndex);

        _lastClipIndex = index;
        return clips[index];
    }
}
