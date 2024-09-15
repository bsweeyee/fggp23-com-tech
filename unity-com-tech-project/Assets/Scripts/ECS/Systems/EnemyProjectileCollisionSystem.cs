using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateAfter(typeof(PlayerMoveSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyProjectileCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<GameStateComponent>()) return;
        if (!SystemAPI.HasSingleton<GameDataComponent>()) return;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);        
        Entity gameEntity = SystemAPI.GetSingletonEntity<GameDataComponent>();
        GameStateComponent gsc = SystemAPI.GetComponent<GameStateComponent>(gameEntity);
        GameDataComponent gdc = SystemAPI.GetComponent<GameDataComponent>(gameEntity);
        DynamicBuffer<CurveBufferData> cbd = SystemAPI.GetBuffer<CurveBufferData>(gameEntity);

        float t = math.clamp((float)(gsc.CurrentWaveCount + 1)/(float)gdc.TotalWaves, 0, 1);
        float nextNormalizedDifficultyValue = curveutility.evaluate(t, cbd);        

        double elapsedTime = SystemAPI.Time.ElapsedTime;         
        foreach(var (projTag, projTransform, projAABB, entity) in SystemAPI.Query<ProjectileTag, LocalTransform, AABBData>().WithEntityAccess())
        {            
            new EnemyProjectileCollisionJob
            {
                ProjectileTransform = projTransform,
                ProjectileBounds = projAABB,
                ProjectileEntity = entity,
                ECB = ecb.AsParallelWriter(),
                // ECB = ecb,
                NextNormalizedDifficultyValue = nextNormalizedDifficultyValue,
                
                GameEntity = gameEntity,
                GSC = gsc,
                GDC = gdc,
                ElapsedTime = elapsedTime,
            }.ScheduleParallel();
            state.Dependency.Complete();
        }
        
        ecb.Playback(state.EntityManager);            
        ecb.Dispose();          
    }
}

[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct EnemyProjectileCollisionJob : IJobEntity
{
    public LocalTransform ProjectileTransform;
    public AABBData ProjectileBounds;
    public Entity ProjectileEntity;
    // public EntityCommandBuffer ECB;
    public EntityCommandBuffer.ParallelWriter ECB;
    public float NextNormalizedDifficultyValue;
    
    public Entity GameEntity;
    public GameStateComponent GSC;
    public GameDataComponent GDC; 

    public double ElapsedTime;
    [BurstCompile]
    private void Execute([ChunkIndexInQuery] int chunkIndex, Entity Enemy, EnabledRefRW<EnemyTag> eTag, in AABBData aabb, in LocalTransform transform, MovementData md)
    {
        if (eTag.ValueRO == false) return;
        
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
            ECB.SetComponentEnabled<EnemyTag>(chunkIndex, Enemy, false);
            ECB.SetComponentEnabled<ProjectileTag>(chunkIndex, ProjectileEntity, false);
           
            GSC.CurrentKills += 1;
            
            if (GSC.CurrentKills >= GSC.TargetKillCount)
            {
                // increase wave count
                GSC.CurrentWaveCount += 1; 
                if (GSC.CurrentWaveCount > GDC.TotalWaves)
                {
                    GSC.CurrentState = 4; // go to win state
                }
                else
                {
                    GSC.CurrentState = 3; // go to next wave state
                    GSC.LastWaveTimeEnded = ElapsedTime; 
                    GSC.TargetKillCount += (int)(NextNormalizedDifficultyValue * GDC.KillsOnFinalWave);                
                }
            }        
            ECB.SetComponent(chunkIndex, GameEntity, GSC);
            // ECB.SetComponent(GameEntity, GSC);

        }                               
    }
}
