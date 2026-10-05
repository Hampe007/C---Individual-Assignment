using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundSet", menuName = "Audio/Sound Set")]
public sealed class SoundSet : ScriptableObject
{
    [SerializeField] private AudioClip[] _clips;

    [Header("Variation")]
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;
    [SerializeField] private Vector2 _pitchRange = new Vector2(0.95f, 1.05f);

    private int _lastClipIndex = -1;

    public float Volume => _volume;

    public float GetPitch()
    {
        return Random.Range(_pitchRange.x, _pitchRange.y);
    }

    public AudioClip GetClip()
    {
        if (_clips == null || _clips.Length == 0)
            return null;

        if (_clips.Length == 1)
            return _clips[0];

        int index;

        do
        {
            index = Random.Range(0, _clips.Length);
        }
        while (index == _lastClipIndex);

        _lastClipIndex = index;
        return _clips[index];
    }
}