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
        
        var gse = SystemAPI.GetSingletonEntity<GameStateComponent>();
        var gameStateComponent = SystemAPI.GetComponent<GameStateComponent>(gse);

        foreach(var (pTag, pTransform, pAABB, pHealthData, entity) in SystemAPI.Query<PlayerTag, LocalToWorld, AABBData, PlayerHealthData>().WithEntityAccess())
        {
            new EnemyPlayerCollisionJob {                
                PlayerEntity = entity,
                PlayerHealthData = pHealthData,
                PlayerTransform = pTransform,
                PlayerBounds = pAABB,
                PlayerVelocityRef = playerVelocityRef,
                GameStateComponent = gameStateComponent,
                GameEntity = gse,
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
    public Entity PlayerEntity;
    public PlayerHealthData PlayerHealthData;
    public LocalToWorld PlayerTransform;
    public AABBData PlayerBounds;
    public NativeReference<float2> PlayerVelocityRef;
    
    public Entity GameEntity;
    public GameStateComponent GameStateComponent;
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
            ECB.SetComponentEnabled<EnemyTag>(Enemy, false);
            
            PlayerHealthData.Value -= 1;
            ECB.SetComponent(PlayerEntity, PlayerHealthData);
            
            if (PlayerHealthData.Value <= 0)
            {
                GameStateComponent.CurrentState = 2; // go to game over state
                ECB.SetComponent(GameEntity, GameStateComponent);
            }
            // ECB.SetComponent(Enemy, new LocalTransform
            // {
            //     Position = new float3(0, 0, -100),
            //     Rotation = transform.Rotation,
            //     Scale = transform.Scale,                    
            // }); 
            // Debug.Log("collided with player: " + PlayerVelocityRef.Value);
        }                                       
    }
}
