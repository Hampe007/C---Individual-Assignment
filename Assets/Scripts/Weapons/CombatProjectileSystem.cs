using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>Moves pooled combat projectiles with swept hits and optional forward tracking.</summary>
public sealed class CombatProjectileSystem : MonoBehaviour
{
    [SerializeField] private HordeManager hordeManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject soulBoltPrefab;
    [SerializeField] private GameObject spitPrefab;
    [SerializeField] private ParticleSystem impactPrefab;
    [SerializeField, Min(1)] private int maxPlayerProjectiles = 48;
    [SerializeField, Min(1)] private int maxEnemyProjectiles = 128;
    [SerializeField, Min(0.1f)] private float playerHitRadius = 0.65f;
    [SerializeField] private PoisonAreaSystem poisonAreas;
    [Header("Impact Audio")]
    [SerializeField] private SoundSet soulImpactSound;
    [SerializeField] private SoundSet spitImpactSound;
    [Header("Tracking Tiers 1 / 2 / 3")]
    [SerializeField] private Vector3 trackingRadii = new(1.5f, 3f, 5f);
    [SerializeField] private Vector3 trackingHalfAngles = new(20f, 40f, 60f);
    [SerializeField] private Vector3 trackingTurnSpeeds = new(30f, 70f, 120f);
    private readonly List<Projectile> active = new();
    private ObjectPool<GameObject> soulPool;
    private ObjectPool<GameObject> spitPool;
    private ObjectPool<ParticleSystem> impactPool;
    private int playerCount;
    private int enemyCount;
    private struct Projectile
    {
        public GameObject View;
        public Vector3 Direction;
        public Vector3 OriginalDirection;
        public float Speed;
        public float Lifetime;
        public float Radius;
        public int Damage;
        public bool FromPlayer;
        public bool LeavesPoison;
        public int TrackingTier;
    }

    private void Awake()
    {
        if (hordeManager == null || playerHealth == null || player == null || soulBoltPrefab == null || spitPrefab == null || impactPrefab == null)
        {
            Debug.LogError("CombatProjectileSystem is missing a required reference.", this);
            enabled = false;
            return;
        }

        soulPool = CreatePool(soulBoltPrefab, maxPlayerProjectiles);
        spitPool = CreatePool(spitPrefab, maxEnemyProjectiles);
        impactPool = new ObjectPool<ParticleSystem>(() => Instantiate(impactPrefab, transform), effect => effect.gameObject.SetActive(true), effect => effect.gameObject.SetActive(false), effect =>
        {
            if (effect != null)
            {
                Destroy(effect.gameObject);
            }
        }, true, 8, 64);
    }

    private ObjectPool<GameObject> CreatePool(GameObject prefab, int max)
    {
        return new ObjectPool<GameObject>(() => Instantiate(prefab, transform), view => view.SetActive(true), view => view.SetActive(false), view =>
        {
            if (view != null)
            {
                Destroy(view);
            }
        }, true, Mathf.Min(16, max), max);
    }

    public void SpawnSoulBolt(Vector3 origin, Vector3 direction, int damage, float speed, float lifetime)
    {
        SpawnSoulBolt(origin, direction, damage, speed, lifetime, 0);
    }

    public void SpawnSoulBolt(Vector3 origin, Vector3 direction, int damage, float speed, float lifetime, int trackingTier)
    {
        Spawn(origin, direction, damage, speed, lifetime, 0.42f, true, 1f, false, trackingTier);
    }

    public void SpawnSpit(Vector3 origin, Vector3 direction, int damage, float speed, float lifetime)
    {
        SpawnSpit(origin, direction, damage, speed, lifetime, 1f, false);
    }

    public void SpawnSpit(Vector3 origin, Vector3 direction, int damage, float speed, float lifetime, float scale, bool leavesPoison)
    {
        Spawn(origin, direction, damage, speed, lifetime, 0.45f * scale, false, scale, leavesPoison, 0);
    }

