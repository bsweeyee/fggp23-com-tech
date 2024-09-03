using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial struct SpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state)
    {
    }
    
    public void OnUpdate(ref SystemState state)
    {
        foreach(RefRW<Spawner> spawner in SystemAPI.Query<RefRW<Spawner>>())
        {
            if (spawner.ValueRO.NextSpawnTime < SystemAPI.Time.ElapsedTime)
            {
                Entity newEntity = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);
                float3 pos = new float3(spawner.ValueRO.SpawnPosition.x,spawner.ValueRO.SpawnPosition.y,0);
                state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(pos));
                spawner.ValueRW.NextSpawnTime = (float) SystemAPI.Time.ElapsedTime + spawner.ValueRO.SpawnRate;
            }
        }
    }
}
