using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

public sealed class CombatVFXSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem bloodPrefab;
    [SerializeField, Min(1)] private int maxBloodParticles = 128;
    [SerializeField] private float bloodHeightOffset = 0.8f;
    private ObjectPool<ParticleSystem> bloodPool;
    private readonly List<ParticleSystem> activeBlood = new();
    private void Awake()
    {
        if (bloodPrefab == null)
        {
            Debug.LogError("CombatVFXSystem requires a blood particle prefab.");
            enabled = false;
            return;
        }

        bloodPool = new ObjectPool<ParticleSystem>(CreateBlood, GetBlood, ReleaseBlood, DestroyBlood, true, 16, maxBloodParticles);
    }

    private void Update()
    {
        for (int i = activeBlood.Count - 1; i >= 0; i--)
        {
            ParticleSystem blood = activeBlood[i];
            if (blood.IsAlive(true))
            {
                continue;
            }

            activeBlood.RemoveAt(i);
            bloodPool.Release(blood);
        }
    }

    internal void PlayBlood(float3 position)
    {
        ParticleSystem blood = bloodPool.Get();
        blood.transform.position = new Vector3(position.x, position.y + bloodHeightOffset, position.z);
        blood.Clear(true);
        blood.Play(true);
        activeBlood.Add(blood);
    }

    private ParticleSystem CreateBlood()
    {
        return Instantiate(bloodPrefab, transform);
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
        {
            Destroy(blood.gameObject);
        }
    }

    private void OnDestroy()
    {
        bloodPool?.Clear();
        activeBlood.Clear();
    }
}