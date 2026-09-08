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

    [Header("Behaviour Unlocks")]
    [SerializeField, Min(0f)] private float _chargerUnlockTime = 45f;
    [SerializeField, Range(0f, 1f)] private float _chargerChance = 0.2f;
    [SerializeField, Min(0f)] private float _bruteUnlockTime = 90f;
    [SerializeField, Range(0f, 1f)] private float _bruteChance = 0.1f;

    private float _elapsed;
    private float _spawnTimer;
    private bool _started;

    private void Start()
    {
        if (_hordeManager != null)
            return;

        Debug.LogError("DifficultyDirector requires a HordeManager.");
        enabled = false;
    }

    private void Update()
    {
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

            _hordeManager.TrySpawnEnemy(GetBehaviour());
        }
    }

    private EnemyBehaviourType GetBehaviour()
    {
        float roll = Random.value;

        if (_elapsed >= _bruteUnlockTime)
        {
            if (roll < _bruteChance)
                return EnemyBehaviourType.Brute;

            if (roll < _bruteChance + _chargerChance)
                return EnemyBehaviourType.Charger;

            return EnemyBehaviourType.Swarm;
        }

        if (_elapsed >= _chargerUnlockTime && roll < _chargerChance)
            return EnemyBehaviourType.Charger;

        return EnemyBehaviourType.Swarm;
    }
}