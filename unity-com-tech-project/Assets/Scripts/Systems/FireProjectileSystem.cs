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
        foreach(var (localToWorld, transform, shooterData) in SystemAPI.Query<LocalToWorld, LocalTransform, RefRW<ProjectileShooterData>>().WithAll<ProjectileShooterData>())
        {
            if (shooterData.ValueRO.ShouldFire)
            {
                var newProjectile = ecb.Instantiate(shooterData.ValueRO.Prefab);
                var projectileTransform = LocalTransform.FromPositionRotation(transform.Position,transform.Rotation);
                

                ecb.SetComponent(newProjectile, projectileTransform);
                // TODO check how to retrieve baked speed values
                ecb.SetComponent(newProjectile, new MovementData { Direction = localToWorld.Up.xy, Speed = 0 });

                shooterData.ValueRW.ShouldFire = false;
            }
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();        
    }
}
