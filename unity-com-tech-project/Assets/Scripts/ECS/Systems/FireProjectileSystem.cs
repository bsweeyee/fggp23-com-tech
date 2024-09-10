using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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

        foreach(var (localToWorld, transform, shooterData, inputData, mvd) in SystemAPI.Query<LocalToWorld, LocalTransform, RefRW<ProjectileShooterData>, RefRW<InputData>, MovementData>().WithAll<ProjectileShooterData>())
        {
            bool isShooting = inputData.ValueRO.InputState == 1 || inputData.ValueRO.InputState == 2;
            bool isCooldownDone = (SystemAPI.Time.ElapsedTime - shooterData.ValueRO.LastFireTime) > gameData.ProjectileShootCooldown;
            
            if (isShooting && isCooldownDone)
            {
                float division = 1.0f / (float)gameData.PlayerNumberOfShots;
                for(int i=0; i<gameData.PlayerNumberOfShots; i++)
                {
                    var newProjectile = ecb.Instantiate(gameData.ProjectileEntity);
                    
                    ecb.SetComponent(newProjectile, new LocalTransform{
                        Position = transform.Position,
                        Rotation = projectileTransform.Rotation,
                        Scale = projectileTransform.Scale
                    });
                    float3 direction = new float3(0, 0, 0);
                    if (gameData.PlayerNumberOfShots%2 == 0)
                    {
                        float3 right = (i%2 == 0) ? localToWorld.Right : -localToWorld.Right;
                        int d = (int) math.floor(i/2.0f);
                        direction = math.lerp(localToWorld.Up, right, (d+1)*division);
                    }
                    else
                    {
                        if (i == 0) { direction = localToWorld.Up; }
                        else
                        {
                            int index = i-1;
                            float3 right = (index%2 == 0) ? localToWorld.Right : -localToWorld.Right;
                            int d = (int) math.floor(index/2.0f);
                            direction = math.lerp(localToWorld.Up, right, (d+1)*division);
                        }                        
                    }                    
                    
                    ecb.SetComponent(newProjectile, new MovementData 
                    { 
                        Direction = direction.xy, 
                        Speed = gameData.ProjectileSpeed + mvd.Speed
                    });
                    
                }
                shooterData.ValueRW.LastFireTime = SystemAPI.Time.ElapsedTime;                
            }
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();        
    }
}
