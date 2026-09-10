using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public sealed class HordeManager : MonoBehaviour
{
    [Header("Enemies")]
    [SerializeField, Min(1)] private int _enemyCount = 1000;
    [SerializeField] private Transform _target;

    [Header("Enemy Definitions")]
    [SerializeField] private EnemyDefinition[] _enemyDefinitions;
    
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

    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private CombatVFXSystem _combatVFX;
    [SerializeField] private SoundSet _enemyHitSFX;
    
    private NativeArray<EnemyRuntime> _enemies;
    private NativeArray<EnemyRuntime> _nextEnemies;
    private NativeArray<EnemyConfig> _enemyConfigs;
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
    public bool IsReady => enabled && _enemies.IsCreated && _enemyConfigs.IsCreated && _views != null;
    
    private void Awake()
    {
        if (_target == null || _camera == null || _playerHealth == null)
        {
            Debug.LogError("HordeManager is missing required references.");
            enabled = false;
            return;
        }

        if (_enemyDefinitions == null || _enemyDefinitions.Length == 0)
        {
            Debug.LogError("HordeManager requires enemy definitions.");
            enabled = false;
            return;
        }

        for (int i = 0; i < _enemyDefinitions.Length; i++)
        {
            if (_enemyDefinitions[i] != null)
                continue;

            Debug.LogError($"Enemy definition at index {i} is missing.");
            enabled = false;
            return;
        }

        _random = new Unity.Mathematics.Random(1);

        _enemyConfigs = new NativeArray<EnemyConfig>(_enemyDefinitions.Length, Allocator.Persistent);
        GameObject[] prefabs = new GameObject[_enemyDefinitions.Length];

        for (int i = 0; i < _enemyDefinitions.Length; i++)
        {
            if (_enemyDefinitions[i].Prefab == null)
            {
                Debug.LogError($"Enemy definition {_enemyDefinitions[i].name} is missing a prefab.");
                enabled = false;
                DisposeNativeData();
                return;
            }

            _enemyConfigs[i] = _enemyDefinitions[i].CreateConfig();
            prefabs[i] = _enemyDefinitions[i].Prefab;
        }

        _enemies = new NativeArray<EnemyRuntime>(_enemyCount, Allocator.Persistent);
        _nextEnemies = new NativeArray<EnemyRuntime>(_enemyCount, Allocator.Persistent);
        _grid = new NativeParallelMultiHashMap<int2, int>(_enemyCount, Allocator.Persistent);
        _damageEvents = new NativeQueue<DamageEvent>(Allocator.Persistent);

        _views = new EnemyViewSystem(prefabs, _enemyCount);
    }

    private void Update()
    {
        if (_activeEnemyCount > 0)
            Simulate();
        
        //if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame && _activeEnemyCount > 0)
        //    DamageEnemy(0, 50);
    }

    internal bool TryGetClosestEnemy(float3 position, float range, out int enemyIndex, out float3 enemyPosition)
    {
        enemyIndex = -1;
        enemyPosition = float3.zero;

        float closestDistanceSq = range * range;

        for (int i = 0; i < _activeEnemyCount; i++)
        {
            float distanceSq = math.distancesq(position, _enemies[i].Position);

            if (distanceSq >= closestDistanceSq)
                continue;
            
            closestDistanceSq = distanceSq;
            enemyIndex = i;
            enemyPosition = _enemies[i].Position;
        }
        return enemyIndex != -1;
    }
    
    internal void DamageEnemy(int index, int damage)
    {
        if (index < 0 || index >= _activeEnemyCount || damage <= 0)
            return;

        EnemyRuntime enemy = _enemies[index];
        enemy.Health -= damage;
        _combatVFX.PlayBlood(enemy.Position);
        _views.PlayHitFlash(index);
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(_enemyHitSFX, new Vector3(enemy.Position.x, enemy.Position.y, enemy.Position.z));
        
        if (enemy.Health <= 0)
        {
            RemoveEnemy(index);
            return;
        }

        _enemies[index] = enemy;
        _nextEnemies[index] = enemy;
    }
    
    private void RemoveEnemy(int index)
    {
        if (index < 0 || index >= _activeEnemyCount)
            return;

        int lastIndex = _activeEnemyCount - 1;

        if (index != lastIndex)
        {
            EnemyRuntime movedEnemy = _enemies[lastIndex];

            _enemies[index] = movedEnemy;
            _nextEnemies[index] = movedEnemy;
        }

        _enemies[lastIndex] = default;
        _nextEnemies[lastIndex] = default;

        _views.RemoveAtSwapBack(index);

        _activeEnemyCount--;
    }
    
    public bool TrySpawnEnemy(EnemyDefinition definition)
    {
        if (!IsReady || _activeEnemyCount >= _enemyCount || definition == null)
            return false;

        if (!TryGetSpawnPosition(out float3 position))
            return false;

        int definitionIndex = GetDefinitionIndex(definition);

        if (definitionIndex < 0)
        {
            Debug.LogError($"Enemy definition {definition.name} is not registered in HordeManager.");
            return false;
        }

        EnemyConfig config = _enemyConfigs[definitionIndex];

        int index = _activeEnemyCount;
        EnemyRuntime enemy = new(position, config.Health, definitionIndex);

        try
        {
            _views.AddView(position, definitionIndex);
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
            EnemyConfigs = _enemyConfigs,
            NextEnemies = _nextEnemies,
            Grid = _grid.AsReadOnly(),
            DamageEvents = _damageEvents.AsParallelWriter(),

            
            Target = _target.position,
            DeltaTime = Time.deltaTime,

            CellSize = _cellSize,
            SeparationDistance = _separationDistance,
            SeparationWeight = _separationWeight,
            MaxNeighbours = _maxSeparationNeighbours
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
    
    private int GetDefinitionIndex(EnemyDefinition definition)
    {
        for (int i = 0; i < _enemyDefinitions.Length; i++)
        {
            if (_enemyDefinitions[i] == definition)
                return i;
        }

        return -1;
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
       
        if (_enemyConfigs.IsCreated)
            _enemyConfigs.Dispose();

        if (_grid.IsCreated)
            _grid.Dispose();

        if (_damageEvents.IsCreated)
            _damageEvents.Dispose();
    }
}
