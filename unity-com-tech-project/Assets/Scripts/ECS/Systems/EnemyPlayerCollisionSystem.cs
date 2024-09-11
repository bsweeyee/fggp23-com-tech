using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;

[UpdateAfter(typeof(PlayerMoveSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyPlayerCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NativeReference<float2> playerVelocityRef = new NativeReference<float2>(Allocator.TempJob);
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);

        foreach(var (pTag, pTransform, pAABB, pmd, entity) in SystemAPI.Query<PlayerTag, LocalToWorld, AABBData, MovementData>().WithEntityAccess())
        {
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
        foreach(var (pTag, movementData) in SystemAPI.Query<PlayerTag, RefRW<MovementData>>())
        {
            if (math.lengthsq(pVel) > 0)
                movementData.ValueRW.ExternalVelocity = pVel;        
        }
        
        ecb.Dispose();
        playerVelocityRef.Dispose();
    }
}

[BurstCompile]
public partial struct EnemyPlayerCollisionJob : IJobEntity
{
    public LocalToWorld PlayerTransform;
    public AABBData PlayerBounds;
    public NativeReference<float2> PlayerVelocityRef;
    public EntityCommandBuffer ECB;
    
    [BurstCompile]
    private void Execute(Entity Enemy, EnabledRefRW<EnemyTag> eTag, in AABBData aabb, in LocalTransform transform, MovementData md)
    {
         if (eTag.ValueRO == false) return;
         
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
