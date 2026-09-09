using Unity.Mathematics;
using UnityEngine;

public sealed class SwordWeapon : WeaponBase
{
    [SerializeField] private HordeManager _hordeManager;
    [SerializeField] private Animator _animator;

    [Header("Attack")]
    [SerializeField, Min(1)] private int _damage = 20;
    [SerializeField, Min(0.1f)] private float _range = 4f;

    protected override bool TryAttack()
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

        if (!_hordeManager.TryGetClosestEnemy(position, _range, out int enemyIndex, out _))
            return;

        _hordeManager.DamageEnemy(enemyIndex, _damage);
    }
    
}