using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using System.Runtime.CompilerServices;

[UpdateAfter(typeof(PlayerMoveSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ProjectileMoveSystem : ISystem
{     
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;        
        Entity cdEntity = SystemAPI.GetSingletonEntity<CameraData>();
        CameraData cd = SystemAPI.GetComponent<CameraData>(cdEntity);
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);

        // projectile movement
        foreach(var localToWorld in SystemAPI.Query<LocalToWorld>().WithAll<ProjectileShooterData>())
        {
            var moveJob = new ProjectilesMoveJob
            {
                DeltaTime = deltaTime,                
            };
            moveJob.Schedule();            
        } 
        
        //destroy entities when hit camera bounds
        var destroyJob = new ProjectileDestroyJob
        {
            CameraData = cd,
            CommandBuffer = ecb,
        };         
        destroyJob.Schedule();                                          

        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();        

        // Run in Unity Main Thread
        // foreach(var (transform,moveSpeed) in SystemAPI.Query<RefRW<LocalTransform>, ProjectileMoveSpeed>())
        // {
        //     transform.ValueRW.Position += transform.ValueRO.Up() * moveSpeed.Value * deltaTime;
        // }
        
        // Run in Jobs
    }
}

[BurstCompile]
public partial struct ProjectileDestroyJob : IJobEntity
{
    public CameraData CameraData;
    public EntityCommandBuffer CommandBuffer;
    
    [BurstCompile]
    private void Execute(Entity entity, in ProjectileTag ptag, LocalTransform transform)
    {
        float2 topRight = CameraData.Position + (CameraData.Bounds/2 + CameraData.BoundsPadding/2);
        float2 bottomLeft = CameraData.Position - (CameraData.Bounds/2 + CameraData.BoundsPadding/2);

        float2 pos = transform.Position.xy;
        
        if (pos.x < bottomLeft.x || pos.y < bottomLeft.y || pos.x > topRight.x || pos.y > topRight.y)
        {            
            CommandBuffer.DestroyEntity(entity);
        }
    }
}

[BurstCompile]
public partial struct ProjectilesMoveJob : IJobEntity
{
    public float DeltaTime;        
    
    [BurstCompile]
    private void Execute(ref LocalTransform transform, in ProjectileTag pTag, MovementData movementData)
    {                                
        transform.Position.xy += movementData.Direction.xy * movementData.Speed * DeltaTime;
    }
}
