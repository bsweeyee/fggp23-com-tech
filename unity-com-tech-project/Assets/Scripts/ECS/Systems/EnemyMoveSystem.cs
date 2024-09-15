using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

[UpdateAfter(typeof(PlayerMoveSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct EnemyMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<CameraData>()) return;

        float deltaTime = SystemAPI.Time.DeltaTime; 
        CameraData cameraData = SystemAPI.GetSingleton<CameraData>();
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);          
                
        foreach(var (pTag, pTransform) in SystemAPI.Query<PlayerTag, LocalToWorld>())
        {
            new EnemyMoveJob {
                DeltaTime = deltaTime,
                PlayerTransformData = pTransform,
                CameraData = cameraData,
                ECB = ecb.AsParallelWriter()                
            }.ScheduleParallel();
            state.Dependency.Complete();                         
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();       
    }
}

[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct EnemyMoveJob : IJobEntity
{
    public float DeltaTime;    
    public LocalToWorld PlayerTransformData;
    public CameraData CameraData;
    public EntityCommandBuffer.ParallelWriter ECB;    
    
    [BurstCompile]
    private void Execute([ChunkIndexInQuery] int chunkIndex, EnabledRefRW<EnemyTag> eTag, Entity Enemy, LocalTransform transform, ref MovementData md)
    {
        if (eTag.ValueRO == false)
        {
            ECB.SetComponent(chunkIndex, Enemy, new LocalTransform
            {
                Position = new float3(0, 0, -100),
                Rotation = transform.Rotation,
                Scale = transform.Scale,                    
            });
            return;
        }

        var direction = PlayerTransformData.Position - transform.Position;
        direction = math.normalize(direction);        
        md.Direction = new float2(direction.xy);   
        var newPos = transform.Position + direction * DeltaTime * md.Speed;

        ECB.SetComponent(chunkIndex, Enemy, new LocalTransform
        {
            Position = newPos,
            Rotation = transform.Rotation,
            Scale = transform.Scale,                    
        });   
    }
}