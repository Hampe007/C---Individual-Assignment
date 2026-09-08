using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public sealed class HordeManager : MonoBehaviour
{
    [Header("Enemies")]
    [SerializeField, Min(1)] private int _enemyCount = 1000;
    [SerializeField, Min(0f)] private float _moveSpeed = 3f;
    [SerializeField] private Transform _target;
    [SerializeField] private GameObject _impPrefab;
    [SerializeField] private GameObject _lycanPrefab;
    [SerializeField] private GameObject _tidebreakerPrefab;

    [Header("Spawning")]
    [SerializeField, Min(1f)] private float _spawnEdge = 55f;
    [SerializeField, Min(0f)] private float _spawnDepth = 2f;
    [SerializeField, Min(0f)] private float _minSpawnDistance = 30f;
    [SerializeField, Range(0f, 22.5f)] private float _zoneJitter = 22.5f;
    [SerializeField] private Camera _camera;

    [Header("Separation")]
    [SerializeField, Min(0.1f)] private float _cellSize = 1.2f;
    [SerializeField, Min(0f)] private float _separationDistance = 1.2f;
    [SerializeField, Range(0f, 2f)] private float _separationWeight = 0.75f;
    [SerializeField, Min(1)] private int _maxSeparationNeighbours = 8;

    [Header("Swarm")]
    [SerializeField, Min(0f)] private float _swarmAttackRange = 1.3f;
    [SerializeField, Min(1)] private int _swarmDamage = 10;
    [SerializeField, Min(0f)] private float _swarmAttackCooldown = 1f;
    
    [Header("Charger")]
    [SerializeField, Min(0f)] private float _chargeRange = 10f;
    [SerializeField, Min(0f)] private float _chargeSpeed = 12f;
    [SerializeField, Min(0f)] private float _chargeDuration = 0.55f;
    [SerializeField, Min(0f)] private float _chargeTelegraph = 0.65f;
    [SerializeField, Min(0f)] private float _chargeRecovery = 0.8f;
    [SerializeField, Min(0f)] private float _chargeCooldown = 5f;
    [SerializeField, Min(0f)] private float _chargeHitRange = 1.5f;
    [SerializeField, Min(1)] private int _chargeDamage = 25;
    
    [Header("Brute")]
    [SerializeField, Min(0f)] private float _bruteMoveSpeed = 1.5f;
    [SerializeField, Min(0f)] private float _bruteSlamRange = 3f;
    [SerializeField, Min(0f)] private float _bruteWindup = 1.2f;
    [SerializeField, Min(0f)] private float _bruteAttackDuration = 0.25f;
    [SerializeField, Min(0f)] private float _bruteRecovery = 1.5f;
    [SerializeField, Min(0f)] private float _bruteCooldown = 2.5f;
    [SerializeField, Min(1)] private int _bruteDamage = 35;

    [SerializeField] private PlayerHealth _playerHealth;
    
    private NativeArray<EnemyRuntime> _enemies;
    private NativeArray<EnemyRuntime> _nextEnemies;
    private NativeParallelMultiHashMap<int2, int> _grid;
    private NativeQueue<DamageEvent> _damageEvents;
    private EnemyViewSystem _views;

    private Unity.Mathematics.Random _random;
    private int _activeEnemyCount;
    private int _lastSpawnZone = -1;
    private int _sameZoneCount;

    public int EnemyCount => _activeEnemyCount;
    public int ActiveEnemyCount => _activeEnemyCount;
    public int MaxEnemyCount => _enemyCount;
    public bool IsReady => enabled && _enemies.IsCreated && _views != null;

    private void Awake()
    {
        if (_target == null || _camera == null || _playerHealth == null ||_impPrefab == null || _lycanPrefab == null || _tidebreakerPrefab == null)
        {
            Debug.LogError("HordeManager is missing required references.");
            enabled = false;
            return;
        }

        _random = new Unity.Mathematics.Random(1);

        _enemies = new NativeArray<EnemyRuntime>(_enemyCount, Allocator.Persistent);
        _nextEnemies = new NativeArray<EnemyRuntime>(_enemyCount, Allocator.Persistent);
        _grid = new NativeParallelMultiHashMap<int2, int>(_enemyCount, Allocator.Persistent);
        _damageEvents = new NativeQueue<DamageEvent>(Allocator.Persistent);

        _views = new EnemyViewSystem(_impPrefab, _lycanPrefab, _tidebreakerPrefab, _enemyCount);
    }

    private void Update()
    {
        if (_activeEnemyCount > 0)
            Simulate();
    }

    private EnemyVisualType GetVisual(EnemyBehaviourType behaviour)
    {
        switch (behaviour)
        {
            case EnemyBehaviourType.Charger:
                return EnemyVisualType.Lycan;

            case EnemyBehaviourType.Brute:
                return EnemyVisualType.Tidebreaker;

            default:
                return EnemyVisualType.Imp;
        }
    }
    
    public bool TrySpawnEnemy(EnemyBehaviourType behaviour)
    {
        if (!IsReady || _activeEnemyCount >= _enemyCount)
            return false;

        if (!TryGetSpawnPosition(out float3 position))
            return false;

        int index = _activeEnemyCount;
        EnemyVisualType visual = GetVisual(behaviour);
        EnemyRuntime enemy = new(position, _moveSpeed, behaviour, visual);

        try
        {
            _views.AddView(position, visual);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Enemy view could not be spawned: {exception.Message}");
            return false;
        }

        _enemies[index] = enemy;
        _nextEnemies[index] = enemy;
        _activeEnemyCount++;

        return true;
    }

    private void Simulate()
    {
        _grid.Clear();

        JobHandle gridHandle = new BuildSpatialGridJob
        {
            Enemies = _enemies,
            CellSize = _cellSize,
            Grid = _grid.AsParallelWriter()
        }.Schedule(_activeEnemyCount, 64);

        JobHandle moveHandle = new MoveEnemiesJob
        {
            Enemies = _enemies,
            NextEnemies = _nextEnemies,
            Grid = _grid.AsReadOnly(),
            DamageEvents = _damageEvents.AsParallelWriter(),

            
            Target = _target.position,
            DeltaTime = Time.deltaTime,

            CellSize = _cellSize,
            SeparationDistance = _separationDistance,
            SeparationWeight = _separationWeight,
            MaxNeighbours = _maxSeparationNeighbours,

            SwarmAttackRange = _swarmAttackRange,
            SwarmDamage = _swarmDamage,
            SwarmAttackCooldown = _swarmAttackCooldown,
            
            ChargeRange = _chargeRange,
            ChargeSpeed = _chargeSpeed,
            ChargeDuration = _chargeDuration,
            ChargeTelegraph = _chargeTelegraph,
            ChargeRecovery = _chargeRecovery,
            ChargeCooldown = _chargeCooldown,
            ChargeHitRange = _chargeHitRange,
            ChargeDamage = _chargeDamage,
            
            BruteMoveSpeed = _bruteMoveSpeed,
            BruteSlamRange = _bruteSlamRange,
            BruteWindup = _bruteWindup,
            BruteAttackDuration = _bruteAttackDuration,
            BruteRecovery = _bruteRecovery,
            BruteCooldown = _bruteCooldown,
            BruteDamage = _bruteDamage
        }.Schedule(_activeEnemyCount, 64, gridHandle);

        JobHandle viewHandle = _views.ScheduleSync(_nextEnemies, _target.position, moveHandle);
        viewHandle.Complete();
        ApplyDamageEvents();

        (_enemies, _nextEnemies) = (_nextEnemies, _enemies);
    }

    private bool TryGetSpawnPosition(out float3 position)
    {
        for (int attempt = 0; attempt < 24; attempt++)
        {
            int zone = GetSpawnZone();
            float angle = zone * 45f + _random.NextFloat(-_zoneJitter, _zoneJitter);
            float2 direction = new(math.sin(math.radians(angle)), math.cos(math.radians(angle)));

            float edge = _spawnEdge + _random.NextFloat(-_spawnDepth, _spawnDepth);
            float scale = edge / math.max(math.abs(direction.x), math.abs(direction.y));

            float3 candidate = new(direction.x * scale, 0f, direction.y * scale);
            float minDistanceSq = _minSpawnDistance * _minSpawnDistance;

            if (math.distancesq(candidate, (float3)_target.position) < minDistanceSq)
                continue;

            if (IsVisible(candidate))
                continue;

            RememberSpawnZone(zone);
            position = candidate;
            return true;
        }

        position = default;
        return false;
    }

    private int GetSpawnZone()
    {
        int zone = _random.NextInt(0, 8);

        if (_sameZoneCount >= 2 && zone == _lastSpawnZone)
            zone = (zone + _random.NextInt(1, 8)) % 8;

        return zone;
    }

    private void RememberSpawnZone(int zone)
    {
        if (zone == _lastSpawnZone)
        {
            _sameZoneCount++;
            return;
        }

        _lastSpawnZone = zone;
        _sameZoneCount = 1;
    }

    private bool IsVisible(float3 position)
    {
        Vector3 point = new(position.x, position.y + 0.9f, position.z);
        Vector3 viewport = _camera.WorldToViewportPoint(point);

        const float padding = 0.05f;

        return viewport.z > 0f && viewport.x >= -padding && viewport.x <= 1f + padding && viewport.y >= -padding && viewport.y <= 1f + padding;
    }
    
    private void ApplyDamageEvents()
    {
        while (_damageEvents.TryDequeue(out DamageEvent damageEvent))
            _playerHealth.TakeDamage(damageEvent.Damage);
    }

    private void OnDestroy()
    {
        try
        {
            _views?.Dispose();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            DisposeNativeData();
        }
    }

    private void DisposeNativeData()
    {
        if (_enemies.IsCreated)
            _enemies.Dispose();

        if (_nextEnemies.IsCreated)
            _nextEnemies.Dispose();

        if (_grid.IsCreated)
            _grid.Dispose();

        if (_damageEvents.IsCreated)
            _damageEvents.Dispose();
    }
}