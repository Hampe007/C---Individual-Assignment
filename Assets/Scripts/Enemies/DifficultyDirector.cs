using UnityEngine;

/// <summary>
/// Controls when enemy batches are requested and which definitions are eligible as the run progresses.
/// HordeManager handles capacity checks, spawn placement, and creating each enemy.
/// </summary>
public sealed class DifficultyDirector : MonoBehaviour
{
    [SerializeField] private HordeManager hordeManager;
    [Header("Starting Pressure")]
    [SerializeField, Min(0)] private int startingEnemies = 20;
    [Header("Difficulty Scaling")]
    [SerializeField, Min(1f)] private float rampSeconds = 300f;
    [SerializeField, Min(0.1f)] private float startSpawnInterval = 1.25f;
    [SerializeField, Min(0.1f)] private float endSpawnInterval = 0.4f;
    [SerializeField, Min(1)] private int startBatchSize = 2;
    [SerializeField, Min(1)] private int endBatchSize = 10;
    [Header("Enemy Spawning")]
    [SerializeField] private EnemySpawnEntry[] spawnEntries;
    private float elapsed;
    private float spawnTimer;
    private bool started;
    private void Start()
    {
        if (hordeManager != null && spawnEntries != null && spawnEntries.Length > 0)
        {
            return;
        }

        Debug.LogError("DifficultyDirector is missing required references.");
        enabled = false;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (!hordeManager.IsReady)
        {
            return;
        }

        if (!started)
        {
            // Wait for HordeManager's native data and view pools to be initialized before starting the run.
            started = true;
            spawnTimer = startSpawnInterval;
            SpawnEnemies(startingEnemies);
            return;
        }

        elapsed += Time.deltaTime;
        if (hordeManager.ActiveEnemyCount >= hordeManager.MaxEnemyCount)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        // Interpolate both spawn cadence and batch size over the same ramp.
        float progress = Mathf.Clamp01(elapsed / rampSeconds);
        float interval = Mathf.Lerp(startSpawnInterval, endSpawnInterval, progress);
        int batchSize = Mathf.RoundToInt(Mathf.Lerp(startBatchSize, endBatchSize, progress));
        SpawnEnemies(batchSize);
        spawnTimer = interval;
    }

    private void SpawnEnemies(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (hordeManager.ActiveEnemyCount >= hordeManager.MaxEnemyCount)
            {
                return;
            }

            EnemyDefinition enemy = GetEnemy();
            if (enemy == null)
            {
                return;
            }

            hordeManager.TrySpawnEnemy(enemy);
        }
    }

    private EnemyDefinition GetEnemy()
    {
        // Weighted selection only considers definitions whose unlock time has passed.
        float totalWeight = 0f;
        for (int i = 0; i < spawnEntries.Length; i++)
        {
            EnemySpawnEntry entry = spawnEntries[i];
            if (entry.Enemy != null && elapsed >= entry.UnlockTime)
            {
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.Range(0f, totalWeight);
        for (int i = 0; i < spawnEntries.Length; i++)
        {
            EnemySpawnEntry entry = spawnEntries[i];
            if (entry.Enemy == null || elapsed < entry.UnlockTime)
            {
                continue;
            }

            roll -= entry.Weight;
            if (roll <= 0f)
            {
                return entry.Enemy;
            }
        }

        return null;
    }
}

/// <summary>Inspector-authored option for adding an enemy type to the director's weighted pool.</summary>
[System.Serializable]
public sealed class EnemySpawnEntry
{
    [SerializeField] private EnemyDefinition enemy;
    [SerializeField, Min(0f)] private float unlockTime;
    [SerializeField, Min(0f)] private float weight = 1f;
    public EnemyDefinition Enemy => enemy;
    public float UnlockTime => unlockTime;
    public float Weight => weight;
}