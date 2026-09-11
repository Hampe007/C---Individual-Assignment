using UnityEngine;

public sealed class DifficultyDirector : MonoBehaviour
{
    [SerializeField] private HordeManager _hordeManager;

    [Header("Starting Pressure")]
    [SerializeField, Min(0)] private int _startingEnemies = 20;

    [Header("Difficulty Scaling")]
    [SerializeField, Min(1f)] private float _rampSeconds = 300f;
    [SerializeField, Min(0.1f)] private float _startSpawnInterval = 1.25f;
    [SerializeField, Min(0.1f)] private float _endSpawnInterval = 0.4f;
    [SerializeField, Min(1)] private int _startBatchSize = 2;
    [SerializeField, Min(1)] private int _endBatchSize = 10;

    [Header("Enemy Spawning")]
    [SerializeField] private EnemySpawnEntry[] _spawnEntries;

    private float _elapsed;
    private float _spawnTimer;
    private bool _started;

    private void Start()
    {
        if (_hordeManager != null && _spawnEntries != null && _spawnEntries.Length > 0)
            return;

        Debug.LogError("DifficultyDirector is missing required references.");
        enabled = false;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        
        if (!_hordeManager.IsReady)
            return;

        if (!_started)
        {
            _started = true;
            _spawnTimer = _startSpawnInterval;
            SpawnEnemies(_startingEnemies);
            return;
        }

        _elapsed += Time.deltaTime;

        if (_hordeManager.ActiveEnemyCount >= _hordeManager.MaxEnemyCount)
            return;

        _spawnTimer -= Time.deltaTime;

        if (_spawnTimer > 0f)
            return;

        float progress = Mathf.Clamp01(_elapsed / _rampSeconds);
        float interval = Mathf.Lerp(_startSpawnInterval, _endSpawnInterval, progress);
        int batchSize = Mathf.RoundToInt(Mathf.Lerp(_startBatchSize, _endBatchSize, progress));

        SpawnEnemies(batchSize);
        _spawnTimer = interval;
    }

    private void SpawnEnemies(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (_hordeManager.ActiveEnemyCount >= _hordeManager.MaxEnemyCount)
                return;

            EnemyDefinition enemy = GetEnemy();

            if (enemy == null)
                return;

            _hordeManager.TrySpawnEnemy(enemy);
        }
    }

    private EnemyDefinition GetEnemy()
    {
        float totalWeight = 0f;

        for (int i = 0; i < _spawnEntries.Length; i++)
        {
            EnemySpawnEntry entry = _spawnEntries[i];

            if (entry.Enemy != null && _elapsed >= entry.UnlockTime)
                totalWeight += entry.Weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);

        for (int i = 0; i < _spawnEntries.Length; i++)
        {
            EnemySpawnEntry entry = _spawnEntries[i];

            if (entry.Enemy == null || _elapsed < entry.UnlockTime)
                continue;

            roll -= entry.Weight;

            if (roll <= 0f)
                return entry.Enemy;
        }

        return null;
    }
}