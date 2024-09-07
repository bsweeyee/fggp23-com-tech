using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities.Serialization;

#region Game Data

/// <summary>
/// This will be the mapping from Unity ScriptableObject into Unity DOTs. It should be a Singleton component data
/// </summary>
public struct GameDataComponent : IComponentData
{    
    // Player
    public Entity PlayerEntity;
    public float2 PlayerStartPosition;
    public float PlayerMoveSpeed;
    public float PlayerAngularSpeed;

    // Projectile
    public Entity ProjectileEntity;
    public float ProjectileSpeed;    

    // Spawn
    public float2 SpawnEnemyStartPosition;
    public float SpawnEnemyRate;       
}

#endregion

public struct TimerComponent : IComponentData
{
    public float NextTime;    
}

public struct MovementData : IComponentData
{
    public float2 Direction;
    public float Speed;
    public float AngularSpeed;
}

public struct InputData : IComponentData
{
    public float2 Direction;
}

#region Player Data

public struct ProjectileShooterData : IComponentData
{
    public bool ShouldFire;
}

public struct PlayerTag : IComponentData
{
}
#endregion

public struct ProjectileTag : IComponentData
{
}
