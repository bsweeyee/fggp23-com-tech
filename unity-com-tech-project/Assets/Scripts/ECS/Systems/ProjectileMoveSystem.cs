using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using System.Runtime.CompilerServices;
using Unity.Collections;
using NUnit.Framework.Constraints;
using Unity.Jobs;

[UpdateBefore(typeof(TransformSystemGroup))]
[UpdateAfter(typeof(FireProjectileSystem))]
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
        var moveJob = new ProjectilesMoveJob
        {
            DeltaTime = deltaTime,
            // ECB = ecb                
            ECB = ecb.AsParallelWriter()                
        };
        moveJob.ScheduleParallel();
        state.Dependency.Complete();
        
        //destroy entities when hit camera bounds
        var destroyJob = new ProjectileDestroyJob
        {
            CameraData = cd,
            // ECB = ecb,
            ECB = ecb.AsParallelWriter(),
        };
        destroyJob.ScheduleParallel();
        // destroyJob.Schedule();
        state.Dependency.Complete();        

        ecb.Playback(state.EntityManager);                                    
        ecb.Dispose();        
    }
}

// [BurstCompile]
// [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct ProjectileDestroyJob : IJobEntity
{
    public CameraData CameraData;
    // public EntityCommandBuffer ECB;
    public EntityCommandBuffer.ParallelWriter ECB;
    
    // [BurstCompile]
    private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, EnabledRefRO<ProjectileTag> pTag, LocalTransform transform)
    {
        float2 topRight = CameraData.Position + (CameraData.Bounds/2 + CameraData.BoundsPadding/2);
        float2 bottomLeft = CameraData.Position - (CameraData.Bounds/2 + CameraData.BoundsPadding/2);

        float2 pos = transform.Position.xy;
        
        if (pos.x < bottomLeft.x || pos.y < bottomLeft.y || pos.x > topRight.x || pos.y > topRight.y)
        {            
            // ECB.SetComponentEnabled<ProjectileTag>(entity, false);
            // ECB.SetComponent(entity, new LocalTransform
            // {
            //     Position = new float3(0, 0, -100),
            //     Rotation = transform.Rotation,            
            //     Scale = transform.Scale             
            // });

            // ECB.SetComponent(chunkIndex, entity, new LocalTransform
            // {
            //     Position = new float3(0, 0, -100),            
            //     Rotation = transform.Rotation,            
            //     Scale = transform.Scale            
            // });            
            ECB.SetComponentEnabled<ProjectileTag>(chunkIndex, entity, false);
        }
    }
}

[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct ProjectilesMoveJob : IJobEntity
{
    public float DeltaTime;
    // public EntityCommandBuffer ECB; 
    public EntityCommandBuffer.ParallelWriter ECB;            
    
    [BurstCompile]
    private void Execute([ChunkIndexInQuery] int chunkIndex, Entity projectileEntity, LocalTransform transform, EnabledRefRO<ProjectileTag> pTag, MovementData movementData)
    {
        if (pTag.ValueRO == false)
        {
            ECB.SetComponent(chunkIndex, projectileEntity, new LocalTransform
            {
                Position = new float3(0, 0, -100),            
                Rotation = transform.Rotation,            
                Scale = transform.Scale            
            });
            return;
        }           
        // transform.Position.xy += movementData.Direction.xy * movementData.Speed * DeltaTime;
        float2 newPos = transform.Position.xy + movementData.Direction.xy * movementData.Speed * DeltaTime;
        ECB.SetComponent(chunkIndex, projectileEntity, new LocalTransform {
            Position = new float3(newPos, 0),
            Rotation = transform.Rotation,
            Scale = transform.Scale,
        });
    }
}
