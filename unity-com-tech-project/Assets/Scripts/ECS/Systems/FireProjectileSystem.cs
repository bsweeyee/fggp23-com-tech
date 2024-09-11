using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct FireProjectileSystem : ISystem
{    
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
        var gameData = SystemAPI.GetSingleton<GameDataComponent>();
        var projectileTransform = SystemAPI.GetComponent<LocalTransform>(gameData.ProjectileEntity);

        NativeArray<Entity> enititesToSpawn = new NativeArray<Entity>(gameData.PlayerNumberOfShots, Allocator.TempJob);
                    
        // retrieve EnemyTags that are disabled
        int index = 0;
        foreach(var (eTag, entity) in SystemAPI.Query<EnabledRefRW<EnemyTag>>().WithEntityAccess().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
        {                                                
            if ( eTag.ValueRO == false )
            {
                enititesToSpawn[index] = entity;
                index++;                    
            }                
            if (index >= gameData.PlayerNumberOfShots)
            {
                break;
            }
        }

        for(int i=0; i<index; i++)
        {
            new FireProjectileJob
            {
                ElapsedTime = SystemAPI.Time.ElapsedTime,
                GameData = gameData,
                ECB = ecb,
                PrefabProjectileTransform = projectileTransform,                
                EntitiesToSpawn = enititesToSpawn,
            }.Schedule();
        }        

        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        enititesToSpawn.Dispose();        
    }
}

[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct FireProjectileJob : IJobEntity
{
    public double ElapsedTime;
    public EntityCommandBuffer ECB;
    public GameDataComponent GameData;
    public LocalTransform PrefabProjectileTransform;

    public NativeArray<Entity> EntitiesToSpawn;

    public void Execute(LocalToWorld ShooterLocalToWorld, RefRW<ProjectileShooterData> ShooterData, InputData InputData, MovementData ShooterMovementData)
    {
        bool isShooting = InputData.InputState == 1 || InputData.InputState == 2;
        bool isCooldownDone = (ElapsedTime - ShooterData.ValueRO.LastFireTime) > GameData.ProjectileShootCooldown;
        
        if (isShooting && isCooldownDone)
        {
            ShooterData.ValueRW.LastFireTime = ElapsedTime;
            float division = 1.0f / (float)EntitiesToSpawn.Length;
            for(int i=0; i<EntitiesToSpawn.Length; i++)
            {
                ECB.SetComponentEnabled<ProjectileTag>(EntitiesToSpawn[i], true);                
                ECB.SetComponent(EntitiesToSpawn[i], new LocalTransform{
                    Position = ShooterLocalToWorld.Position,
                    Rotation = PrefabProjectileTransform.Rotation,
                    Scale = PrefabProjectileTransform.Scale
                });
                float3 direction = new float3(0, 0, 0);
                if (GameData.PlayerNumberOfShots%2 == 0)
                {
                    float3 right = (i%2 == 0) ? ShooterLocalToWorld.Right : -ShooterLocalToWorld.Right;
                    int d = (int) math.floor(i/2.0f);
                    direction = math.lerp(ShooterLocalToWorld.Up, right, (d+1)*division);
                }
                else
                {
                    if (i == 0) { direction = ShooterLocalToWorld.Up; }
                    else
                    {
                        int index = i-1;
                        float3 right = (index%2 == 0) ? ShooterLocalToWorld.Right : -ShooterLocalToWorld.Right;
                        int d = (int) math.floor(index/2.0f);
                        direction = math.lerp(ShooterLocalToWorld.Up, right, (d+1)*division);
                    }                        
                }                    
                
                ECB.SetComponent(EntitiesToSpawn[i], new MovementData 
                { 
                    Direction = direction.xy, 
                    Speed = GameData.ProjectileSpeed + ShooterMovementData.Speed
                });
            }
        }
    }    
}
