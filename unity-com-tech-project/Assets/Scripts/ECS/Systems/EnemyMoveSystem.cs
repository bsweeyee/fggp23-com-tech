using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using System.Threading;
using Unity.Collections;
using UnityEditor;
using Unity.Burst;
using Unity.Entities.UniversalDelegates;

[UpdateAfter(typeof(PlayerMoveSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime; 
        CameraData cameraData = SystemAPI.GetSingleton<CameraData>();          
                
        foreach(var (pTag, pTransform) in SystemAPI.Query<PlayerTag, LocalToWorld>())
        {
            new EnemyMoveJob {
                DeltaTime = deltaTime,
                PlayerTransformData = pTransform,
                CameraData = cameraData,                
            }.ScheduleParallel();                         
        }       
    }
}

[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct EnemyMoveJob : IJobEntity
{
    public float DeltaTime;    
    public LocalToWorld PlayerTransformData;
    public CameraData CameraData;    
    
    [BurstCompile]
    private void Execute(EnabledRefRW<EnemyTag> eTag, ref LocalTransform transform, ref MovementData md)
    {
        if (eTag.ValueRO == false)
        {            
            transform.Position.xy = CameraData.Position + new float2(0, 10000);
            return;
        }        

        var direction = PlayerTransformData.Position - transform.Position;
        direction = math.normalize(direction);
        
        md.Direction = new float2(direction.xy);        

        transform.Position += direction * DeltaTime * md.Speed;
    }
}