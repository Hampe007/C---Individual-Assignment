using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Owns the active enemy simulation: native runtime/configuration buffers, spawn placement, job scheduling,
/// damage/reward events, and the pooled GameObject views that represent simulated enemies.
/// </summary>
public sealed class HordeManager : MonoBehaviour
{
    [Header("Enemies")]
    [SerializeField, Min(1)] private int enemyCount = 1000;
    [SerializeField] private Transform target;
    [Header("Enemy Definitions")]
    [SerializeField] private EnemyDefinition[] enemyDefinitions;
    [Header("Spawning")]
    [SerializeField, Min(1f)] private float spawnEdge = 55f;
    [SerializeField, Min(0f)] private float spawnDepth = 2f;
    [SerializeField, Min(0f)] private float minSpawnDistance = 30f;
    [SerializeField, Range(0f, 22.5f)] private float zoneJitter = 22.5f;
    [SerializeField] private Camera visibilityCamera;
    [Header("Separation")]
    [SerializeField, Min(0.1f)] private float cellSize = 1.2f;
    [SerializeField, Min(0f)] private float separationDistance = 1.2f;
    [SerializeField, Range(0f, 2f)] private float separationWeight = 0.75f;
    [SerializeField, Min(1)] private int maxSeparationNeighbours = 8;
    [Header("Static Obstacle Navigation")]
    [SerializeField] private Transform obstacleRoot;
    [SerializeField] private Vector2 navigationArea = new(120f, 120f);
    [SerializeField, Min(0.25f)] private float navigationCellSize = 0.75f;
    [SerializeField, Min(0f)] private float obstacleClearance = 0.5f;
    private HordeNavigation navigation;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private CombatVFXSystem combatVFX;
    [SerializeField] private SoundSet enemyHitSFX;
    [SerializeField] private CombatProjectileSystem projectiles;
    // Jobs read the current state and write the next state, then the arrays swap after completion.
    private NativeArray<EnemyRuntime> enemies;
    private NativeArray<EnemyRuntime> nextEnemies;
    private NativeArray<EnemyConfig> enemyConfigs;
    private NativeParallelMultiHashMap<int2, int> grid;
    private NativeQueue<int> damageEvents;
    private NativeQueue<EnemySpitEvent> spitEvents;
    private EnemyViewSystem views;
    private Unity.Mathematics.Random random;
    private int activeEnemyCount;
    private int lastSpawnZone = -1;
    private int sameZoneCount;
    public int ActiveEnemyCount => activeEnemyCount; // Number of live entries at the front of the runtime arrays.
    public int MaxEnemyCount => enemyCount; // Capacity shared by runtime buffers and pooled views.

    public event Action<int, int> EnemyDied; // Publishes XP and score rewards when an enemy is removed by damage.
    public bool IsReady => enabled && enemies.IsCreated && enemyConfigs.IsCreated && views != null; // Guards spawning until setup succeeds.

    private void Awake()
    {
        if (target == null || visibilityCamera == null || playerHealth == null)
        {
            Debug.LogError("HordeManager is missing required references.");
            enabled = false;
            return;
        }

        if (enemyDefinitions == null || enemyDefinitions.Length == 0)
        {
            Debug.LogError("HordeManager requires enemy definitions.");
            enabled = false;
            return;
        }

        for (int i = 0; i < enemyDefinitions.Length; i++)
        {
            if (enemyDefinitions[i] != null)
            {
                continue;
            }

            Debug.LogError($"Enemy definition at index {i} is missing.");
            enabled = false;
            return;
        }

        random = new Unity.Mathematics.Random(1);
        // Convert managed ScriptableObject authoring data into job-friendly structs once at startup.
        enemyConfigs = new NativeArray<EnemyConfig>(enemyDefinitions.Length, Allocator.Persistent);
        GameObject[] prefabs = new GameObject[enemyDefinitions.Length];
        for (int i = 0; i < enemyDefinitions.Length; i++)
        {
            if (enemyDefinitions[i].Prefab == null)
            {
                Debug.LogError($"Enemy definition {enemyDefinitions[i].name} is missing a prefab.");
                enabled = false;
                DisposeNativeData();
                return;
            }

            enemyConfigs[i] = enemyDefinitions[i].CreateConfig();
            prefabs[i] = enemyDefinitions[i].Prefab;
        }

        enemies = new NativeArray<EnemyRuntime>(enemyCount, Allocator.Persistent); // Current simulation snapshot read by jobs.
        nextEnemies = new NativeArray<EnemyRuntime>(enemyCount, Allocator.Persistent); // Destination snapshot written during this frame's simulation.
        grid = new NativeParallelMultiHashMap<int2, int>(enemyCount, Allocator.Persistent); // Maps each occupied XZ cell to its enemy indices for neighbor lookup.
        damageEvents = new NativeQueue<int>(Allocator.Persistent); // Collects job-produced player damage for main-thread handling.
        spitEvents = new NativeQueue<EnemySpitEvent>(Allocator.Persistent);
        views = new EnemyViewSystem(prefabs, enemyCount); // Creates per-definition prefab pools with capacity matching the simulation.
        navigation = new HordeNavigation(obstacleRoot, new Vector2(transform.position.x, transform.position.z), navigationArea, navigationCellSize, obstacleClearance);
        navigation.Schedule(target.position).Complete();
    }

