using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>Pooled ground hazards and a single refreshing poison status per actor.</summary>
public sealed class PoisonAreaSystem : MonoBehaviour
{
    [SerializeField] private HordeManager hordeManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject puddlePrefab;
    [SerializeField] private ParticleSystem statusParticlesPrefab;
    [SerializeField, Min(0.1f)] private float radius = 1.5f;
    [SerializeField, Min(0.1f)] private float puddleLifetime = 5f;
    [SerializeField, Min(0.1f)] private float lingerDuration = 3f;
    [SerializeField, Min(0.1f)] private float tickInterval = 1f;
    [SerializeField, Min(1)] private int damagePerTick = 2;
    [SerializeField, Min(1)] private int maxPuddles = 64;
    private struct Puddle
    {
        public GameObject View;
        public float Remaining;
    }

    private readonly List<Puddle> active = new();
    private ObjectPool<GameObject> pool;
    private ParticleSystem playerParticles;
    private float playerPoisonRemaining;
    private float playerTickTimer;
    private void Awake()
    {
        if (hordeManager == null || playerHealth == null || player == null || puddlePrefab == null || statusParticlesPrefab == null)
        {
            Debug.LogError("PoisonAreaSystem is missing a required reference.", this);
            enabled = false;
            return;
        }

        pool = new ObjectPool<GameObject>(() => Instantiate(puddlePrefab, transform), view => view.SetActive(true), view => view.SetActive(false), view =>
        {
            if (view != null)
            {
                Destroy(view);
            }
        }, true, 8, maxPuddles);
        playerParticles = Instantiate(statusParticlesPrefab, player);
        playerParticles.name = "PoisonStatus";
        playerParticles.transform.localPosition = Vector3.up * 0.7f;
        playerParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        playerHealth.Died += Clear;
    }

    public void SpawnPuddle(Vector3 position)
    {
        if (!isActiveAndEnabled || pool == null || playerHealth.CurrentHealth <= 0)
        {
            return;
        }

        if (active.Count >= maxPuddles)
        {
            ReleaseAt(0);
        }

        GameObject view = pool.Get();
        view.transform.position = new Vector3(position.x, 0.03f, position.z);
        // The authored mesh has unit radius; keep its XZ footprint aligned with damage.
        view.transform.localScale = new Vector3(radius, 1f, radius);
        active.Add(new Puddle { View = view, Remaining = puddleLifetime });
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f || playerHealth.CurrentHealth <= 0 || !hordeManager.IsReady)
        {
            return;
        }

        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        bool playerInside = false;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Puddle puddle = active[i];
            Vector3 position = puddle.View.transform.position;
            Vector2 offset = new(player.position.x - position.x, player.position.z - position.z);
            playerInside |= offset.sqrMagnitude <= radius * radius;
            hordeManager.ApplyPoisonInRadius(position, radius, lingerDuration + deltaTime, tickInterval);
            puddle.Remaining -= deltaTime;
            if (puddle.Remaining <= 0f)
            {
                ReleaseAt(i);
            }
            else
            {
                active[i] = puddle;
            }
        }

        if (playerInside)
        {
            if (playerPoisonRemaining <= 0f)
            {
                playerTickTimer = tickInterval;
            }

            playerPoisonRemaining = lingerDuration + deltaTime;
        }

        hordeManager.TickPoison(deltaTime, damagePerTick, tickInterval, statusParticlesPrefab);
        if (playerPoisonRemaining > 0f)
        {
            playerTickTimer -= Mathf.Min(deltaTime, playerPoisonRemaining);
            playerPoisonRemaining = Mathf.Max(0f, playerPoisonRemaining - deltaTime);
            int ticks = 0;
            while (playerTickTimer <= 0f)
            {
                ticks++;
                playerTickTimer += tickInterval;
            }

            if (ticks > 0)
            {
                playerHealth.TakePoisonDamage(damagePerTick * ticks);
            }
        }

        if (playerPoisonRemaining > 0f && !playerParticles.isPlaying)
        {
            playerParticles.Play(true);
        }
        else if (playerPoisonRemaining <= 0f)
        {
            playerParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void ReleaseAt(int index)
    {
        pool.Release(active[index].View);
        active.RemoveAt(index);
    }

    private void Clear()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            ReleaseAt(i);
        }

        playerPoisonRemaining = 0f;
        playerTickTimer = 0f;
        if (playerParticles != null)
        {
            playerParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (hordeManager != null)
        {
            hordeManager.ClearPoison();
        }
    }

    private void OnDisable() => Clear();
    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= Clear;
        }

        if (playerParticles != null)
        {
            Destroy(playerParticles.gameObject);
        }

        pool?.Clear();
    }
}