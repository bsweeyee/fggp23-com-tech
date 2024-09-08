using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateAfter(typeof(PlayerMoveSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyMoveSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        GameDataComponent gd = SystemAPI.GetSingleton<GameDataComponent>();
        foreach(var (pTag, pTransform) in SystemAPI.Query<PlayerTag, LocalToWorld>())
        {
            new EnemyMoveJob {
                DeltaTime = deltaTime,                
                PlayerTransformData = pTransform,
                GameData = gd,
            }.Schedule();            
        }
    }
}

public partial struct EnemyMoveJob : IJobEntity
{
    public float DeltaTime;    
    public LocalToWorld PlayerTransformData;
    public GameDataComponent GameData;
    private void Execute(in EnemyTag ptag, ref LocalTransform transform)
    {
        var direction = PlayerTransformData.Position - transform.Position;
        direction = math.normalize(direction);
        transform.Position += direction * DeltaTime * GameData.EnemyMoveSpeed;
    }
}
