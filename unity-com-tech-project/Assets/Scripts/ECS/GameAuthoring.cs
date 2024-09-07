using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Entities;
using Unity.Entities.Serialization;

public class GameAuthoring : MonoBehaviour
{
    [SerializeField] public GameDataSO GameData;
    public class GameAuthoringBaker : Baker<GameAuthoring>
    {
        public override void Bake(GameAuthoring authoring)
        {
            if (authoring.GameData == null) 
            {
                Debug.LogWarning($"{authoring.GetType().ToString()} Missing Game Data scriptable Object");
                return;
            }            
            
            // Game Entity
            Entity gameEntity = GetEntity(TransformUsageFlags.None);            
            Entity playerEntity = GetEntity(authoring.GameData.PlayerPrefab, TransformUsageFlags.None);
            Entity projectileEntity = GetEntity(authoring.GameData.ProjectilePrefab, TransformUsageFlags.None);
                    
            AddComponent(gameEntity, new GameDataComponent
            {
                // Player
                PlayerEntity = playerEntity,
                PlayerStartPosition = authoring.GameData.PlayerStartPosition,
                PlayerMoveSpeed = authoring.GameData.PlayerMoveSpeed,
                PlayerAngularSpeed = authoring.GameData.PlayerAngularSpeed,

                // Projectile
                ProjectileEntity = projectileEntity,
                ProjectileSpeed = authoring.GameData.ProjectileSpeed,    

                // Spawn
                SpawnEnemyStartPosition = authoring.GameData.EnemySpawnStartPosition,
                SpawnEnemyRate = authoring.GameData.EnemySpawnRate         
            });            
            Debug.Log("game authoring baked");            
        }
    }    
}


