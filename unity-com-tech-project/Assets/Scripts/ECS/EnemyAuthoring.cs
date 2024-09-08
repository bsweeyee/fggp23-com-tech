using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class EnemyAuthoring : MonoBehaviour
{
    public GameDataSO GameData;

    public class EnemyAuthoringBaker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            Entity e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<EnemyTag>(e);
            AddComponent(e, new MovementData{
                Speed = authoring.GameData.EnemyMoveSpeed,
            });
        }
    }
}
