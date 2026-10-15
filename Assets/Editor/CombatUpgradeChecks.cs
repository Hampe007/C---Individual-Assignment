using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

/// <summary>Deterministic integration checks against the wired Game scene. Run in disposable Play Mode.</summary>
public static class CombatUpgradeChecks
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static T Read<T>(object obj, string field) => (T)obj.GetType().GetField(field, Fields).GetValue(obj);
    private static void Write(object obj, string field, object value) => obj.GetType().GetField(field, Fields).SetValue(obj, value);
    private static object Call(object obj, string method, params object[] arguments) =>
        obj.GetType().GetMethods(Fields).Single(m => m.Name == method && m.GetParameters().Length == arguments.Length).Invoke(obj, arguments);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [MenuItem("Tools/Combat/Run Upgrade Checks (Play Mode)")]
    private static void RunMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (!Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game")
            throw new InvalidOperationException("Open Game and enter Play Mode first. Checks alter only disposable runtime state.");
        Time.timeScale = 0f;
        var horde = UnityEngine.Object.FindFirstObjectByType<HordeManager>();
        var projectiles = UnityEngine.Object.FindFirstObjectByType<CombatProjectileSystem>();
        var poison = UnityEngine.Object.FindFirstObjectByType<PoisonAreaSystem>();
        var health = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
        var progression = UnityEngine.Object.FindFirstObjectByType<PlayerProgression>();
        var skills = UnityEngine.Object.FindFirstObjectByType<SkillTreeProgression>();
        var weapon = UnityEngine.Object.FindFirstObjectByType<SoulBoltWeapon>();
        progression.enabled = false; // Keep the checks from awarding XP or saving a high score.
        var passed = new List<string>();
        NativeArray<EnemyRuntime> Enemies() => Read<NativeArray<EnemyRuntime>>(horde, "_enemies");
        void Move(int index, Vector3 position)
        {
            var enemies = Enemies(); var next = Read<NativeArray<EnemyRuntime>>(horde, "_nextEnemies");
            var enemy = enemies[index]; enemy.Position = position; enemies[index] = enemy; next[index] = enemy;
        }
        void ClearEnemies()
        {
            for (int i = horde.ActiveEnemyCount - 1; i >= 0; i--) Call(horde, "DamageEnemy", i, 100000);
        }
        IList Bolts() => Read<IList>(projectiles, "_active");
        IList Puddles() => Read<IList>(poison, "_active");
        void ClearBolts() { while (Bolts().Count > 0) Call(projectiles, "ReleaseAt", Bolts().Count - 1); }
        var grid = Read<HordeNavigation>(horde, "_navigation").Grid;
        Vector3 center = default; bool found = false;
        for (int y = 10; y < grid.Size.y - 10 && !found; y++)
        {
            for (int x = 10; x < grid.Size.x - 10 && !found; x++)
            {
                float2 p = grid.Origin + new float2(x + 0.5f, y + 0.5f) * grid.CellSize;
                bool clear = true;
                for (int dz = -6; dz <= 6 && clear; dz++)
                    for (int dx = -6; dx <= 6 && clear; dx++) clear &= grid.IsReachable(p + new float2(dx, dz));
                if (clear) { center = new Vector3(p.x, 0f, p.y); found = true; }
            }
        }
        Check(found, "No open test area found.");
        var imp = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Data/Enemies/Enemy_Imp_T1.asset");
        var puglin = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Data/Enemies/Enemy_Puglin_T1.asset");
        void Spawn(EnemyDefinition definition, Vector3 position) => Check(horde.TrySpawnEnemyAt(definition, position), "Enemy spawn failed.");
        ClearEnemies(); ClearBolts(); Call(poison, "Clear");
        health.transform.position = center + Vector3.right * 10f;
        Write(health, "_currentHealth", 100);

        // Real job-safe volley requests verify the fan order, not just authoring values.
        foreach (int tier in new[] { 1, 2, 3 })
        {
            var definition = AssetDatabase.LoadAssetAtPath<SpitterEnemyDefinition>($"Assets/Data/Enemies/Enemy_Puglin_T{tier}.asset");
            var c = definition.CreateConfig();
            var queue = Read<NativeQueue<EnemySpitEvent>>(horde, "_spitEvents");
            queue.Enqueue(new EnemySpitEvent { Origin = center + Vector3.up, Target = center + Vector3.forward * 8f,
                Count = c.ProjectileCount, Spread = c.ProjectileSpread, Damage = c.Damage, Speed = c.ProjectileSpeed,
                Lifetime = c.ProjectileLifetime, SmallOrbScale = c.SmallOrbScale, MediumOrbScale = c.MediumOrbScale,
                LargeOrbScale = c.LargeOrbScale, LeavesPoison = c.LeavesPoison });
            Call(horde, "ApplySpitEvents");
            float[] expected = tier == 1 ? new[] { 1f } : tier == 2 ? new[] { 0.65f, 1.4f, 0.65f } : new[] { 0.65f, 1f, 1.4f, 1f, 0.65f };
            Check(Bolts().Count == expected.Length, "Wrong volley count.");
            var prefab = Read<GameObject>(projectiles, "_spitPrefab");
            for (int i = 0; i < expected.Length; i++)
            {
                object bolt = Bolts()[i]; var view = Read<GameObject>(bolt, "View");
                Check(Mathf.Abs(view.transform.localScale.x / prefab.transform.localScale.x - expected[i]) < 0.001f, "Wrong orb size order.");
                Check(Mathf.Abs(Read<float>(bolt, "Radius") - 0.45f * expected[i]) < 0.001f, "Wrong orb hit radius.");
                Check(Read<bool>(bolt, "LeavesPoison") == (tier > 1 && i == expected.Length / 2), "Wrong poison orb.");
                float angle = Vector3.SignedAngle(Vector3.forward, Read<Vector3>(bolt, "Direction"), Vector3.up);
                Check(Mathf.Abs(angle - (i - (expected.Length - 1) * 0.5f) * 14f) < 0.01f, "Wrong fan direction.");
            }
            Call(projectiles, "Tick", c.ProjectileLifetime);
            Check(Puddles().Count == (tier > 1 ? 1 : 0), "Expiration must create exactly one eligible puddle.");
            ClearBolts(); Call(poison, "Clear");
        }
        passed.Add("Puglin 1/3/5 fans, sizes, hit radii, poison selection and expiry");

        // Verify actual purchases, prerequisites and spending rather than direct setters.
        Write(progression, "_skillPoints", 10);
        Check(!skills.CanPurchase("soul_bolt_count") && !skills.CanPurchase("soul_bolt_tracking"), "Locked weapon upgrades became available.");
        Check(skills.TryPurchase("soul_bolt_unlock") && weapon.BoltCount == 1, "Weapon unlock failed.");
        Check(skills.GetLevel("soul_bolt_count") == 0 && skills.GetLevel("soul_bolt_tracking") == 0,
            "New upgrades started above level zero.");
        Check(skills.TryPurchase("soul_bolt_count") && weapon.BoltCount == 3, "First volley purchase failed.");
        Check(skills.TryPurchase("soul_bolt_count") && weapon.BoltCount == 5, "Second volley purchase failed.");
        for (int tier = 1; tier <= 3; tier++) Check(skills.TryPurchase("soul_bolt_tracking") && weapon.TrackingTier == tier, "Tracking purchase failed.");
        Check(progression.SkillPoints == 4 && skills.IsMaxed("soul_bolt_count") &&
              skills.IsMaxed("soul_bolt_tracking") &&
              !skills.TryPurchase("soul_bolt_count") && !skills.TryPurchase("soul_bolt_tracking"),
            "Upgrade caps or skill point spending failed.");
        weapon.AddBolt(); weapon.AddTrackingTier(); Check(weapon.BoltCount == 5 && weapon.TrackingTier == 3, "Upgrade caps failed.");
        Spawn(imp, center + Vector3.forward * 2f); health.transform.position = center;
        Time.timeScale = 1f; Call(weapon, "Update"); Time.timeScale = 0f;
        Check(Bolts().Count == 5 && Bolts().Cast<object>().All(b => Read<int>(b, "TrackingTier") == 3), "Purchased upgrades did not reach fired bolts.");
        ClearBolts(); ClearEnemies(); health.transform.position = center + Vector3.right * 10f;
        passed.Add("Purchase prerequisites, 1/3/5 bolts, three tracking tiers, costs and caps");

        Spawn(imp, center + new Vector3(0.2f, 0f, 1.2f));
        float[] turns = new float[3];
        for (int tier = 1; tier <= 3; tier++)
        {
            projectiles.SpawnSoulBolt(center + Vector3.up, Vector3.forward, 1, 0.1f, 5f, tier);
            Call(projectiles, "Tick", 0.05f);
            turns[tier - 1] = Vector3.Angle(Vector3.forward, Read<Vector3>(Bolts()[0], "Direction"));
            ClearBolts();
        }
        Check(turns[0] > 0f && turns[1] > turns[0] && turns[2] > turns[1], "Tracking tiers do not turn progressively.");
        Move(0, center - Vector3.forward);
        projectiles.SpawnSoulBolt(center + Vector3.up, Vector3.forward, 1, 1f, 2f, 3); Call(projectiles, "Tick", 0.1f);
        Check(Read<Vector3>(Bolts()[0], "Direction") == Vector3.forward, "Tracking made a U-turn."); ClearBolts();
        Move(0, center + new Vector3(0.3f, 0f, 2.5f));
        projectiles.SpawnSoulBolt(center + Vector3.up, Vector3.forward, 1, 1f, 2f, 1); Call(projectiles, "Tick", 0.1f);
        Check(Read<Vector3>(Bolts()[0], "Direction") == Vector3.forward, "Tier 1 exceeded acquisition range."); ClearBolts();
        foreach (int tier in new[] { 2, 3 })
        {
            float range = tier == 2 ? 2.5f : 4.5f;
            Move(0, center + new Vector3(range * 0.5f, 0f, range * 0.866f));
            projectiles.SpawnSoulBolt(center + Vector3.up, Vector3.forward, 1, 0f, 2f, tier); Call(projectiles, "Tick", 0.05f);
            Check(Read<Vector3>(Bolts()[0], "Direction").x > 0f, "Higher tracking tier did not acquire farther enemies."); ClearBolts();
        }
        Move(0, center + new Vector3(1f, 0f, 1f));
        projectiles.SpawnSoulBolt(center + Vector3.up, Vector3.forward, 1, 0f, 2f, 3); Call(projectiles, "Tick", 0.1f);
        Vector3 oneStep = Read<Vector3>(Bolts()[0], "Direction"); ClearBolts();
        projectiles.SpawnSoulBolt(center + Vector3.up, Vector3.forward, 1, 0f, 2f, 3);
        for (int i = 0; i < 4; i++) Call(projectiles, "Tick", 0.025f);
        Check(Vector3.Distance(oneStep, Read<Vector3>(Bolts()[0], "Direction")) < 0.0001f, "Tracking depends on frame rate.");
        ClearEnemies(); Call(projectiles, "Tick", 0.1f);
        Check(Vector3.Distance(oneStep, Read<Vector3>(Bolts()[0], "Direction")) < 0.0001f, "Dead target changed tracking direction.");
        ClearBolts(); Spawn(imp, center + new Vector3(0.3f, 0f, 2.5f));
        int2 wall = grid.Cell(new float2(center.x, center.z + 1f)); int wallIndex = wall.y * grid.Size.x + wall.x;
        byte previousBlocked = grid.Blocked[wallIndex];
        try
        {
            grid.Blocked[wallIndex] = 1;
            projectiles.SpawnSoulBolt(center + Vector3.up, Vector3.forward, 1, 1f, 2f, 3); Call(projectiles, "Tick", 0.1f);
            Check(Read<Vector3>(Bolts()[0], "Direction") == Vector3.forward, "Tracking targeted through a wall."); ClearBolts();
            projectiles.SpawnSpit(center + Vector3.up, Vector3.forward, 4, 10f, 2f, 1.4f, true); Call(projectiles, "Tick", 0.2f);
            Check(Bolts().Count == 0 && Puddles().Count == 1, "Wall impact did not leave one puddle.");
            Check(Read<GameObject>(Puddles()[0], "View").transform.position.z < center.z + 1.5f, "Puddle overshot the wall.");
        }
        finally { grid.Blocked[wallIndex] = previousBlocked; ClearBolts(); Call(poison, "Clear"); }
        ClearEnemies(); health.transform.position = center + Vector3.forward * 2f;
        projectiles.SpawnSpit(center + Vector3.up, Vector3.forward, 4, 10f, 2f, 1.4f, true); Call(projectiles, "Tick", 0.2f);
        Check(Bolts().Count == 0 && Puddles().Count == 1 && Read<GameObject>(Puddles()[0], "View").transform.position.z < health.transform.position.z, "Player impact missed or puddle overshot.");
        Call(poison, "Clear"); Write(health, "_currentHealth", 100);
        passed.Add("Progressive tracking, range, frame-rate independence, target death, forward cone, wall occlusion and swept impacts");

        ClearEnemies(); Spawn(imp, center); Spawn(puglin, center + Vector3.right * 0.2f);
        int impHealth = Enemies()[0].Health; int pugHealth = Enemies()[1].Health;
        health.transform.position = center; Write(progression, "_currentXP", 5);
        Write(health, "_nextDamageTime", Time.time + 100f);
        poison.SpawnPuddle(center); poison.SpawnPuddle(center);
        Call(poison, "Tick", 0.5f); Check(health.CurrentHealth == 100, "Poison ticked immediately.");
        Call(poison, "Tick", 0.5f);
        Check(health.CurrentHealth == 98 && Enemies()[0].Health == impHealth - 2 && Enemies()[1].Health == pugHealth, "Poison damage, overlap or Puglin immunity failed.");
        Check(progression.CurrentXP == 5 && Read<float>(health, "_nextDamageTime") > Time.time + 90f, "Poison consumed XP or hit protection.");
        Check(Read<ParticleSystem>(poison, "_playerParticles").isPlaying, "Player poison particles missing.");
        health.transform.position = center + Vector3.right * 10f; Move(0, center + Vector3.right * 10f);
        for (int i = 0; i < 3; i++) Call(poison, "Tick", 1f);
        Check(health.CurrentHealth == 92 && Enemies()[0].Health == impHealth - 8, "Three-second lingering damage failed.");
        Check(!Read<ParticleSystem>(poison, "_playerParticles").isPlaying, "Poison particles persisted after expiry.");
        Call(poison, "Tick", 1f); Check(health.CurrentHealth == 92 && Puddles().Count == 0, "Poison or puddle exceeded lifetime.");
        poison.SpawnPuddle(center); health.transform.position = center; Call(poison, "Tick", 0.5f);
        health.transform.position = center + Vector3.right * 10f; Call(poison, "Tick", 0.5f);
        health.transform.position = center; Call(poison, "Tick", 0.5f);
        Check(Mathf.Abs(Read<float>(poison, "_playerPoisonRemaining") - 3f) < 0.001f && health.CurrentHealth == 90, "Re-entry did not refresh a single poison status.");
        passed.Add("Delayed ticks, overlap refresh, lingering duration, particles, Puglin immunity and separate player damage rules");

        Call(poison, "Clear"); ClearEnemies(); Spawn(imp, center); Spawn(imp, center); Spawn(puglin, center);
        Call(horde, "ApplyPoisonInRadius", center, 1.5f, 3f, 1f);
        Call(horde, "DamageEnemy", 0, 100000);
        Check(Enemies()[0].PoisonRemaining == 0f && Enemies()[1].PoisonRemaining == 3f, "Swap-back transferred poison to the wrong enemy.");
        int survivorHealth = Enemies()[1].Health; Call(horde, "TickPoison", 1f, 2, 1f, Read<ParticleSystem>(poison, "_statusParticlesPrefab"));
        Check(Enemies()[1].Health == survivorHealth - 2, "Survivor lost poison after compaction.");
        Call(horde, "Simulate"); Check(Enemies()[1].PoisonRemaining == 2f, "Simulation buffer swap lost poison.");
        var enemies = Enemies(); var nextEnemies = Read<NativeArray<EnemyRuntime>>(horde, "_nextEnemies");
        var dying = enemies[1]; dying.Health = 2; enemies[1] = dying; nextEnemies[1] = dying;
        int deaths = 0; void Reward(int xp, int score) { deaths++; Check(xp == imp.XPReward && score == imp.ScoreReward, "Wrong poison kill rewards."); }
        horde.EnemyDied += Reward;
        try { Call(horde, "TickPoison", 1f, 2, 1f, Read<ParticleSystem>(poison, "_statusParticlesPrefab")); }
        finally { horde.EnemyDied -= Reward; }
        Check(deaths == 1 && horde.ActiveEnemyCount == 1, "Poison death did not award exactly once.");
        Spawn(imp, center); Check(Enemies()[1].PoisonRemaining == 0f, "Pooled enemy retained poison.");
        var views = Read<object>(horde, "_views"); var activeViews = Read<IList>(views, "_activeViews");
        var particles = Read<ParticleSystem>(activeViews[1], "PoisonParticles");
        Check(particles == null || !particles.isPlaying, "Reused enemy retained particles.");
        passed.Add("Enemy buffer swaps, swap-back removal, poison kill rewards and pooled status reset");

        poison.SpawnPuddle(center); health.transform.position = center; Call(poison, "Tick", 0.25f);
        float remaining = Read<float>(poison, "_playerPoisonRemaining");
        Call(poison, "LateUpdate"); Check(Read<float>(poison, "_playerPoisonRemaining") == remaining, "Paused poison advanced.");
        projectiles.SpawnSpit(center + Vector3.up, Vector3.forward, 4, 1f, 1f, 1.4f, true);
        var positionBefore = Read<GameObject>(Bolts()[0], "View").transform.position;
        Call(projectiles, "Update"); Check(Read<GameObject>(Bolts()[0], "View").transform.position == positionBefore, "Paused projectile moved.");
        ClearBolts(); projectiles.SpawnSpit(center + Vector3.up, Vector3.forward, 4, 1f, 1f, 0.65f, false);
        Check(!Read<bool>(Bolts()[0], "LeavesPoison") && Mathf.Abs(Read<float>(Bolts()[0], "Radius") - 0.2925f) < 0.001f, "Pooled spit retained size or poison metadata.");
        ClearBolts(); Call(poison, "Clear"); ClearEnemies();
        for (int i = 0; i < 70; i++) poison.SpawnPuddle(center);
        Check(Puddles().Count == 64, "Puddle cap failed."); Call(poison, "Clear");
        var deathObject = new GameObject("PoisonDeathCheck"); var testHealth = deathObject.AddComponent<PlayerHealth>(); int died = 0; testHealth.Died += () => died++;
        testHealth.TakePoisonDamage(100); testHealth.TakePoisonDamage(100);
        Check(testHealth.CurrentHealth == 0 && died == 1, "Player poison death fired incorrectly."); UnityEngine.Object.Destroy(deathObject);
        passed.Add("Pause, pooled orb metadata reset and player poison death");
        return $"PASS: {passed.Count} integration groups\n" + string.Join("\n", passed);
    }
}
