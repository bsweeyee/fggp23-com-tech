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
    public double ProjectileShootCooldown;    

    // Spawn    
    public float2 SpawnEnemyStartPosition;
    public double SpawnEnemyRate;

    // Camera
    public Entity CameraEntity;
    public float2 CameraBoundsPadding;    

    // Enemy
    public Entity EnemyEntity;
    public float EnemyMoveSpeed;
}

#endregion

public struct MovementData : IComponentData
{
    public float2 Direction;
    public float Speed;
    public float AngularSpeed;
    public float2 ExternalVelocity;
}

public struct InputData : IComponentData
{
    public float2 Direction;
    public float2 PreviousDirection;
    public int InputState; // 0: none, 1: pressed, 2: held, 3: released 
}

#region Player Data

public struct ProjectileShooterData : IComponentData
{    
    public double LastFireTime;
}

public struct PlayerTag : IComponentData
{
}
#endregion

public struct ProjectileTag : IComponentData
{
}

public struct EnemyTag : IComponentData
{    
}

public struct CameraData : IComponentData
{
    public float2 Position;
    public float2 BoundsPadding;
    public float2 Bounds;    
}

public struct SpawnerData : IComponentData
{
    public double LastSpawnTime;    
}

public struct AABBData : IComponentData
{
    public float2 Min;
    public float2 Max;
    public float2 OriginalSize;
}