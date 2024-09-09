using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

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
                var newProjectile = ecb.Instantiate(gameData.ProjectileEntity);
                
                ecb.SetComponent(newProjectile, new LocalTransform{
                    Position = transform.Position,
                    Rotation = projectileTransform.Rotation,
                    Scale = projectileTransform.Scale
                });
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
