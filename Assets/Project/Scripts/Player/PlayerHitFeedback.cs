using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public sealed class PlayerHitFeedback : MonoBehaviour
{
    [SerializeField] private SoundSet _hitSFX;
    [SerializeField, Min(0f)] private float _soundCooldown = 0.3f;

    private PlayerHealth _playerHealth;
    private float _nextSoundTime;

    private void Awake()
    {
        _playerHealth = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        _playerHealth.Damaged += HandleDamage;
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.Damaged -= HandleDamage;
    }

    private void HandleDamage(int damage)
    {
        if (Time.time < _nextSoundTime)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySFX(_hitSFX, transform.position);
        _nextSoundTime = Time.time + _soundCooldown;
    }
}