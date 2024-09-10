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

        new FireProjectileJob 
        {
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            GameData = gameData,
            ECB = ecb,
            PrefabProjectileTransform = projectileTransform,
        }.Schedule();
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();        
    }
}

public partial struct FireProjectileJob : IJobEntity
{
    public double ElapsedTime;
    public GameDataComponent GameData;
    public EntityCommandBuffer ECB;
    public LocalTransform PrefabProjectileTransform;

    private void Execute(LocalToWorld localToWorld, LocalTransform transform, ref ProjectileShooterData shooterData, ref InputData inputData, MovementData mvd)
    {
        bool isShooting = inputData.InputState == 1 || inputData.InputState == 2;
        bool isCooldownDone = (ElapsedTime - shooterData.LastFireTime) > GameData.ProjectileShootCooldown;
        
        if (isShooting && isCooldownDone)
        {
            float division = 1.0f / (float)GameData.PlayerNumberOfShots;
            for(int i=0; i<GameData.PlayerNumberOfShots; i++)
            {
                var newProjectile = ECB.Instantiate(GameData.ProjectileEntity);
                
                ECB.SetComponent(newProjectile, new LocalTransform{
                    Position = transform.Position,
                    Rotation = PrefabProjectileTransform.Rotation,
                    Scale = PrefabProjectileTransform.Scale
                });
                float3 direction = new float3(0, 0, 0);
                if (GameData.PlayerNumberOfShots%2 == 0)
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
                
                ECB.SetComponent(newProjectile, new MovementData 
                { 
                    Direction = direction.xy, 
                    Speed = GameData.ProjectileSpeed + mvd.Speed
                });
                
            }
            shooterData.LastFireTime = ElapsedTime;
        }
    }
}
