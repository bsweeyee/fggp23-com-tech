using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public float SpawnRate;

    class SpawnerBaker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {            
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Spawner
            {
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                SpawnPosition = new float2(0, 0),
                NextSpawnTime = 0,
                SpawnRate = authoring.SpawnRate              
            });
        }
    }
}
