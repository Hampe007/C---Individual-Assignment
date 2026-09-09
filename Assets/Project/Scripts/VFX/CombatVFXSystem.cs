using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

public sealed class CombatVFXSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem _bloodPrefab;
    [SerializeField, Min(1)] private int _maxBloodParticles = 128;
    [SerializeField] private float _bloodHeightOffset = 0.8f;

    private ObjectPool<ParticleSystem> _bloodPool;
    private readonly List<ParticleSystem> _activeBlood = new();

    private void Awake()
    {
        if (_bloodPrefab == null)
        {
            Debug.LogError("CombatVFXSystem requires a blood particle prefab.");
            enabled = false;
            return;
        }

        _bloodPool = new ObjectPool<ParticleSystem>(
            CreateBlood,
            GetBlood,
            ReleaseBlood,
            DestroyBlood,
            true,
            16,
            _maxBloodParticles);
    }

    private void Update()
    {
        for (int i = _activeBlood.Count - 1; i >= 0; i--)
        {
            ParticleSystem blood = _activeBlood[i];

            if (blood.IsAlive(true))
                continue;

            _activeBlood.RemoveAt(i);
            _bloodPool.Release(blood);
        }
    }

    internal void PlayBlood(float3 position)
    {
        ParticleSystem blood = _bloodPool.Get();

        blood.transform.position = new Vector3(
            position.x,
            position.y + _bloodHeightOffset,
            position.z);

        blood.Clear(true);
        blood.Play(true);

        _activeBlood.Add(blood);
    }

    private ParticleSystem CreateBlood()
    {
        return Instantiate(_bloodPrefab, transform);
    }

    private static void GetBlood(ParticleSystem blood)
    {
        blood.gameObject.SetActive(true);
    }

    private static void ReleaseBlood(ParticleSystem blood)
    {
        blood.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        blood.gameObject.SetActive(false);
    }

    private static void DestroyBlood(ParticleSystem blood)
    {
        if (blood != null)
            Destroy(blood.gameObject);
    }

    private void OnDestroy()
    {
        _bloodPool?.Clear();
        _activeBlood.Clear();
    }
}