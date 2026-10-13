using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>Moves pooled, straight-line combat projectiles in the ground plane.</summary>
public sealed class CombatProjectileSystem : MonoBehaviour
{
    [SerializeField] private HordeManager _hordeManager;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private Transform _player;
    [SerializeField] private GameObject _soulBoltPrefab;
    [SerializeField] private GameObject _spitPrefab;
    [SerializeField] private ParticleSystem _impactPrefab;
    [SerializeField, Min(1)] private int _maxPlayerProjectiles = 48;
    [SerializeField, Min(1)] private int _maxEnemyProjectiles = 128;
    [SerializeField, Min(0.1f)] private float _playerHitRadius = 0.65f;

    private readonly List<Projectile> _active = new();
    private ObjectPool<GameObject> _soulPool;
    private ObjectPool<GameObject> _spitPool;
    private ObjectPool<ParticleSystem> _impactPool;
    private int _playerCount;
    private int _enemyCount;

    private struct Projectile
    {
        public GameObject View;
        public Vector3 Direction;
        public float Speed;
        public float Lifetime;
        public float Radius;
        public int Damage;
        public bool FromPlayer;
    }

    private void Awake()
    {
        if (_hordeManager == null || _playerHealth == null || _player == null ||
            _soulBoltPrefab == null || _spitPrefab == null || _impactPrefab == null)
        {
            Debug.LogError("CombatProjectileSystem is missing a required reference.", this);
            enabled = false;
            return;
        }

        _soulPool = CreatePool(_soulBoltPrefab, _maxPlayerProjectiles);
        _spitPool = CreatePool(_spitPrefab, _maxEnemyProjectiles);
        _impactPool = new ObjectPool<ParticleSystem>(
            () => Instantiate(_impactPrefab, transform),
            effect => effect.gameObject.SetActive(true),
            effect => effect.gameObject.SetActive(false),
            effect => { if (effect != null) Destroy(effect.gameObject); },
            true, 8, 64);
    }

    private ObjectPool<GameObject> CreatePool(GameObject prefab, int max)
    {
        return new ObjectPool<GameObject>(
            () => Instantiate(prefab, transform),
            view => view.SetActive(true),
            view => view.SetActive(false),
            view => { if (view != null) Destroy(view); },
            true, Mathf.Min(16, max), max);
    }

    public void SpawnSoulBolt(Vector3 origin, Vector3 direction, int damage, float speed, float lifetime)
    {
        Spawn(origin, direction, damage, speed, lifetime, 0.42f, true);
    }

    public void SpawnSpit(Vector3 origin, Vector3 direction, int damage, float speed, float lifetime)
    {
        Spawn(origin, direction, damage, speed, lifetime, 0.45f, false);
    }

    private void Spawn(Vector3 origin, Vector3 direction, int damage, float speed, float lifetime, float radius, bool fromPlayer)
    {
        if (!isActiveAndEnabled || damage <= 0 || direction.sqrMagnitude < 0.001f ||
            (fromPlayer ? _playerCount >= _maxPlayerProjectiles : _enemyCount >= _maxEnemyProjectiles))
            return;

        direction.y = 0f;
        GameObject view = (fromPlayer ? _soulPool : _spitPool).Get();
        view.transform.position = origin;
        view.transform.localScale = Vector3.one;
        if (view.TryGetComponent(out TrailRenderer trail))
            trail.Clear();

        _active.Add(new Projectile
        {
            View = view,
            Direction = direction.normalized,
            Speed = speed,
            Lifetime = lifetime,
            Radius = radius,
            Damage = damage,
            FromPlayer = fromPlayer
        });

        if (fromPlayer) _playerCount++;
        else _enemyCount++;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || _playerHealth.CurrentHealth <= 0)
            return;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Projectile projectile = _active[i];
            Vector3 start = projectile.View.transform.position;
            Vector3 end = start + projectile.Direction * (projectile.Speed * Time.deltaTime);
            projectile.Lifetime -= Time.deltaTime;

            if (!_hordeManager.IsProjectilePathClear(end))
            {
                PlayImpact(end, projectile.FromPlayer);
                ReleaseAt(i);
                continue;
            }

            bool hit = projectile.FromPlayer
                ? HitEnemy(projectile, start, end)
                : HitPlayer(projectile, start, end);

            if (hit || projectile.Lifetime <= 0f)
            {
                if (hit) PlayImpact(end, projectile.FromPlayer);
                ReleaseAt(i);
                continue;
            }

            projectile.View.transform.position = end;
            _active[i] = projectile;
        }
    }

    private bool HitEnemy(Projectile projectile, Vector3 start, Vector3 end)
    {
        if (!_hordeManager.TryGetEnemyOnSegment((float3)start, (float3)end, projectile.Radius, out int index))
            return false;

        _hordeManager.DamageEnemy(index, projectile.Damage);
        return true;
    }

    private bool HitPlayer(Projectile projectile, Vector3 start, Vector3 end)
    {
        Vector2 from = new(start.x, start.z);
        Vector2 to = new(end.x, end.z);
        Vector2 player = new(_player.position.x, _player.position.z);
        Vector2 path = to - from;
        float progress = path.sqrMagnitude > 0f
            ? Mathf.Clamp01(Vector2.Dot(player - from, path) / path.sqrMagnitude)
            : 0f;
        float radius = _playerHitRadius + projectile.Radius;
        if ((player - (from + path * progress)).sqrMagnitude > radius * radius)
            return false;

        _playerHealth.TakeDamage(projectile.Damage);
        return true;
    }

    private void PlayImpact(Vector3 position, bool fromPlayer)
    {
        ParticleSystem effect = _impactPool.Get();
        var main = effect.main;
        main.startColor = fromPlayer ? new Color(0.25f, 0.8f, 1f) : new Color(0.65f, 1f, 0.15f);
        effect.transform.position = position;
        effect.Play(true);
        StartCoroutine(ReleaseImpact(effect));
    }

    private System.Collections.IEnumerator ReleaseImpact(ParticleSystem effect)
    {
        yield return new WaitForSeconds(effect.main.duration + effect.main.startLifetime.constantMax);
        if (effect != null)
        {
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _impactPool.Release(effect);
        }
    }

    private void ReleaseAt(int index)
    {
        Projectile projectile = _active[index];
        if (projectile.View.TryGetComponent(out TrailRenderer trail)) trail.Clear();
        (projectile.FromPlayer ? _soulPool : _spitPool).Release(projectile.View);
        if (projectile.FromPlayer) _playerCount--;
        else _enemyCount--;
        _active.RemoveAt(index);
    }

    private void OnDestroy()
    {
        _active.Clear();
        _soulPool?.Clear();
        _spitPool?.Clear();
        _impactPool?.Clear();
    }
}
