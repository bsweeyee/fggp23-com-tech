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

[UpdateAfter(typeof(PlayerMoveSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyMoveSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        NativeReference<float2> playerVelocityRef = new NativeReference<float2>(Allocator.TempJob);
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
        foreach(var (pTag, pTransform, pAABB, pmd, entity) in SystemAPI.Query<PlayerTag, LocalToWorld, AABBData, MovementData>().WithEntityAccess())
        {
            new EnemyMoveJob {
                DeltaTime = deltaTime,
                PlayerTransformData = pTransform,                
            }.Schedule();

            new EnemyPlayerCollisionJob {
                PlayerTransform = pTransform,
                PlayerBounds = pAABB,
                PlayerVelocityRef = playerVelocityRef,
                ECB = ecb,
            }.Schedule();                  
        }
        state.Dependency.Complete();        
        ecb.Playback(state.EntityManager);

        float2 pVel = playerVelocityRef.Value;
        // Debug.Log(pVel);
        foreach(var (pTag, movementData) in SystemAPI.Query<PlayerTag, RefRW<MovementData>>())
        {
            if (math.lengthsq(pVel) > 0)
                movementData.ValueRW.ExternalVelocity = pVel;        
        }
        
        ecb.Dispose();
        playerVelocityRef.Dispose();
    }
}

public partial struct EnemyMoveJob : IJobEntity
{
    public float DeltaTime;    
    public LocalToWorld PlayerTransformData;    
    private void Execute(in EnemyTag ptag, ref LocalTransform transform, ref MovementData md)
    {
        var direction = PlayerTransformData.Position - transform.Position;
        direction = math.normalize(direction);
        
        md.Direction = new float2(direction.xy);        

        transform.Position += direction * DeltaTime * md.Speed;
    }
}

public partial struct EnemyPlayerCollisionJob : IJobEntity
{
    public LocalToWorld PlayerTransform;
    public AABBData PlayerBounds;
    public NativeReference<float2> PlayerVelocityRef;
    public EntityCommandBuffer ECB;
    
    private void Execute(Entity Enemy, in EnemyTag eTag, in AABBData aabb, in LocalTransform transform, MovementData md)
    {
        // check intersection
        float2 tmin = transform.Position.xy + aabb.Min;
        float2 tmax = transform.Position.xy + aabb.Max;
        
        float2 pmin = PlayerTransform.Position.xy + PlayerBounds.Min;
        float2 pmax = PlayerTransform.Position.xy + PlayerBounds.Max;

        bool collisionX =  tmax.x >= pmin.x &&
                            pmax.x >= tmin.x;
        bool collisionY =  tmax.y >= pmin.y &&
                            pmax.y >= tmin.y;
        if (collisionX && collisionY)
        {
            // we deal damage to player
            PlayerVelocityRef.Value = md.Direction * 10;            
            ECB.DestroyEntity(Enemy);
            // Debug.Log("collided with player: " + PlayerVelocityRef.Value);
        }                               
    }
}
