using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor.Search;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct FireProjectileSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
        var gameData = SystemAPI.GetSingleton<GameDataComponent>();


        foreach(var (localToWorld, transform, shooterData, inputData, mvd) in SystemAPI.Query<LocalToWorld, LocalTransform, RefRW<ProjectileShooterData>, RefRW<InputData>, MovementData>().WithAll<ProjectileShooterData>())
        {
            bool isShooting = inputData.ValueRO.InputState == 1 || inputData.ValueRO.InputState == 2;
            bool isCooldownDone = (SystemAPI.Time.ElapsedTime - shooterData.ValueRO.LastFireTime) > gameData.ProjectileShootCooldown;
            
            if (isShooting && isCooldownDone)
            {
                var newProjectile = ecb.Instantiate(gameData.ProjectileEntity);
                var projectileTransform = LocalTransform.FromPositionRotation(transform.Position,transform.Rotation);
                
                ecb.SetComponent(newProjectile, projectileTransform);
                // TODO check how to retrieve baked speed values
                ecb.SetComponent(newProjectile, new MovementData 
                { 
                    Direction = localToWorld.Up.xy, 
                    Speed = gameData.ProjectileSpeed + mvd.Speed
                });
                
                shooterData.ValueRW.LastFireTime = SystemAPI.Time.ElapsedTime;                
            }
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();        
    }
}
