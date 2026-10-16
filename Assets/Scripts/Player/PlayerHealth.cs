using System;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private PlayerProgression progression;
    [Header("Damage Protection")]
    [SerializeField, Min(0f)] private float damageProtectionDuration = 0.75f;
    [Header("Hit Audio")]
    [SerializeField] private SoundSet hitSFX;
    [SerializeField, Min(0f)] private float soundCooldown = 0.3f;
    private int currentHealth;
    private float nextDamageTime;
    private float nextSoundTime;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public event Action<int, int> HealthChanged;
    public event Action Died;
    [ContextMenu("Test Damage 10")]
    private void TestDamage()
    {
        TakeDamage(10);
    }

    private void Awake()
    {
        if (progression == null)
        {
            progression = GetComponent<PlayerProgression>();
        }

        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0 || Time.time < nextDamageTime)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        nextDamageTime = Time.time + damageProtectionDuration;
        if (progression != null)
        {
            progression.LoseXPFromDamage();
        }

        if (Time.time >= nextSoundTime && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(hitSFX, transform.position);
            nextSoundTime = Time.time + soundCooldown;
        }

        HealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth == 0)
        {
            if (progression != null)
            {
                progression.SaveHighScore();
            }

            Died?.Invoke();
        }
    }

    /// <summary>Damage over time does not consume hit protection or penalize XP.</summary>
    public void TakePoisonDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        HealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth == 0)
        {
            if (progression != null)
            {
                progression.SaveHighScore();
            }

            Died?.Invoke();
        }
    }

    public bool TryHeal(int amount)
    {
        if (amount <= 0 || currentHealth <= 0 || currentHealth >= maxHealth)
        {
            return false;
        }

        currentHealth += Mathf.Min(amount, maxHealth - currentHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
        return true;
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        maxHealth += amount;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }
}