using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    // TODO move to ScriptableObjects data file
    public float MoveSpeed;
    public GameObject ProjectilePrefab;

    class PlayerAuthoringBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity playerEntity = GetEntity(TransformUsageFlags.Dynamic);
            
            // add player components
            AddComponent<PlayerTag>(playerEntity);
            AddComponent<PlayerMoveInput>(playerEntity);
            AddComponent(playerEntity, new MovementData 
            {
                Speed = authoring.MoveSpeed
            });

            // add projectile components
            AddComponent(playerEntity, new ProjectileShooterData {
                Prefab = GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.Dynamic),
                ShouldFire = false
            });
        }
    }
}
