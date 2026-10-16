using Unity.Mathematics;
using UnityEngine;

/// <summary>Automatically fires spectral bolts once purchased in the skill tree.</summary>
public sealed class SoulBoltWeapon : MonoBehaviour
{
    [SerializeField] private HordeManager hordeManager;
    [SerializeField] private CombatProjectileSystem projectiles;
    [SerializeField, Min(0.1f)] private float range = 14f;
    [SerializeField, Min(0.1f)] private float cooldown = 1.7f;
    [SerializeField, Min(0.1f)] private float speed = 15f;
    [SerializeField, Min(0.1f)] private float lifetime = 1.4f;
    [SerializeField, Min(1)] private int damage = 15;
    [SerializeField, Range(0f, 45f)] private float spreadDegrees = 14f;
    private bool unlocked;
    private int boltCount = 1;
    private int trackingTier;
    private float timer;
    public bool IsUnlocked => unlocked;
    public int BoltCount => boltCount;
    public int TrackingTier => trackingTier;

    public void Unlock()
    {
        unlocked = true;
        timer = 0f;
    }

    public void AddDamage(int amount)
    {
        damage += Mathf.Max(0, amount);
    }

    public void AddBolt()
    {
        boltCount = Mathf.Min(5, boltCount + 2);
    }

    public void AddTrackingTier()
    {
        trackingTier = Mathf.Min(3, trackingTier + 1);
    }

    private void Update()
    {
        if (!unlocked || hordeManager == null || projectiles == null || Time.timeScale <= 0f)
        {
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f)
        {
            return;
        }

        Vector3 origin = transform.position + Vector3.up * 1.1f;
        if (!hordeManager.TryGetClosestEnemy((float3)transform.position, range, out _, out float3 target))
        {
            return;
        }

        Vector3 direction = new Vector3(target.x - origin.x, 0f, target.z - origin.z).normalized;
        for (int i = 0; i < boltCount; i++)
        {
            // Keep the first bolt aimed at the target as extra side bolts are added.
            float angle = i == 0 ? 0f : ((i + 1) / 2) * (i % 2 == 1 ? -spreadDegrees : spreadDegrees);
            projectiles.SpawnSoulBolt(origin, Quaternion.Euler(0f, angle, 0f) * direction, damage, speed, lifetime, trackingTier);
        }

        timer = cooldown;
    }
}