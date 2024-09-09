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
        /* TODO we need this dependency check because projectile collision job accesses enemy. 
            but this may not be efficient and can cause thread to state dependpency complete may cause thread to stall?

            maybe check if there's another way to handle this    
        */
        state.Dependency.Complete();        
        Entity gameEntity = SystemAPI.GetSingletonEntity<GameDataComponent>();
        GameStateComponent gsc = SystemAPI.GetComponent<GameStateComponent>(gameEntity);
        GameDataComponent gdc = SystemAPI.GetComponent<GameDataComponent>(gameEntity);
        DynamicBuffer<CurveBufferData> cbd = SystemAPI.GetBuffer<CurveBufferData>(gameEntity);

        foreach(var (projTag, projTransform, projAABB, entity) in SystemAPI.Query<ProjectileTag, LocalToWorld, AABBData>().WithEntityAccess())
        {
            new EnemyProjectileCollisionJob
            {
                ProjectileTransform = projTransform,
                ProjectileBounds = projAABB,
                ProjectileEntity = entity,
                ECB = ecb,
                
                GameEntity = gameEntity,
                GSC = gsc,
                GDC = gdc,
                CBD = cbd,
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

[BurstCompile]
public partial struct EnemyPlayerCollisionJob : IJobEntity
{
    public LocalToWorld PlayerTransform;
    public AABBData PlayerBounds;
    public NativeReference<float2> PlayerVelocityRef;
    public EntityCommandBuffer ECB;
    
    [BurstCompile]
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

[BurstCompile]
public partial struct EnemyProjectileCollisionJob : IJobEntity
{
    public LocalToWorld ProjectileTransform;
    public AABBData ProjectileBounds;
    public Entity ProjectileEntity;
    public EntityCommandBuffer ECB;
    
    public Entity GameEntity;
    public GameStateComponent GSC;
    public GameDataComponent GDC;
    public DynamicBuffer<CurveBufferData> CBD;
    
    [BurstCompile]
    private void Execute(Entity Enemy, in EnemyTag eTag, in AABBData aabb, in LocalTransform transform, MovementData md)
    {
        // check intersection
        float2 tmin = transform.Position.xy + aabb.Min;
        float2 tmax = transform.Position.xy + aabb.Max;
        
        float2 pmin = ProjectileTransform.Position.xy + ProjectileBounds.Min;
        float2 pmax = ProjectileTransform.Position.xy + ProjectileBounds.Max;

        bool collisionX =  tmax.x >= pmin.x &&
                            pmax.x >= tmin.x;
        bool collisionY =  tmax.y >= pmin.y &&
                            pmax.y >= tmin.y;
        
        if (collisionX && collisionY)
        {
            // we deal damage to player
            ECB.DestroyEntity(Enemy);
            ECB.DestroyEntity(ProjectileEntity);

            float t = math.clamp((float)GSC.CurrentWaveCount/(float)GDC.TotalWaves, 0, 1);
            GSC.CurrentKills += 1;
            
            float normalizedDifficultyValue = curveutility.evaluate(t, CBD);
            if (GSC.CurrentKills >= normalizedDifficultyValue * GDC.KillsOnFinalWave)
            {
                // increase wave count
                GSC.CurrentWaveCount += 1;                
            }        
            ECB.SetComponent(GameEntity, GSC);

            // Debug.Log("collided with player: " + PlayerVelocityRef.Value);
        }                               
    }
}