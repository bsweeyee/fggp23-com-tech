using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct EnemySpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var gameDataEntity = SystemAPI.GetSingletonEntity<GameDataComponent>();

        var gameData = SystemAPI.GetComponent<GameDataComponent>(gameDataEntity);        
        var spawnerData = SystemAPI.GetComponentRW<SpawnerData>(gameDataEntity);
        var gameState = SystemAPI.GetComponent<GameStateComponent>(gameDataEntity);        
        var diffcultyCurve = SystemAPI.GetBuffer<CurveBufferData>(gameDataEntity);

        var cameraEntity = SystemAPI.GetSingletonEntity<CameraData>();
        var cameraData = SystemAPI.GetComponent<CameraData>(cameraEntity);

        var enemyInitialLocalTransform = SystemAPI.GetComponent<LocalTransform>(gameData.EnemyEntity);

        double t = SystemAPI.Time.ElapsedTime - spawnerData.ValueRO.LastSpawnTime;        
        if (t > gameData.SpawnEnemyRate)
        {
            spawnerData.ValueRW.LastSpawnTime = SystemAPI.Time.ElapsedTime;            

            float wt = (float)gameState.CurrentWaveCount / (float)gameData.TotalWaves;
            float nf = gameData.SpawnCount.x + gameData.SpawnCount.y * curveutility.evaluate(wt, diffcultyCurve);            
            int n = (int)math.floor(nf);
            Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)gameState.SystemTimeWhenGameStarted);            
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
            
            NativeArray<Entity> enititesToSpawn = new NativeArray<Entity>(n, Allocator.TempJob);
            
            // retrieve EnemyTags that are disabled
            int index = 0;
            foreach(var (eTag, entity) in SystemAPI.Query<EnabledRefRW<EnemyTag>>().WithEntityAccess().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {                                                
                if ( eTag.ValueRO == false )
                {
                    enititesToSpawn[index] = entity;
                    index++;                    
                }                
                if (index >= n)
                {
                    break;
                }
            }

            // send to job to spawn
            for(int i=0; i<index; i++)
            {
                new SpawnEnemyJob
                {
                    ECB = ecb,
                    RandomNumberA = (random.NextFloat() * 2) - 1,
                    RandomNumberB = (random.NextFloat() * 2) - 1,
                    RandomNumberC = (random.NextFloat() * 2) - 1,
                    EnemyEntityInitialLocalTransform = enemyInitialLocalTransform,                    
                    EntityToSpawn = enititesToSpawn[i],
                }.Schedule();
            }
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            enititesToSpawn.Dispose();
            // Debug.Log($"{n}, {gameState.CurrentKills}, {gameState.CurrentWaveCount}");                            

        }    
    }
}

public partial struct SpawnEnemyJob : IJobEntity
{
    public EntityCommandBuffer ECB;
    public float RandomNumberA;
    public float RandomNumberB;
    public float RandomNumberC;
    public LocalTransform EnemyEntityInitialLocalTransform;
    public Entity EntityToSpawn;            

    public void Execute(in GameDataComponent gameData, in CameraData cameraData)
    {                                      
        // var ee = gameData.EnemyEntity;        
        // var enemyEntity = ECB.Instantiate(ee);
        ECB.SetComponentEnabled<EnemyTag>(EntityToSpawn, true);

        int s1 = (int)math.sign(RandomNumberA); 
        int s2 = (int)math.sign(RandomNumberB);                
        float2 offset = new float2(0, 0);
        
        if (s2 > 0)
        {
            offset = new float2(s1 * cameraData.Bounds.x/2 + RandomNumberA * cameraData.BoundsPadding.x, RandomNumberC * cameraData.Bounds.y/2);                    
        }
        else
        {
            offset = new float2(RandomNumberC * cameraData.Bounds.x/2, s1 * cameraData.Bounds.y/2 + RandomNumberA * cameraData.BoundsPadding.y);
        }        
                        
        float2 randomPosition = cameraData.Position + offset; 
                        
        var lt = EnemyEntityInitialLocalTransform;                
        ECB.SetComponent(EntityToSpawn, new LocalTransform {
            Position = new float3(randomPosition.xy, 0),
            Rotation = lt.Rotation,
            Scale = lt.Scale
        });                
    }
}
