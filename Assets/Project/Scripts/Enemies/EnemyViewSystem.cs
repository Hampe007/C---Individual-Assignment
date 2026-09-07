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

    internal EnemyViewSystem(GameObject prefab, int count)
    {
        _activeViews = new List<GameObject>(count);
        _transforms = new TransformAccessArray(count);

        _pool = new ObjectPool<GameObject>(
            () => CreateView(prefab),
            view =>
            {
                if (view != null)
                    view.SetActive(true);
            },
            view =>
            {
                if (view != null)
                    view.SetActive(false);
            },
            view =>
            {
                if (view != null)
                    UnityEngine.Object.Destroy(view);
            },
            true,
            count,
            count);

        for (int i = 0; i < count; i++)
        {
            GameObject view = _pool.Get();

            _activeViews.Add(view);
            _transforms.Add(view.transform);
        }
    }

    internal JobHandle ScheduleSync(
        NativeArray<EnemyRuntime> enemies,
        float3 target,
        JobHandle dependency)
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
        {
            if (view != null)
                UnityEngine.Object.Destroy(view);
        }

        _activeViews.Clear();
        _pool.Clear();
    }

    private static GameObject CreateView(GameObject prefab)
    {
        GameObject view = UnityEngine.Object.Instantiate(prefab);
        view.hideFlags = HideFlags.HideInHierarchy;

        return view;
    }
}