    private void Spawn(Vector3 origin, Vector3 direction, int damage, float speed, float lifetime, float radius, bool fromPlayer, float scale, bool leavesPoison, int trackingTier)
    {
        direction.y = 0f;
        if (!isActiveAndEnabled || damage <= 0 || direction.sqrMagnitude < 0.001f || (fromPlayer ? playerCount >= maxPlayerProjectiles : enemyCount >= maxEnemyProjectiles))
        {
            return;
        }

        GameObject view = (fromPlayer ? soulPool : spitPool).Get();
        view.transform.position = origin;
        view.transform.localScale = (fromPlayer ? soulBoltPrefab : spitPrefab).transform.localScale * scale;
        if (view.TryGetComponent(out TrailRenderer trail))
        {
            trail.Clear();
        }

        active.Add(new Projectile { View = view, Direction = direction.normalized, OriginalDirection = direction.normalized, Speed = speed, Lifetime = lifetime, Radius = radius, Damage = damage, FromPlayer = fromPlayer, LeavesPoison = leavesPoison, TrackingTier = Mathf.Clamp(trackingTier, 0, 3) });
        if (fromPlayer)
        {
            playerCount++;
        }
        else
        {
            enemyCount++;
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || playerHealth.CurrentHealth <= 0)
        {
            return;
        }

        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Projectile projectile = active[i];
            Vector3 start = projectile.View.transform.position;
            Track(ref projectile, start, deltaTime);
            Vector3 end = start + projectile.Direction * (projectile.Speed * Mathf.Min(deltaTime, projectile.Lifetime));
            projectile.Lifetime -= deltaTime;
            bool wallHit = !hordeManager.IsProjectileSegmentClear(start, end);
            if (wallHit)
            {
                end = hordeManager.GetProjectileWallImpact(start, end);
            }

            Vector3 impact;
            bool hit = projectile.FromPlayer ? HitEnemy(projectile, start, end, out impact) : HitPlayer(projectile, start, end, out impact);
            if (hit || wallHit || projectile.Lifetime <= 0f)
            {
                Vector3 position = hit ? impact : end;
                if (hit || wallHit)
                {
                    PlayImpact(position, projectile.FromPlayer);
                }

                if (projectile.LeavesPoison && poisonAreas != null)
                {
                    poisonAreas.SpawnPuddle(position);
                }

                ReleaseAt(i);
                continue;
            }

            projectile.View.transform.position = end;
            active[i] = projectile;
        }
    }

    private void Track(ref Projectile projectile, Vector3 position, float deltaTime)
    {
        if (!projectile.FromPlayer || projectile.TrackingTier == 0)
        {
            return;
        }

        int tier = projectile.TrackingTier - 1;
        float halfAngle = Mathf.Clamp(trackingHalfAngles[tier], 0f, 89f);
        if (!hordeManager.TryGetTrackingTarget(position, projectile.Direction, projectile.OriginalDirection, trackingRadii[tier], halfAngle, out Vector3 target))
        {
            return;
        }

        Vector3 desired = target - position;
        desired.y = 0f;
        projectile.Direction = Vector3.RotateTowards(projectile.Direction, desired.normalized, Mathf.Max(0f, trackingTurnSpeeds[tier]) * Mathf.Deg2Rad * deltaTime, 0f).normalized;
    }

    private bool HitEnemy(Projectile projectile, Vector3 start, Vector3 end, out Vector3 impact)
    {
        impact = end;
        if (!hordeManager.TryGetEnemyOnSegment((float3)start, (float3)end, projectile.Radius, out int index, out float progress))
        {
            return false;
        }

        impact = Vector3.Lerp(start, end, progress);
        hordeManager.DamageEnemy(index, projectile.Damage);
        return true;
    }

    private bool HitPlayer(Projectile projectile, Vector3 start, Vector3 end, out Vector3 impact)
    {
        impact = end;
        Vector2 from = new(start.x, start.z);
        Vector2 to = new(end.x, end.z);
        Vector2 player = new(this.player.position.x, this.player.position.z);
        float radius = playerHitRadius + projectile.Radius;
        if (!TryCircleHit(from, to, player, radius, out float progress))
        {
            return false;
        }

        impact = Vector3.Lerp(start, end, progress);
        playerHealth.TakeDamage(projectile.Damage);
        return true;
    }

    internal static bool TryCircleHit(Vector2 from, Vector2 to, Vector2 center, float radius, out float progress)
    {
        progress = 0f;
        Vector2 offset = from - center;
        float c = offset.sqrMagnitude - radius * radius;
        if (c <= 0f)
        {
            return true;
        }

        Vector2 path = to - from;
        float a = path.sqrMagnitude;
        if (a <= 0.000001f)
        {
            return false;
        }

        float b = Vector2.Dot(offset, path);
        float discriminant = b * b - a * c;
        if (discriminant < 0f)
        {
            return false;
        }

        progress = (-b - Mathf.Sqrt(discriminant)) / a;
        return progress >= 0f && progress <= 1f;
    }

    private void PlayImpact(Vector3 position, bool fromPlayer)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(fromPlayer ? soulImpactSound : spitImpactSound, position);
        }

        ParticleSystem effect = impactPool.Get();
        var main = effect.main;
        main.startColor = fromPlayer ? new Color(0.25f, 0.8f, 1f) : new Color(0.65f, 1f, 0.15f);
        effect.transform.position = position;
        effect.Play(true);
        StartCoroutine(ReleaseImpact(effect));
    }

    private System.Collections.IEnumerator ReleaseImpact(ParticleSystem effect)
    {
        yield return new WaitForSeconds(effect.main.duration + effect.main.startLifetime.constantMax);
        if (effect != null)
        {
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            impactPool.Release(effect);
        }
    }

    private void ReleaseAt(int index)
    {
        Projectile projectile = active[index];
        if (projectile.View.TryGetComponent(out TrailRenderer trail))
        {
            trail.Clear();
        }

        (projectile.FromPlayer ? soulPool : spitPool).Release(projectile.View);
        if (projectile.FromPlayer)
        {
            playerCount--;
        }
        else
        {
            enemyCount--;
        }

        active.RemoveAt(index);
    }

    private void OnDestroy()
    {
        active.Clear();
        soulPool?.Clear();
        spitPool?.Clear();
        impactPool?.Clear();
    }
}