    private void Update()
    {
        // Pausing also freezes enemy timers and movement because no simulation jobs are scheduled.
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (activeEnemyCount > 0)
        {
            Simulate();
        }
    }

    internal bool TryGetClosestEnemy(float3 position, float range, out int enemyIndex, out float3 enemyPosition)
    {
        // Start with no result; squared distances avoid square roots during the scan.
        enemyIndex = -1;
        enemyPosition = float3.zero;
        float closestDistanceSq = range * range;
        for (int i = 0; i < activeEnemyCount; i++)
        {
            float distanceSq = math.distancesq(position, enemies[i].Position);
            if (distanceSq >= closestDistanceSq)
            {
                continue;
            }

            closestDistanceSq = distanceSq;
            enemyIndex = i;
            enemyPosition = enemies[i].Position;
        }

        return enemyIndex != -1;
    }

    internal bool TryGetEnemyOnSegment(float3 start, float3 end, float radius, out int enemyIndex)
    {
        return TryGetEnemyOnSegment(start, end, radius, out enemyIndex, out _);
    }

    internal bool TryGetEnemyOnSegment(float3 start, float3 end, float radius, out int enemyIndex, out float hitProgress)
    {
        enemyIndex = -1;
        hitProgress = float.MaxValue;
        for (int i = 0; i < activeEnemyCount; i++)
        {
            float3 point = enemies[i].Position;
            if (!CombatProjectileSystem.TryCircleHit(new Vector2(start.x, start.z), new Vector2(end.x, end.z), new Vector2(point.x, point.z), radius, out float progress) || progress >= hitProgress)
            {
                continue;
            }

            hitProgress = progress;
            enemyIndex = i;
        }

        return enemyIndex >= 0;
    }

    internal bool IsProjectilePathClear(Vector3 point)
    {
        return navigation != null && navigation.Grid.IsReachable(new float2(point.x, point.z));
    }

    internal bool IsProjectileSegmentClear(Vector3 start, Vector3 end)
    {
        return navigation != null && navigation.Grid.CanTravel(new float2(start.x, start.z), new float2(end.x, end.z));
    }

