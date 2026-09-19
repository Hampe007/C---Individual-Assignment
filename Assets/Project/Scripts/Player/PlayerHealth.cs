using System;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int _maxHealth = 100;

    [Header("Hit Audio")]
    [SerializeField] private SoundSet _hitSFX;
    [SerializeField, Min(0f)] private float _soundCooldown = 0.3f;

    private int _currentHealth;
    private float _nextSoundTime;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    [ContextMenu("Test Damage 10")]
    private void TestDamage()
    {
        TakeDamage(10);
    }

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || _currentHealth <= 0)
            return;

        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        if (Time.time >= _nextSoundTime && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(_hitSFX, transform.position);
            _nextSoundTime = Time.time + _soundCooldown;
        }

        HealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth == 0)
            Died?.Invoke();
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0)
            return;

        _maxHealth += amount;
        HealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
}
