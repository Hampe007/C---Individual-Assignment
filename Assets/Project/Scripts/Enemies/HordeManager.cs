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
    [SerializeField] private GameObject _enemyViewPrefab;

    [Header("Spawning")]
    [SerializeField, Min(1f)] private float _spawnEdge = 55f;
    [SerializeField, Min(0f)] private float _minSpawnDistance = 30f;
    [SerializeField, Range(0f, 22.5f)] private float _zoneJitter = 15f;
    [SerializeField] private Camera _camera;
    
    [Header("Separation")]
    [SerializeField, Min(0.1f)] private float _cellSize = 1.2f;
    [SerializeField, Min(0f)] private float _separationDistance = 1.2f;
    [SerializeField, Range(0f, 2f)] private float _separationWeight = 0.75f;
    [SerializeField, Min(1)] private int _maxSeparationNeighbours = 8;

    private NativeArray<EnemyRuntime> _enemies;
    private NativeArray<EnemyRuntime> _nextEnemies;
    private NativeParallelMultiHashMap<int2, int> _grid;
    private EnemyViewSystem _views;
    
    [Header("Charger")]
    [SerializeField, Min(0f)] private float _chargeRange = 10f;
    [SerializeField, Min(0f)] private float _chargeSpeed = 12f;
    [SerializeField, Min(0f)] private float _chargeDuration = 0.55f;
    [SerializeField, Min(0f)] private float _chargeTelegraph = 0.65f;
    [SerializeField, Min(0f)] private float _chargeRecovery = 0.8f;
    [SerializeField, Min(0f)] private float _chargeCooldown = 5f;
    
    [SerializeField] private EnemyBehaviourType _spawnBehaviour =
        EnemyBehaviourType.Swarm;

    public int EnemyCount => _enemyCount;

    private void Start()
    {
        if (_target == null || _enemyViewPrefab == null)
        {
            Debug.LogError("HordeManager requires a target and enemy view prefab.");
            enabled = false;
            return;
        }

        InitializeEnemies();
        _views = new EnemyViewSystem(_enemyViewPrefab, _enemyCount);
    }

    private void Update()
    {
        if (!_enemies.IsCreated)
            return;

        Simulate();
    }

    private void Simulate()
    {
        _grid.Clear();

        JobHandle gridHandle = new BuildSpatialGridJob
        {
            Enemies = _enemies,
            CellSize = _cellSize,
            Grid = _grid.AsParallelWriter()
        }.Schedule(_enemies.Length, 64);

        JobHandle moveHandle = new MoveEnemiesJob
        {
            Enemies = _enemies,
            NextEnemies = _nextEnemies,
            Grid = _grid.AsReadOnly(),
            Target = _target.position,
            DeltaTime = Time.deltaTime,
            CellSize = _cellSize,
            SeparationDistance = _separationDistance,
            SeparationWeight = _separationWeight,
            MaxNeighbours = _maxSeparationNeighbours,
            
            ChargeRange = _chargeRange,
            ChargeSpeed = _chargeSpeed,
            ChargeDuration = _chargeDuration,
            ChargeTelegraph = _chargeTelegraph,
            ChargeRecovery = _chargeRecovery,
            ChargeCooldown = _chargeCooldown
        }.Schedule(_enemies.Length, 64, gridHandle);

        JobHandle viewHandle = _views.ScheduleSync(
            _nextEnemies,
            _target.position,
            moveHandle);

        viewHandle.Complete();

        (_enemies, _nextEnemies) = (_nextEnemies, _enemies);
    }

    private void InitializeEnemies()
    {
        _enemies = new NativeArray<EnemyRuntime>(
            _enemyCount,
            Allocator.Persistent);

        _nextEnemies = new NativeArray<EnemyRuntime>(
            _enemyCount,
            Allocator.Persistent);

        _grid = new NativeParallelMultiHashMap<int2, int>(
            _enemyCount,
            Allocator.Persistent);

        var random = new Unity.Mathematics.Random(1);

        for (int i = 0; i < _enemyCount; i++)
        {
            float3 position = GetSpawnPosition(ref random);
            _enemies[i] = new EnemyRuntime(
                position,
                _moveSpeed,
                _spawnBehaviour);
        }
    }

    private float3 GetSpawnPosition(ref Unity.Mathematics.Random random)
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            int zone = random.NextInt(0, 8);

            float angle = math.radians(
                zone * 45f +
                random.NextFloat(-_zoneJitter, _zoneJitter));

            float2 direction = new(math.sin(angle), math.cos(angle));

            float scale = _spawnEdge /
                          math.max(math.abs(direction.x), math.abs(direction.y));

            float3 position = new(
                direction.x * scale,
                0f,
                direction.y * scale);

            if (math.distance(position, (float3)_target.position) < _minSpawnDistance)
                continue;

            if (!IsVisible(position))
                return position;
        }

        return new float3(0f, 0f, _spawnEdge);
    }

    private bool IsVisible(float3 position)
    {
        if (_camera == null)
            return false;

        Vector3 viewport = _camera.WorldToViewportPoint(position);

        const float padding = 0.05f;

        return viewport.z > 0f &&
               viewport.x >= -padding &&
               viewport.x <= 1f + padding &&
               viewport.y >= -padding &&
               viewport.y <= 1f + padding;
    }
    
    private void OnDestroy()
    {
        try
        {
            _views?.Dispose();
        }
        catch (System.Exception exception)
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
    }
}