using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using System.Diagnostics;

[UpdateAfter(typeof(PlayerMoveSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ProjectileMoveSystem : ISystem
{     
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        // Fire based on having the FireProjectileTag data component
        foreach(var localToWorld in SystemAPI.Query<LocalToWorld>().WithAll<ProjectileShooterData>())
        {
            new ProjectilesMoveJob
            {
                DeltaTime = deltaTime,
                PlayerLocalToWorld = localToWorld
            }.Schedule();      
        }            

        // Run in Unity Main Thread
        // foreach(var (transform,moveSpeed) in SystemAPI.Query<RefRW<LocalTransform>, ProjectileMoveSpeed>())
        // {
        //     transform.ValueRW.Position += transform.ValueRO.Up() * moveSpeed.Value * deltaTime;
        // }
        
        // Run in Jobs
    }
}

[BurstCompile]
public partial struct ProjectilesMoveJob : IJobEntity
{
    public float DeltaTime;    
    public LocalToWorld PlayerLocalToWorld;
    
    [BurstCompile]
    private void Execute(ref LocalTransform transform, in ProjectileTag pTag, MovementData movementData)
    {
        float3 moveDirection = PlayerLocalToWorld.Up;        
        transform.Position.xy += moveDirection.xy * movementData.Speed * DeltaTime;
    }
}
