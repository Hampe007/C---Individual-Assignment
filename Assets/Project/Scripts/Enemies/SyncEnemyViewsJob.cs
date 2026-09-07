using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
internal struct SyncEnemyViewsJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<EnemyRuntime> Enemies;
    [ReadOnly] public float3 Target;

    public void Execute(int index, TransformAccess transform)
    {
        float3 position = Enemies[index].Position;
        float3 forward = Target - position;

        quaternion rotation = math.lengthsq(forward) > 0.0001f
            ? quaternion.LookRotationSafe(forward, new float3(0f, 1f, 0f))
            : quaternion.identity;

        transform.SetPositionAndRotation(
            new Vector3(position.x, position.y, position.z),
            new Quaternion(
                rotation.value.x,
                rotation.value.y,
                rotation.value.z,
                rotation.value.w));
    }
}