using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct FireProjectileSystem : ISystem
{    
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
        var gameData = SystemAPI.GetSingleton<GameDataComponent>();
        var projectileTransform = SystemAPI.GetComponent<LocalTransform>(gameData.ProjectileEntity);
        
        // retrieve EnemyTags that are disabled
        
        foreach(var (localToWorld, shooterData, inputData, movementData) in SystemAPI.Query<LocalToWorld, RefRW<ProjectileShooterData>, InputData, MovementData>())
        {
            bool isShooting = inputData.InputState == 1 || inputData.InputState == 2;
            bool isCooldownDone = (SystemAPI.Time.ElapsedTime - shooterData.ValueRO.LastFireTime) > gameData.ProjectileShootCooldown;
            
            if (isShooting && isCooldownDone)
            {
                shooterData.ValueRW.LastFireTime = SystemAPI.Time.ElapsedTime;
                float division = 1.0f/(float)gameData.PlayerNumberOfShots;
                
                int index = 0;
                foreach(var (pData, pTag, spawnFlag, entity) in SystemAPI.Query<RefRW<SpawnData>, EnabledRefRW<ProjectileTag>, EnabledRefRW<ToSpawnFlag>>().WithEntityAccess().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
                { 
                    if ( pTag.ValueRO == false )
                    {
                        pData.ValueRW.SpawnIndex = index;
                        pTag.ValueRW = true;
                        spawnFlag.ValueRW = true;
                        index++;                    
                    }                
                    if (index >= gameData.PlayerNumberOfShots)
                    {
                        break;
                    }
                }

                new FireProjectileJob
                {                    
                    GameData = gameData,
                    // ECB = ecb,
                    ECB = ecb.AsParallelWriter(),
                    PrefabProjectileTransform = projectileTransform,                    
                    Division = division,
                    ShooterLocalToWorld = localToWorld,
                    ShooterMovementData = movementData,
                }.ScheduleParallel();
                state.Dependency.Complete();                
            }    
        }                

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

public partial struct FireProjectileJob : IJobEntity
{    
    // public EntityCommandBuffer ECB;
    public EntityCommandBuffer.ParallelWriter ECB;
    public GameDataComponent GameData;
    public LocalTransform PrefabProjectileTransform;
    public LocalToWorld ShooterLocalToWorld;
    public MovementData ShooterMovementData;
    public float Division;

    public void Execute([ChunkIndexInQuery] int chunkIndex, Entity EntityToSpawn, SpawnData pData, EnabledRefRW<ProjectileTag> pTag, EnabledRefRW<ToSpawnFlag> toSpawnFlag)
    // public void Execute()
    {
        // ECB.SetComponentEnabled<ProjectileTag>(EntityToSpawn, true);                
        // ECB.SetComponent(EntityToSpawn, new LocalTransform{
        //     Position = ShooterLocalToWorld.Position,
        //     Rotation = PrefabProjectileTransform.Rotation,
        //     Scale = PrefabProjectileTransform.Scale
        // });

        if (toSpawnFlag.ValueRO == true)
        {
            // ECB.SetComponentEnabled<ProjectileTag>(chunkIndex, EntityToSpawn, true);                
            ECB.SetComponentEnabled<ToSpawnFlag>(chunkIndex, EntityToSpawn, false);
            
            ECB.SetComponent(chunkIndex, EntityToSpawn, new LocalTransform{
                Position = ShooterLocalToWorld.Position,
                Rotation = PrefabProjectileTransform.Rotation,
                Scale = PrefabProjectileTransform.Scale
            });

            float3 direction = new float3(0, 0, 0);
            if (GameData.PlayerNumberOfShots%2 == 0)
            {
                float3 right = (pData.SpawnIndex%2 == 0) ? ShooterLocalToWorld.Right : -ShooterLocalToWorld.Right;
                int d = (int) math.floor(pData.SpawnIndex/2.0f);
                direction = math.lerp(ShooterLocalToWorld.Up, right, (d+1)*Division);
            }
            else
            {
                if (pData.SpawnIndex == 0) { direction = ShooterLocalToWorld.Up; }
                else
                {
                    int index = pData.SpawnIndex-1;
                    float3 right = (index%2 == 0) ? ShooterLocalToWorld.Right : -ShooterLocalToWorld.Right;
                    int d = (int) math.floor(index/2.0f);
                    direction = math.lerp(ShooterLocalToWorld.Up, right, (d+1)*Division);
                }                        
            }                    
            
            // ECB.SetComponent(EntityToSpawn, new MovementData 
            // { 
            //     Direction = direction.xy, 
            //     Speed = GameData.ProjectileSpeed + ShooterMovementData.Speed
            // });             
            ECB.SetComponent(chunkIndex, EntityToSpawn, new MovementData 
            { 
                Direction = direction.xy, 
                Speed = GameData.ProjectileSpeed + ShooterMovementData.Speed
            });             
        }
    }    
}
