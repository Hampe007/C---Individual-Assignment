using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

public sealed class SwordWeapon : MonoBehaviour
{
    [SerializeField] private HordeManager _hordeManager;
    [SerializeField] private Animator _animator;
    [SerializeField] private SoundSet _swingSFX;

    [Header("Attack")]
    [SerializeField, Min(0.05f)] private float _cooldown = 1f;
    [SerializeField, Min(1)] private int _damage = 20;
    [SerializeField, Min(0.1f)] private float _range = 4f;
    [SerializeField, Min(1)] private int _swordCount = 1;

    private readonly List<int> _targets = new();

    private float _cooldownTimer;

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            return;
        }

        if (TryAttack())
            _cooldownTimer = _cooldown;
    }

    public void ReduceCooldown(float amount)
    {
        if (amount > 0f)
            _cooldown = Mathf.Max(0.05f, _cooldown - amount);
    }

    private bool TryAttack()
    {
        float3 position = transform.position;

        if (!_hordeManager.TryGetClosestEnemy(position, _range, out _, out _))
            return false;

        if (_animator != null)
            _animator.SetTrigger("SwordAttack");

        return true;
    }

    public void ApplyHit()
    {
        float3 position = transform.position;

        _hordeManager.GetClosestEnemies(position, _range, _swordCount, _targets);

        _targets.Sort();

        for (int targetIndex = _targets.Count - 1; targetIndex >= 0; targetIndex--)
            _hordeManager.DamageEnemy(_targets[targetIndex], _damage);
    }

    public void AddDamage(int amount)
    {
        _damage += amount;
    }

    public void AddSword()
    {
        _swordCount++;
    }

    public void PlaySwingSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(_swingSFX, transform.position);
    }

}
