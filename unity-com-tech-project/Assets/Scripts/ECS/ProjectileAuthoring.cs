using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ProjectileAuthoring : MonoBehaviour
{
    public GameDataSO GameData;
    public class ProjectileAuthoringBaker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            if (authoring.GameData == null) 
            {
                Debug.LogWarning($"{authoring.GetType().ToString()} Missing Game Data scriptable Object");
                return;
            }

            Entity entity = GetEntity(TransformUsageFlags.Dynamic); 
            SpriteRenderer sr = authoring.GameData.ProjectilePrefab.GetComponent<SpriteRenderer>();
            Vector3 srScale = authoring.GameData.ProjectilePrefab.transform.lossyScale;
            Vector3 size = sr.bounds.size; 

            AddComponent<ProjectileTag>(entity);
            AddComponent<ToSpawnFlag>(entity);            
            AddComponent<SpawnData>(entity);            
            AddComponent<MovementData>(entity);
            AddComponent(entity, new AABBData {
                Min = new float2(-size.x/2 * srScale.x/2, -size.y/2 * srScale.y/2),
                Max = new float2(size.x/2 * srScale.x/2, size.y/2 * srScale.y/2),
                OriginalSize = new float2(size.x * srScale.x/2, size.y * srScale.y/2),
            });
            Debug.Log("projectile authoring baked");
        }
    }
}
