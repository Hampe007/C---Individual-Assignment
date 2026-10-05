using UnityEngine;

public sealed class SceneMusic : MonoBehaviour
{
    [SerializeField] private AudioClip _music;

    private void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(_music);
    }
}