using System;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int _maxHealth = 100;
    [SerializeField] private PlayerProgression _progression;

    [Header("Damage Protection")]
    [SerializeField, Min(0f)] private float _damageProtectionDuration = 0.75f;

    [Header("Hit Audio")]
    [SerializeField] private SoundSet _hitSFX;
    [SerializeField, Min(0f)] private float _soundCooldown = 0.3f;

    private int _currentHealth;
    private float _nextDamageTime;
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
        if (_progression == null)
            _progression = GetComponent<PlayerProgression>();
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || _currentHealth <= 0 || Time.time < _nextDamageTime)
            return;

        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        _nextDamageTime = Time.time + _damageProtectionDuration;
        if (_progression != null)
            _progression.LoseXPFromDamage();
        if (Time.time >= _nextSoundTime && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(_hitSFX, transform.position);
            _nextSoundTime = Time.time + _soundCooldown;
        }

        HealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth == 0)
        {
            if (_progression != null)
                _progression.SaveHighScore();

            Died?.Invoke();
        }
    }

    /// <summary>Damage over time does not consume hit protection or penalize XP.</summary>
    public void TakePoisonDamage(int damage)
    {
        if (damage <= 0 || _currentHealth <= 0) return;
        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        HealthChanged?.Invoke(_currentHealth, _maxHealth);
        if (_currentHealth == 0)
        {
            if (_progression != null) _progression.SaveHighScore();
            Died?.Invoke();
        }
    }

    public bool TryHeal(int amount)
    {
        if (amount <= 0 || _currentHealth <= 0 || _currentHealth >= _maxHealth)
            return false;

        _currentHealth += Mathf.Min(amount, _maxHealth - _currentHealth);
        HealthChanged?.Invoke(_currentHealth, _maxHealth);
        return true;
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0)
            return;

        _maxHealth += amount;
        HealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
}
