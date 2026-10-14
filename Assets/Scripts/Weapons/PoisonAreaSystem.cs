using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>Pooled ground hazards and a single refreshing poison status per actor.</summary>
public sealed class PoisonAreaSystem : MonoBehaviour
{
    [SerializeField] private HordeManager _hordeManager;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private Transform _player;
    [SerializeField] private GameObject _puddlePrefab;
    [SerializeField] private ParticleSystem _statusParticlesPrefab;
    [SerializeField, Min(0.1f)] private float _radius = 1.5f;
    [SerializeField, Min(0.1f)] private float _puddleLifetime = 5f;
    [SerializeField, Min(0.1f)] private float _lingerDuration = 3f;
    [SerializeField, Min(0.1f)] private float _tickInterval = 1f;
    [SerializeField, Min(1)] private int _damagePerTick = 2;
    [SerializeField, Min(1)] private int _maxPuddles = 64;

    private struct Puddle
    {
        public GameObject View;
        public float Remaining;
    }

    private readonly List<Puddle> _active = new();
    private ObjectPool<GameObject> _pool;
    private ParticleSystem _playerParticles;
    private float _playerPoisonRemaining;
    private float _playerTickTimer;

    private void Awake()
    {
        if (_hordeManager == null || _playerHealth == null || _player == null ||
            _puddlePrefab == null || _statusParticlesPrefab == null)
        {
            Debug.LogError("PoisonAreaSystem is missing a required reference.", this);
            enabled = false;
            return;
        }
        _pool = new ObjectPool<GameObject>(() => Instantiate(_puddlePrefab, transform),
            view => view.SetActive(true), view => view.SetActive(false),
            view => { if (view != null) Destroy(view); }, true, 8, _maxPuddles);
        _playerParticles = Instantiate(_statusParticlesPrefab, _player);
        _playerParticles.name = "PoisonStatus";
        _playerParticles.transform.localPosition = Vector3.up * 0.7f;
        _playerParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _playerHealth.Died += Clear;
    }

    public void SpawnPuddle(Vector3 position)
    {
        if (!isActiveAndEnabled || _pool == null || _playerHealth.CurrentHealth <= 0) return;
        if (_active.Count >= _maxPuddles) ReleaseAt(0);
        GameObject view = _pool.Get();
        view.transform.position = new Vector3(position.x, 0.03f, position.z);
        // The authored mesh has unit radius; keep its XZ footprint aligned with damage.
        view.transform.localScale = new Vector3(_radius, 1f, _radius);
        _active.Add(new Puddle { View = view, Remaining = _puddleLifetime });
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f || _playerHealth.CurrentHealth <= 0 || !_hordeManager.IsReady) return;
        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        bool playerInside = false;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Puddle puddle = _active[i];
            Vector3 position = puddle.View.transform.position;
            Vector2 offset = new(_player.position.x - position.x, _player.position.z - position.z);
            playerInside |= offset.sqrMagnitude <= _radius * _radius;
            _hordeManager.ApplyPoisonInRadius(position, _radius, _lingerDuration + deltaTime, _tickInterval);
            puddle.Remaining -= deltaTime;
            if (puddle.Remaining <= 0f) ReleaseAt(i);
            else _active[i] = puddle;
        }
        if (playerInside)
        {
            if (_playerPoisonRemaining <= 0f) _playerTickTimer = _tickInterval;
            _playerPoisonRemaining = _lingerDuration + deltaTime;
        }
        _hordeManager.TickPoison(deltaTime, _damagePerTick, _tickInterval, _statusParticlesPrefab);
        if (_playerPoisonRemaining > 0f)
        {
            _playerTickTimer -= Mathf.Min(deltaTime, _playerPoisonRemaining);
            _playerPoisonRemaining = Mathf.Max(0f, _playerPoisonRemaining - deltaTime);
            int ticks = 0;
            while (_playerTickTimer <= 0f)
            {
                ticks++;
                _playerTickTimer += _tickInterval;
            }
            if (ticks > 0) _playerHealth.TakePoisonDamage(_damagePerTick * ticks);
        }
        if (_playerPoisonRemaining > 0f && !_playerParticles.isPlaying) _playerParticles.Play(true);
        else if (_playerPoisonRemaining <= 0f)
            _playerParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void ReleaseAt(int index)
    {
        _pool.Release(_active[index].View);
        _active.RemoveAt(index);
    }

    private void Clear()
    {
        for (int i = _active.Count - 1; i >= 0; i--) ReleaseAt(i);
        _playerPoisonRemaining = 0f;
        _playerTickTimer = 0f;
        if (_playerParticles != null) _playerParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_hordeManager != null) _hordeManager.ClearPoison();
    }

    private void OnDisable() => Clear();

    private void OnDestroy()
    {
        if (_playerHealth != null) _playerHealth.Died -= Clear;
        if (_playerParticles != null) Destroy(_playerParticles.gameObject);
        _pool?.Clear();
    }
}
