using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(PlayerMoveSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyProjectileCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);        
        Entity gameEntity = SystemAPI.GetSingletonEntity<GameDataComponent>();
        GameStateComponent gsc = SystemAPI.GetComponent<GameStateComponent>(gameEntity);
        GameDataComponent gdc = SystemAPI.GetComponent<GameDataComponent>(gameEntity);
        DynamicBuffer<CurveBufferData> cbd = SystemAPI.GetBuffer<CurveBufferData>(gameEntity);

        float t = math.clamp((float)gsc.CurrentWaveCount/(float)gdc.TotalWaves, 0, 1);
        float normalizedDifficultyValue = curveutility.evaluate(t, cbd);        
               
        foreach(var (projTag, projTransform, projAABB, entity) in SystemAPI.Query<ProjectileTag, LocalToWorld, AABBData>().WithEntityAccess())
        {            
            new EnemyProjectileCollisionJob
            {
                ProjectileTransform = projTransform,
                ProjectileBounds = projAABB,
                ProjectileEntity = entity,
                ECB = ecb,
                NormalizedDifficultyValue = normalizedDifficultyValue,
                
                GameEntity = gameEntity,
                GSC = gsc,
                GDC = gdc,
            }.Schedule();
        }
        
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);            
        ecb.Dispose();          
    }
}

[BurstCompile]
public partial struct EnemyProjectileCollisionJob : IJobEntity
{
    public LocalToWorld ProjectileTransform;
    public AABBData ProjectileBounds;
    public Entity ProjectileEntity;
    public EntityCommandBuffer ECB;
    public float NormalizedDifficultyValue;
    
    public Entity GameEntity;
    public GameStateComponent GSC;
    public GameDataComponent GDC; 
    
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

            GSC.CurrentKills += 1;
            
            if (GSC.CurrentKills >= NormalizedDifficultyValue * GDC.KillsOnFinalWave)
            {
                // increase wave count
                GSC.CurrentWaveCount += 1;                
            }        
            ECB.SetComponent(GameEntity, GSC);
        }                               
    }
}
