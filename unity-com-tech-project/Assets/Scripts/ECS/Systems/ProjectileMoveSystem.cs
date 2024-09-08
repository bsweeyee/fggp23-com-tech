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

        // TODO check collision with Enemy entity.
        NativeArray<MovementData> enemyMovementData = new NativeArray<MovementData>(99, Allocator.TempJob);                                                  
        NativeArray<LocalTransform> enemyLocalTransform = new NativeArray<LocalTransform>(99, Allocator.TempJob);                                                  
        NativeArray<AABBData> enemyAABBData = new NativeArray<AABBData>(99, Allocator.TempJob);                                                  
        NativeArray<Entity> enemyEntity = new NativeArray<Entity>(99, Allocator.TempJob);                                                  

        int i = 0;                                              
        foreach (var (et, md, lt, aabb, entity) in SystemAPI.Query<EnemyTag, MovementData, LocalTransform, AABBData>().WithEntityAccess())
        {
            enemyMovementData[i] = md;
            enemyLocalTransform[i] = lt;
            enemyAABBData[i] = aabb;
            enemyEntity[i] = entity;

            i++;
        }

        new ProjectileCollisionJob
        {
            EnemyMovementData = enemyMovementData,
            EnemyLocalTransform = enemyLocalTransform,
            EnemyAABB = enemyAABBData,
            EnemyEntity = enemyEntity,
        }.Schedule();

        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        
        ecb.Dispose();
        enemyMovementData.Dispose();        
        enemyLocalTransform.Dispose();        
        enemyAABBData.Dispose();        
        enemyEntity.Dispose();        

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

public partial struct ProjectileCollisionJob : IJobEntity
{
    public NativeArray<MovementData> EnemyMovementData;
    public NativeArray<LocalTransform> EnemyLocalTransform;
    public NativeArray<AABBData> EnemyAABB;
    public NativeArray<Entity> EnemyEntity;
    public EntityCommandBuffer ECB;

    public void Execute(in ProjectileTag pTag, in AABBData aabb, in LocalTransform transform, Entity entity)
    {
        // TODO error: this writes to LocalTransform but type was not assigned to the Dependency Property

        // check intersection
        float2 tmin = transform.Position.xy + aabb.Min;
        float2 tmax = transform.Position.xy + aabb.Max;
        for(int i=0; i<EnemyMovementData.Length; i++)
        {
            MovementData emd = EnemyMovementData[i];
            LocalTransform el = EnemyLocalTransform[i];
            AABBData eaabb = EnemyAABB[i];

            float2 pmin = el.Position.xy + eaabb.Min;
            float2 pmax = el.Position.xy + eaabb.Max;

            bool collisionX =  tmax.x >= pmin.x &&
                                pmax.x >= tmin.x;
            bool collisionY =  tmax.y >= pmin.y &&
                                pmax.y >= tmin.y;
            if (collisionX && collisionY)
            {
                // we deal damage to player                            
                ECB.DestroyEntity(entity);
                ECB.DestroyEntity(EnemyEntity[i]);
                // Debug.Log("collided with player: " + PlayerVelocityRef.Value);
            }
        }        
    }
}