    internal Vector3 GetProjectileWallImpact(Vector3 start, Vector3 end)
    {
        float low = 0f, high = 1f;
        for (int i = 0; i < 12; i++)
        {
            float middle = (low + high) * 0.5f;
            if (IsProjectileSegmentClear(start, Vector3.Lerp(start, end, middle)))
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return Vector3.Lerp(start, end, low);
    }

    internal bool TryGetTrackingTarget(Vector3 origin, Vector3 direction, Vector3 originalDirection, float radius, float halfAngle, out Vector3 target)
    {
        target = default;
        bool found = false;
        float nearest = radius * radius;
        float threshold = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        for (int i = 0; i < activeEnemyCount; i++)
        {
            float3 position = enemies[i].Position;
            Vector3 candidate = new(position.x, origin.y, position.z);
            Vector3 offset = candidate - origin;
            float distanceSq = offset.sqrMagnitude;
            if (distanceSq >= nearest || distanceSq < 0.000001f)
            {
                continue;
            }

            Vector3 aim = offset.normalized;
            if (Vector3.Dot(direction, aim) < threshold || Vector3.Dot(originalDirection, aim) < threshold || !IsProjectileSegmentClear(origin, candidate))
            {
                continue;
            }

            nearest = distanceSq;
            target = candidate;
            found = true;
        }

        return found;
    }

    internal void ApplyPoisonInRadius(Vector3 center, float radius, float duration, float tickInterval)
    {
        for (int i = 0; i < activeEnemyCount; i++)
        {
            EnemyRuntime enemy = enemies[i];
            if (enemyConfigs[enemy.DefinitionIndex].Behaviour == EnemyBehaviourType.Spitter || math.distancesq(enemy.Position.xz, new float2(center.x, center.z)) > radius * radius)
            {
                continue;
            }

            if (enemy.PoisonRemaining <= 0f)
            {
                enemy.PoisonTickTimer = tickInterval;
            }

            enemy.PoisonRemaining = duration;
            enemies[i] = enemy;
            nextEnemies[i] = enemy;
        }
    }

    internal void TickPoison(float deltaTime, int damage, float tickInterval, ParticleSystem particles)
    {
        // Reverse traversal remains valid when a lethal tick swaps the last enemy into this slot.
        for (int i = activeEnemyCount - 1; i >= 0; i--)
        {
            EnemyRuntime enemy = enemies[i];
            if (enemy.PoisonRemaining <= 0f)
            {
                continue;
            }

            enemy.PoisonTickTimer -= Mathf.Min(deltaTime, enemy.PoisonRemaining);
            enemy.PoisonRemaining = Mathf.Max(0f, enemy.PoisonRemaining - deltaTime);
            int ticks = 0;
            while (enemy.PoisonTickTimer <= 0f)
            {
                ticks++;
                enemy.PoisonTickTimer += tickInterval;
            }

            enemies[i] = enemy;
            nextEnemies[i] = enemy;
            views.SetPoisoned(i, enemy.PoisonRemaining > 0f, particles);
            if (ticks > 0)
            {
                DamageEnemy(i, damage * ticks);
            }
        }
    }

    internal void ClearPoison()
    {
        if (!enemies.IsCreated)
        {
            return;
        }

        for (int i = 0; i < activeEnemyCount; i++)
        {
            EnemyRuntime enemy = enemies[i];
            enemy.PoisonRemaining = 0f;
            enemy.PoisonTickTimer = 0f;
            enemies[i] = enemy;
            nextEnemies[i] = enemy;
            views.SetPoisoned(i, false, null);
        }
    }

    internal void GetClosestEnemies(float3 position, float range, int maxCount, List<int> results)
    {
        // Rebuild the caller's result list by repeatedly finding the nearest not-yet-selected enemy.
        results.Clear();
        float rangeSq = range * range;
        for (int count = 0; count < maxCount; count++)
        {
            int closestIndex = -1;
            float closestDistanceSq = rangeSq;
            for (int i = 0; i < activeEnemyCount; i++)
            {
                if (results.Contains(i))
                {
                    continue;
                }

                float distanceSq = math.distancesq(position, enemies[i].Position);
                if (distanceSq >= closestDistanceSq)
                {
                    continue;
                }

                closestDistanceSq = distanceSq;
                closestIndex = i;
            }

            if (closestIndex < 0)
            {
                return;
            }

            results.Add(closestIndex);
        }
    }

    internal void DamageEnemy(int index, int damage)
    {
        // Ignore stale indices and non-positive hits before touching runtime state or presentation effects.
        if (index < 0 || index >= activeEnemyCount || damage <= 0)
        {
            return;
        }

        EnemyRuntime enemy = enemies[index]; // Work on a local copy, then write it back if it survives.
        enemy.Health -= damage;
        combatVFX.PlayBlood(enemy.Position);
        views.PlayHitFlash(index);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(enemyHitSFX, new Vector3(enemy.Position.x, enemy.Position.y, enemy.Position.z));
        }

        if (enemy.Health <= 0)
        {
            EnemyConfig config = enemyConfigs[enemy.DefinitionIndex];
            EnemyDied?.Invoke(config.XPReward, config.ScoreReward);
            RemoveEnemy(index);
            return;
        }

        enemies[index] = enemy;
        nextEnemies[index] = enemy;
    }

    private void RemoveEnemy(int index)
    {
        if (index < 0 || index >= activeEnemyCount)
        {
            return;
        }

        int lastIndex = activeEnemyCount - 1; // Active entries are packed, so the last one can fill the removed slot.
        if (index != lastIndex)
        {
            EnemyRuntime movedEnemy = enemies[lastIndex];
            enemies[index] = movedEnemy;
            nextEnemies[index] = movedEnemy;
        }

        enemies[lastIndex] = default;
        nextEnemies[lastIndex] = default;
        views.RemoveAtSwapBack(index); // Keep view and TransformAccessArray indices aligned with the runtime swap.
        activeEnemyCount--;
    }

