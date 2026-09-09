using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Pool;

internal sealed class EnemyViewSystem : IDisposable
{
    
    private readonly ObjectPool<GameObject> _impPool;
    private readonly ObjectPool<GameObject> _lycanPool;
    private readonly ObjectPool<GameObject> _tidebreakerPool;
    
    private struct EnemyView
    {
        public GameObject GameObject;
        public EnemyHitFlash HitFlash;
        public EnemyVisualType Visual;

        public EnemyView(GameObject gameObject, EnemyHitFlash hitFlash, EnemyVisualType visual)
        {
            GameObject = gameObject;
            HitFlash = hitFlash;
            Visual = visual;
        }
    }
    
    private readonly List<EnemyView> _activeViews;

    private TransformAccessArray _transforms;
    private bool _disposed;

    internal EnemyViewSystem(GameObject impPrefab, GameObject lycanPrefab, GameObject tidebreakerPrefab, int maxCount)
    {
        _activeViews = new List<EnemyView>(maxCount);
        _transforms = new TransformAccessArray(maxCount);

        _impPool = CreatePool(impPrefab, maxCount);
        _lycanPool = CreatePool(lycanPrefab, maxCount);
        _tidebreakerPool = CreatePool(tidebreakerPrefab, maxCount);
    }


    internal void AddView(float3 position, EnemyVisualType visual)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EnemyViewSystem));

        GameObject view = GetPool(visual).Get();
        view.transform.position = new Vector3(position.x, position.y, position.z);

        EnemyHitFlash hitFlash = view.GetComponent<EnemyHitFlash>();

        _activeViews.Add(new EnemyView(view, hitFlash, visual));
        _transforms.Add(view.transform);
    }

    internal void PlayHitFlash(int index)
    {
        if (index < 0 || index >= _activeViews.Count)
            return;

        EnemyHitFlash hitFlash = _activeViews[index].HitFlash;

        if (hitFlash != null)
            hitFlash.Play();
    }
    
    internal void RemoveAtSwapBack(int index)
    {
        if (index < 0 || index >= _activeViews.Count)
            return;

        int lastIndex = _activeViews.Count - 1;
        EnemyView removed = _activeViews[index];

        if (index != lastIndex)
            _activeViews[index] = _activeViews[lastIndex];

        _activeViews.RemoveAt(lastIndex);
        _transforms.RemoveAtSwapBack(index);

        if (removed.GameObject != null)
            GetPool(removed.Visual).Release(removed.GameObject);
    }
    
    internal JobHandle ScheduleSync(NativeArray<EnemyRuntime> enemies, float3 target, JobHandle dependency)
    {
        return new SyncEnemyViewsJob
        {
            Enemies = enemies,
            Target = target
        }.Schedule(_transforms, dependency);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_transforms.isCreated)
            _transforms.Dispose();

        foreach (EnemyView view in _activeViews)
            DestroyView(view.GameObject);

        _activeViews.Clear();
        
        _impPool.Clear();
        _lycanPool.Clear();
        _tidebreakerPool.Clear();
    }

    private ObjectPool<GameObject> GetPool(EnemyVisualType visual)
    {
        switch (visual)
        {
            case EnemyVisualType.Lycan:
                return _lycanPool;

            case EnemyVisualType.Tidebreaker:
                return _tidebreakerPool;

            default:
                return _impPool;
        }
    }

    private static ObjectPool<GameObject> CreatePool(GameObject prefab, int maxCount)
    {
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
        GameObject view = UnityEngine.Object.Instantiate(prefab);
        view.hideFlags = HideFlags.HideInHierarchy;
        return view;
    }

    private static void SetActive(GameObject view, bool active)
    {
        if (view != null)
            view.SetActive(active);
    }

    private static void DestroyView(GameObject view)
    {
        if (view != null)
            UnityEngine.Object.Destroy(view);
    }
}
