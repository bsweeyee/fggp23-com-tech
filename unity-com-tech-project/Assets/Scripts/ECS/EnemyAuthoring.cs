using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class EnemyAuthoring : MonoBehaviour
{
    public GameDataSO GameData;

    public class EnemyAuthoringBaker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            SpriteRenderer sr = authoring.GameData.EnemyPrefab.GetComponent<SpriteRenderer>();            
            Vector3 srScale = authoring.GameData.EnemyPrefab.transform.lossyScale;            

            Entity e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<EnemyTag>(e);
            AddComponent<ToSpawnFlag>(e);
            AddComponent<SpawnData>(e);
            AddComponent(e, new MovementData
            {
                Speed = authoring.GameData.EnemyMoveSpeed,
            });
            AddComponent(e, new AABBData {
                Min = new float2(-sr.bounds.size.x/2 * srScale.x/2, -sr.bounds.size.y/2 * srScale.y/2),
                Max = new float2(sr.bounds.size.x/2 * srScale.x/2, sr.bounds.size.y/2 * srScale.y/2),
                OriginalSize = new float2(sr.bounds.size.x * srScale.x/2, sr.bounds.size.y * srScale.x/2),
            });
        }
    }
}
