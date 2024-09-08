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
        foreach(RefRW<SpawnerData> spawner in SystemAPI.Query<RefRW<SpawnerData>>())
        {
            // if (spawner.ValueRO.NextEnemySpawnTime < SystemAPI.Time.ElapsedTime)
            // {
            // Entity newEntity = state.EntityManager.Instantiate(spawner.ValueRO.EnemyPrefab);
                // float3 pos = new float3(spawner.ValueRO.EnemySpawnStartPosition.x,spawner.ValueRO.EnemySpawnStartPosition.y,0);
                // state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(pos));
                // spawner.ValueRW.NextEnemySpawnTime = (float) SystemAPI.Time.ElapsedTime + spawner.ValueRO.EnemySpawnRate;
            // }
        }
    }
}