    /// <summary>
    /// Adds one enemy if the manager is ready and has capacity. Runtime state and the pooled view are kept at
    /// the same index so jobs and view operations can address an enemy consistently.
    /// </summary>
    public bool TrySpawnEnemy(EnemyDefinition definition)
    {
        if (!IsReady || activeEnemyCount >= enemyCount || definition == null)
        {
            return false;
        }

        if (!TryGetSpawnPosition(out float3 position))
        {
            return false;
        }

        return SpawnEnemy(definition, position);
    }

    public bool TrySpawnEnemyAt(EnemyDefinition definition, Vector3 position)
    {
        if (!IsReady || activeEnemyCount >= enemyCount || definition == null || !navigation.Grid.IsReachable(new float2(position.x, position.z)))
        {
            return false;
        }

        return SpawnEnemy(definition, new float3(position.x, 0f, position.z));
    }

    private bool SpawnEnemy(EnemyDefinition definition, float3 position)
    {
        int definitionIndex = GetDefinitionIndex(definition); // Runtime jobs use compact indices into _enemyConfigs.
        if (definitionIndex < 0)
        {
            Debug.LogError($"Enemy definition {definition.name} is not registered in HordeManager.");
            return false;
        }

        EnemyConfig config = enemyConfigs[definitionIndex];
        int index = activeEnemyCount; // Append new enemies at the first unused slot in each aligned collection.
        EnemyRuntime enemy = new(position, config.Health, definitionIndex);
        try
        {
            views.AddView(position, definitionIndex);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Enemy view could not be spawned: {exception.Message}");
            return false;
        }

        enemies[index] = enemy;
        nextEnemies[index] = enemy;
        activeEnemyCount++;
        return true;
    }

    private void Simulate()
    {
        grid.Clear();
        // Stage 1: build a cell-to-enemy lookup from current positions. Movement depends on this grid
        // to find nearby enemies without comparing every pair.
        JobHandle gridHandle = new BuildSpatialGridJob
        {
            Enemies = enemies,
            CellSize = cellSize,
            Grid = grid.AsParallelWriter()
        }.Schedule(activeEnemyCount, 64);
        JobHandle navigationHandle = navigation.Schedule(target.position);
        // Stage 2: each enemy reads current state and the completed grid, then writes only its own next slot.
        // Damage is queued because jobs cannot call PlayerHealth or other managed Unity APIs.
        JobHandle moveHandle = new MoveEnemiesJob
        {
            Enemies = enemies,
            EnemyConfigs = enemyConfigs,
            NextEnemies = nextEnemies,
            Grid = grid.AsReadOnly(),
            DamageEvents = damageEvents.AsParallelWriter(),
            SpitEvents = spitEvents.AsParallelWriter(),
            Target = target.position,
            DeltaTime = Time.deltaTime,
            CellSize = cellSize,
            SeparationDistance = separationDistance,
            SeparationWeight = separationWeight,
            MaxNeighbours = maxSeparationNeighbours,
            Navigation = navigation.Grid
        }.Schedule(activeEnemyCount, 64, JobHandle.CombineDependencies(gridHandle, navigationHandle));
        // Stage 3: transform sync depends on movement so views use the newly computed positions.
        JobHandle viewHandle = views.ScheduleSync(nextEnemies, target.position, moveHandle);
        viewHandle.Complete();
        views.SyncTelegraphs(nextEnemies);
        // Main-thread effects are applied only after jobs finish; then next becomes the new current state.
        ApplyDamageEvents();
        ApplySpitEvents();
        (enemies, nextEnemies) = (nextEnemies, enemies);
    }

