using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using System.Threading;
using Unity.Collections;
using UnityEditor;
using Unity.Burst;
using Unity.Entities.UniversalDelegates;

[UpdateAfter(typeof(PlayerMoveSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;        
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
                
        foreach(var (pTag, pTransform, pAABB, pmd, entity) in SystemAPI.Query<PlayerTag, LocalToWorld, AABBData, MovementData>().WithEntityAccess())
        {
            new EnemyMoveJob {
                DeltaTime = deltaTime,
                PlayerTransformData = pTransform,                
            }.Schedule();                         
        }

        state.Dependency.Complete();        
        ecb.Playback(state.EntityManager);            
        ecb.Dispose();
    }
}

[BurstCompile]
public partial struct EnemyMoveJob : IJobEntity
{
    public float DeltaTime;    
    public LocalToWorld PlayerTransformData;    
    
    [BurstCompile]
    private void Execute(in EnemyTag ptag, ref LocalTransform transform, ref MovementData md)
    {
        var direction = PlayerTransformData.Position - transform.Position;
        direction = math.normalize(direction);
        
        md.Direction = new float2(direction.xy);        

        transform.Position += direction * DeltaTime * md.Speed;
    }
}