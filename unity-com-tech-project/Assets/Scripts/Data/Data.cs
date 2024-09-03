using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

#region Spawn Data
public struct Spawner : IComponentData
{
    public Entity Prefab;
    public float2 SpawnPosition;
    public float NextSpawnTime;
    public float SpawnRate;
}
#endregion

#region Player Data
public struct PlayerMoveInput : IComponentData
{
    public float2 Value;
}

public struct MovementData : IComponentData
{
    public float2 Direction;
    public float Speed;
}

public struct PlayerTag : IComponentData
{
}
#endregion

public struct ProjectileTag : IComponentData
{
}

public struct ProjectileShooterData : IComponentData
{
    public Entity Prefab;
    public bool ShouldFire;
}