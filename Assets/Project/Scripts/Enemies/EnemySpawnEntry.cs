using System;
using UnityEngine;

[Serializable]
public sealed class EnemySpawnEntry
{
    [SerializeField] private EnemyDefinition _enemy;
    [SerializeField, Min(0f)] private float _unlockTime;
    [SerializeField, Min(0f)] private float _weight = 1f;

    public EnemyDefinition Enemy => _enemy;
    public float UnlockTime => _unlockTime;
    public float Weight => _weight;
}