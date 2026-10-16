using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

public sealed class SwordWeapon : MonoBehaviour
{
    [SerializeField] private HordeManager hordeManager;
    [SerializeField] private Animator animator;
    [SerializeField] private SoundSet swingSFX;
    [Header("Attack")]
    [SerializeField, Min(0.05f)] private float cooldown = 1f;
    [SerializeField, Min(1)] private int damage = 20;
    [SerializeField, Min(0.1f)] private float range = 4f;
    [SerializeField, Min(1)] private int targetsPerSwing = 1;
    private readonly List<int> targets = new();
    private float cooldownTimer;
    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        if (TryAttack())
        {
            cooldownTimer = cooldown;
        }
    }

    public void ReduceCooldown(float amount)
    {
        if (amount > 0f)
        {
            cooldown = Mathf.Max(0.05f, cooldown - amount);
        }
    }

    private bool TryAttack()
    {
        float3 position = transform.position;
        if (!hordeManager.TryGetClosestEnemy(position, range, out _, out _))
        {
            return false;
        }

        if (animator != null)
        {
            animator.SetTrigger("SwordAttack");
        }

        return true;
    }

    public void ApplyHit()
    {
        float3 position = transform.position;
        hordeManager.GetClosestEnemies(position, range, targetsPerSwing, targets);
        targets.Sort();
        for (int targetIndex = targets.Count - 1; targetIndex >= 0; targetIndex--)
        {
            hordeManager.DamageEnemy(targets[targetIndex], damage);
        }
    }

    public void AddDamage(int amount)
    {
        damage += amount;
    }

    public void IncreaseTargetsPerSwing()
    {
        targetsPerSwing++;
    }

    public void PlaySwingSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(swingSFX, transform.position);
        }
    }
}