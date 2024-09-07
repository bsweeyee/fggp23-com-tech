using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ProjectileAuthoring : MonoBehaviour
{
    public class ProjectileAuthoringBaker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);            
            AddComponent<ProjectileTag>(entity);
            AddComponent<MovementData>(entity);
            Debug.Log("projectile authoring baked");
        }
    }
}
