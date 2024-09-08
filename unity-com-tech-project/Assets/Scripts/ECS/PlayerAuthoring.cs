using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    [SerializeField] public GameDataSO GameData;
    public class PlayerAuthoringBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            if (authoring.GameData == null) 
            {
                Debug.LogWarning($"{authoring.GetType().ToString()} Missing Game Data scriptable Object");
                return;
            } 

            Entity playerEntity = GetEntity(TransformUsageFlags.Dynamic);            
                        
            AddComponent<PlayerTag>(playerEntity);            
            AddComponent<InputData>(playerEntity);            
            AddComponent(playerEntity, new MovementData {
                Speed = authoring.GameData.PlayerMoveSpeed,
                AngularSpeed = 0
            });
            AddComponent(playerEntity, new ProjectileShooterData {
                LastFireTime = 0,
            });
            
            Debug.Log("player authoring baked");
        }
    }
}