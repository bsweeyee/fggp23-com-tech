using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour
{
    public GameDataSO GameData;
    public class SpawnBaker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            Entity e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new SpawnerData{
                LastSpawnTime = 0,             
            });
        }
    }
}
