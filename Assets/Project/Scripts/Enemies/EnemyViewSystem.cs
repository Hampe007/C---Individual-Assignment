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
    
    private readonly List<ObjectPool<GameObject>> _pools;
    private readonly List<EnemyView> _activeViews;
    
    private struct EnemyView
    {
        public GameObject GameObject;
        public EnemyHitFlash HitFlash;
        public int DefinitionIndex;

        public EnemyView(GameObject gameObject, EnemyHitFlash hitFlash, int definitionIndex)
        {
            GameObject = gameObject;
            HitFlash = hitFlash;
            DefinitionIndex = definitionIndex;
        }
    }
    
    private TransformAccessArray _transforms;
    private bool _disposed;

    internal EnemyViewSystem(GameObject[] prefabs, int maxCount)
    {
        _pools = new List<ObjectPool<GameObject>>(prefabs.Length);
        _activeViews = new List<EnemyView>(maxCount);
        _transforms = new TransformAccessArray(maxCount);

        for (int i = 0; i < prefabs.Length; i++)
            _pools.Add(CreatePool(prefabs[i], maxCount));
    }


    internal void AddView(float3 position, int definitionIndex)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EnemyViewSystem));

        GameObject view = _pools[definitionIndex].Get();
        view.transform.position = new Vector3(position.x, position.y, position.z);

        EnemyHitFlash hitFlash = view.GetComponent<EnemyHitFlash>();

        _activeViews.Add(new EnemyView(view, hitFlash, definitionIndex));
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
            _pools[removed.DefinitionIndex].Release(removed.GameObject);
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
        
        foreach (ObjectPool<GameObject> pool in _pools)
            pool.Clear();

        _pools.Clear();
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
