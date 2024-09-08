using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using System.Threading;

[UpdateAfter(typeof(PlayerMoveSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyMoveSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        GameDataComponent gd = SystemAPI.GetSingleton<GameDataComponent>();
        foreach(var (pTag, pTransform, pAABB) in SystemAPI.Query<PlayerTag, LocalToWorld, AABBData>())
        {
            new EnemyMoveJob {
                DeltaTime = deltaTime,
                PlayerTransformData = pTransform,
                GameData = gd,
            }.Schedule();

            new EnemyPlayerCollisionJob {
                PlayerTransform = pTransform,
                PlayerBounds = pAABB
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

public partial struct EnemyPlayerCollisionJob : IJobEntity
{
    public LocalToWorld PlayerTransform;
    public AABBData PlayerBounds;
    private void Execute(in EnemyTag eTag, in AABBData aabb, in LocalTransform transform)
    {
        // check intersection
        float2 tmin = transform.Position.xy + aabb.Min;
        float2 tmax = transform.Position.xy + aabb.Max;
        
        float2 pmin = PlayerTransform.Position.xy + PlayerBounds.Min;
        float2 pmax = PlayerTransform.Position.xy + PlayerBounds.Max;

        bool collisionX =  tmax.x >= pmin.x &&
                            pmax.x >= tmin.x;
        bool collisionY =  tmax.y >= pmin.y &&
                            pmax.y >= tmin.y;
        if (collisionX && collisionY)
        {
            // we deal damage to player
            Debug.Log("collided with player!");
        }                               
    }
}
