using Unity.Mathematics;
using UnityEngine;

/// <summary>Automatically fires spectral bolts once purchased in the skill tree.</summary>
public sealed class SoulBoltWeapon : MonoBehaviour
{
    [SerializeField] private HordeManager _hordeManager;
    [SerializeField] private CombatProjectileSystem _projectiles;
    [SerializeField, Min(0.1f)] private float _range = 14f;
    [SerializeField, Min(0.1f)] private float _cooldown = 1.7f;
    [SerializeField, Min(0.1f)] private float _speed = 15f;
    [SerializeField, Min(0.1f)] private float _lifetime = 1.4f;
    [SerializeField, Min(1)] private int _damage = 15;
    [SerializeField, Range(0f, 45f)] private float _spreadDegrees = 14f;

    private bool _unlocked;
    private int _boltCount = 1;
    private float _timer;

    public bool IsUnlocked => _unlocked;
    public void Unlock() { _unlocked = true; _timer = 0f; }
    public void AddDamage(int amount) { _damage += Mathf.Max(0, amount); }
    public void AddBolt() { _boltCount = Mathf.Min(5, _boltCount + 1); }

    private void Update()
    {
        if (!_unlocked || _hordeManager == null || _projectiles == null || Time.timeScale <= 0f)
            return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        Vector3 origin = transform.position + Vector3.up * 1.1f;
        if (!_hordeManager.TryGetClosestEnemy((float3)transform.position, _range, out _, out float3 target))
            return;

        Vector3 direction = new Vector3(target.x - origin.x, 0f, target.z - origin.z).normalized;
        for (int i = 0; i < _boltCount; i++)
        {
            // Keep the first bolt aimed at the target as extra side bolts are added.
            float angle = i == 0 ? 0f : ((i + 1) / 2) * (i % 2 == 1 ? -_spreadDegrees : _spreadDegrees);
            _projectiles.SpawnSoulBolt(origin, Quaternion.Euler(0f, angle, 0f) * direction,
                _damage, _speed, _lifetime);
        }
        _timer = _cooldown;
    }
}
