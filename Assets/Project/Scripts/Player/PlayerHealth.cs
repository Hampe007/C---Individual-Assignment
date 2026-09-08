using System;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int _maxHealth = 100;

    private int _currentHealth;

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
        HealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth == 0)
            Died?.Invoke();
    }
}