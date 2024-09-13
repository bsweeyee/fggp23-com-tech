using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using System;
using System.Diagnostics;

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
            int n = (int)math.ceil(nf);
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
                        
            // retrieve EnemyTags that are disabled
            int index = 0;
            foreach(var (eTag, spawnFlag, spawnIndexData, entity) in SystemAPI.Query<EnabledRefRW<EnemyTag>, EnabledRefRW<ToSpawnFlag>, RefRW<SpawnData>>().WithEntityAccess().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {                                                
                if ( eTag.ValueRO == false )
                {
                    spawnIndexData.ValueRW.SpawnIndex = index;
                    eTag.ValueRW = true;
                    spawnFlag.ValueRW = true;
                    index++;                    
                }                
                if (index >= n)
                {
                    break;
                }
            }

             new SpawnEnemyJob
            {
                // ECB = ecb,
                ECB = ecb.AsParallelWriter(),
                EnemyEntityInitialLocalTransform = enemyInitialLocalTransform,                    
                gameData = gameData,
                cameraData = cameraData
            }.ScheduleParallel();
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            // Debug.Log($"{n}, {gameState.CurrentKills}, {gameState.CurrentWaveCount}");
        }    
    }
}
public partial struct SpawnEnemyJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    public LocalTransform EnemyEntityInitialLocalTransform;
    public GameDataComponent gameData;
    public CameraData cameraData;

    public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, EnabledRefRW<EnemyTag> eTag, EnabledRefRW<ToSpawnFlag> toSpawnFlag, SpawnData spawnData)
    {
        if (toSpawnFlag.ValueRO == true)
        {            
            // ECB.SetComponentEnabled<EnemyTag>(chunkIndex, entity, true);
            ECB.SetComponentEnabled<ToSpawnFlag>(chunkIndex, entity, false);

            Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)(System.DateTime.Now.Ticks + spawnData.SpawnIndex));            
            for(int i=0; i<spawnData.SpawnIndex; i++) random.NextFloat();

            float RandomNumberA = random.NextFloat() * 2 - 1;
            float RandomNumberB = random.NextFloat() * 2 - 1;
            float RandomNumberC = random.NextFloat() * 2 - 1;

            // UnityEngine.Debug.Log($"{RandomNumberA}, {RandomNumberB}, {RandomNumberC}");
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
                                                
            ECB.SetComponent(chunkIndex, entity, new LocalTransform {
                Position = new float3(randomPosition.xy, 0),
                Rotation = EnemyEntityInitialLocalTransform.Rotation,
                Scale = EnemyEntityInitialLocalTransform.Scale
            });     
        }
    }
}
