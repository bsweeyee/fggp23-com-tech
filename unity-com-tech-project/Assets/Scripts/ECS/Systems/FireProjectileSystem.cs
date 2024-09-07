using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct FireProjectileSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
        var gameData = SystemAPI.GetSingleton<GameDataComponent>();

        foreach(var (localToWorld, transform, shooterData) in SystemAPI.Query<LocalToWorld, LocalTransform, RefRW<ProjectileShooterData>>().WithAll<ProjectileShooterData>())
        {
            if (shooterData.ValueRO.ShouldFire)
            {
                var newProjectile = ecb.Instantiate(gameData.ProjectileEntity);
                var projectileTransform = LocalTransform.FromPositionRotation(transform.Position,transform.Rotation);
                
                ecb.SetComponent(newProjectile, projectileTransform);
                // TODO check how to retrieve baked speed values
                ecb.SetComponent(newProjectile, new MovementData 
                { 
                    Direction = localToWorld.Up.xy, 
                    Speed = gameData.ProjectileSpeed 
                });

                shooterData.ValueRW.ShouldFire = false;
                Debug.Log("projectile fired!");
            }
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();        
    }
}