    private bool TryGetSpawnPosition(out float3 position)
    {
        // Candidates are sampled around the arena perimeter and rejected if too close to or visible by the player.
        for (int attempt = 0; attempt < 24; attempt++)
        {
            int zone = GetSpawnZone();
            float angle = zone * 45f + random.NextFloat(-zoneJitter, zoneJitter); // Eight 45-degree sectors with a small random offset.
            float2 direction = new(math.sin(math.radians(angle)), math.cos(math.radians(angle)));
            float edge = spawnEdge + random.NextFloat(-spawnDepth, spawnDepth);
            // Scale the direction until its largest axis reaches the square arena edge.
            float scale = edge / math.max(math.abs(direction.x), math.abs(direction.y));
            float3 candidate = new(direction.x * scale, 0f, direction.y * scale);
            if (!navigation.Grid.IsReachable(candidate.xz))
            {
                continue;
            }

            float minDistanceSq = minSpawnDistance * minSpawnDistance; // Compare squared values to avoid a square root.
            if (math.distancesq(candidate, (float3)target.position) < minDistanceSq)
            {
                continue;
            }

            if (IsVisible(candidate))
            {
                continue;
            }

            RememberSpawnZone(zone);
            position = candidate;
            return true;
        }

        position = default;
        return false;
    }

    private int GetSpawnZone()
    {
        // Pick a sector, then nudge repeated choices after two accepted spawns in the same sector.
        int zone = random.NextInt(0, 8);
        if (sameZoneCount >= 2 && zone == lastSpawnZone)
        {
            zone = (zone + random.NextInt(1, 8)) % 8;
        }

        return zone;
    }

    private void RememberSpawnZone(int zone)
    {
        // Only accepted positions affect the repetition history; rejected candidates are forgotten.
        if (zone == lastSpawnZone)
        {
            sameZoneCount++;
            return;
        }

        lastSpawnZone = zone;
        sameZoneCount = 1;
    }

    private bool IsVisible(float3 position)
    {
        // Test near the enemy's body height rather than at its ground-level origin.
        Vector3 point = new(position.x, position.y + 0.9f, position.z);
        Vector3 viewport = visibilityCamera.WorldToViewportPoint(point);
        // A small margin avoids spawning just outside the edge where it could still be immediately visible.
        const float padding = 0.05f;
        return viewport.z > 0f && viewport.x >= -padding && viewport.x <= 1f + padding && viewport.y >= -padding && viewport.y <= 1f + padding;
    }

    private int GetDefinitionIndex(EnemyDefinition definition)
    {
        // Config and prefab arrays share the definition asset order, so the index identifies both.
        for (int i = 0; i < enemyDefinitions.Length; i++)
        {
            if (enemyDefinitions[i] == definition)
            {
                return i;
            }
        }

        return -1;
    }

    private void ApplyDamageEvents()
    {
        // Drain after job completion: this is where queued damage can safely call managed gameplay code.
        while (damageEvents.TryDequeue(out int damage))
        {
            playerHealth.TakeDamage(damage);
        }
    }

    private void ApplySpitEvents()
    {
        while (spitEvents.TryDequeue(out EnemySpitEvent spit))
        {
            if (projectiles == null)
            {
                continue;
            }

            Vector3 origin = spit.Origin;
            Vector3 direction = new(spit.Target.x - spit.Origin.x, 0f, spit.Target.z - spit.Origin.z);
            if (direction.sqrMagnitude < 0.001f)
            {
                continue;
            }

            direction.Normalize();
            for (int i = 0; i < spit.Count; i++)
            {
                float angle = (i - (spit.Count - 1) * 0.5f) * spit.Spread;
                int distanceFromCenter = Mathf.Abs(i - spit.Count / 2);
                float scale = spit.Count == 1 ? spit.MediumOrbScale : distanceFromCenter == 0 ? spit.LargeOrbScale : spit.Count == 5 && distanceFromCenter == 1 ? spit.MediumOrbScale : spit.SmallOrbScale;
                projectiles.SpawnSpit(origin, Quaternion.Euler(0f, angle, 0f) * direction, spit.Damage, spit.Speed, spit.Lifetime, scale, spit.LeavesPoison != 0 && distanceFromCenter == 0 && spit.Count > 1);
            }
        }
    }

    private void OnDestroy()
    {
        // Dispose view-side native containers first, and always release the manager's containers afterward.
        try
        {
            views?.Dispose();
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
        navigation?.Dispose();
        // Persistent allocations outlive frames and must be explicitly released during teardown or failed setup.
        if (enemies.IsCreated)
        {
            enemies.Dispose();
        }

        if (nextEnemies.IsCreated)
        {
            nextEnemies.Dispose();
        }

        if (enemyConfigs.IsCreated)
        {
            enemyConfigs.Dispose();
        }

        if (grid.IsCreated)
        {
            grid.Dispose();
        }

        if (damageEvents.IsCreated)
        {
            damageEvents.Dispose();
        }

        if (spitEvents.IsCreated)
        {
            spitEvents.Dispose();
        }
    }
}