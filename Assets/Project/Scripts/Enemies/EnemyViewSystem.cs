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
    private readonly ObjectPool<GameObject> _pool;
    private readonly List<GameObject> _activeViews;

    private TransformAccessArray _transforms;
    private bool _disposed;

    internal EnemyViewSystem(GameObject prefab, int maxCount)
    {
        _activeViews = new List<GameObject>(maxCount);
        _transforms = new TransformAccessArray(maxCount);

        _pool = new ObjectPool<GameObject>(
            () => CreateView(prefab), view => SetActive(view, true), view => SetActive(view, false), DestroyView, true, Mathf.Min(64, maxCount), maxCount);
    }

    internal void AddView(float3 position)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EnemyViewSystem));

        GameObject view = _pool.Get();
        view.transform.position = new Vector3(position.x, position.y, position.z);

        _activeViews.Add(view);
        _transforms.Add(view.transform);
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

        foreach (GameObject view in _activeViews)
            DestroyView(view);

        _activeViews.Clear();
        _pool.Clear();
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