using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Pool;

/// <summary>
/// Manages pooled prefab instances separately from job data. Its active-view list and TransformAccessArray
/// follow the same index ordering as HordeManager's active runtime arrays.
/// </summary>
internal sealed class EnemyViewSystem : IDisposable
{
    
    private readonly List<ObjectPool<GameObject>> _pools; // One prefab pool per EnemyDefinition index.
    private readonly List<EnemyView> _activeViews; // Active presentation data in the same order as runtime enemies.
    
    private struct EnemyView
    {
        public GameObject GameObject; // Pooled object currently representing this enemy.
        public EnemyHitFlash HitFlash; // Cached component avoids looking it up for every hit.
        public int DefinitionIndex; // Selects the correct pool when this view is released.

        public EnemyView(GameObject gameObject, EnemyHitFlash hitFlash, int definitionIndex)
        {
            GameObject = gameObject;
            HitFlash = hitFlash;
            DefinitionIndex = definitionIndex;
        }
    }
    
    private TransformAccessArray _transforms; // Job-compatible transform list aligned by index with _activeViews.
    private bool _disposed; // Prevents use or cleanup of native transform storage after disposal.

    internal EnemyViewSystem(GameObject[] prefabs, int maxCount)
    {
        _pools = new List<ObjectPool<GameObject>>(prefabs.Length); // Capacity equals the number of enemy definitions.
        _activeViews = new List<EnemyView>(maxCount); // Pre-size to reduce list growth up to the active-enemy limit.
        _transforms = new TransformAccessArray(maxCount); // Pre-size the transform collection used by the sync job.

        // Pools are created in definition order so a definition index addresses the matching pool.
        for (int i = 0; i < prefabs.Length; i++)
            _pools.Add(CreatePool(prefabs[i], maxCount));
    }


    internal void AddView(float3 position, int definitionIndex)
    {
        // A view cannot be added after its TransformAccessArray has been disposed.
        if (_disposed)
            throw new ObjectDisposedException(nameof(EnemyViewSystem));

        GameObject view = _pools[definitionIndex].Get(); // Reuse an inactive object or create one through the pool callback.
        view.transform.position = new Vector3(position.x, position.y, position.z);

        EnemyHitFlash hitFlash = view.GetComponent<EnemyHitFlash>(); // Cache optional hit feedback for later damage calls.

        // Append to both collections together to preserve the shared runtime/view index invariant.
        _activeViews.Add(new EnemyView(view, hitFlash, definitionIndex));
        _transforms.Add(view.transform);
    }

    internal void PlayHitFlash(int index)
    {
        // Damage may arrive for an index that is no longer active, so validate before indexing the list.
        if (index < 0 || index >= _activeViews.Count)
            return;

        EnemyHitFlash hitFlash = _activeViews[index].HitFlash;

        if (hitFlash != null)
            hitFlash.Play();
    }
    
    internal void RemoveAtSwapBack(int index)
    {
        // Invalid or already-removed indices are harmless no-ops.
        if (index < 0 || index >= _activeViews.Count)
            return;

        int lastIndex = _activeViews.Count - 1; // Swap-back removal keeps the packed collections O(1).
        EnemyView removed = _activeViews[index];

        if (index != lastIndex)
            _activeViews[index] = _activeViews[lastIndex];

        _activeViews.RemoveAt(lastIndex);
        _transforms.RemoveAtSwapBack(index); // Apply the same index swap as _activeViews and HordeManager's arrays.

        // Return the removed object to the pool for its original enemy definition.
        if (removed.GameObject != null)
            _pools[removed.DefinitionIndex].Release(removed.GameObject);
    }
    
    internal JobHandle ScheduleSync(NativeArray<EnemyRuntime> enemies, float3 target, JobHandle dependency)
    {
        // Transform work is chained after movement so the views reflect the next-state buffer.
        return new SyncEnemyViewsJob
        {
            Enemies = enemies,
            Target = target
        }.Schedule(_transforms, dependency);
    }

    public void Dispose()
    {
        // Disposal can be reached from multiple teardown paths; only release native storage once.
        if (_disposed)
            return;

        _disposed = true;

        if (_transforms.isCreated)
            _transforms.Dispose();

        // Active views are destroyed directly; pooled inactive instances are destroyed by clearing each pool.
        foreach (EnemyView view in _activeViews)
            DestroyView(view.GameObject);

        _activeViews.Clear();
        
        foreach (ObjectPool<GameObject> pool in _pools)
            pool.Clear();

        _pools.Clear();
    }
    
    private static ObjectPool<GameObject> CreatePool(GameObject prefab, int maxCount)
    {
        // Pool callbacks define creation, activation, deactivation, destruction, and retained capacity.
        return new ObjectPool<GameObject>(
            () => CreateView(prefab),
            view => SetActive(view, true),
            view => SetActive(view, false),
            DestroyView,
            true,
            Mathf.Min(32, maxCount),
            maxCount);
    }
    
    private static GameObject CreateView(GameObject prefab)
    {
        // Instantiate lazily when a pool has no inactive view available.
        GameObject view = UnityEngine.Object.Instantiate(prefab);
        view.hideFlags = HideFlags.HideInHierarchy;
        return view;
    }

    private static void SetActive(GameObject view, bool active)
    {
        // Unity objects can become null after destruction even when a managed reference remains.
        if (view != null)
            view.SetActive(active);
    }

    private static void DestroyView(GameObject view)
    {
        // Pool overflow and pool clearing both route through this callback.
        if (view != null)
            UnityEngine.Object.Destroy(view);
    }
}
