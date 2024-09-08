using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
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
            Entity playerEntity = GetEntity(authoring.GameData.PlayerPrefab, TransformUsageFlags.Dynamic);
            Entity projectileEntity = GetEntity(authoring.GameData.ProjectilePrefab, TransformUsageFlags.Dynamic);
            Entity cameraEntity = GetEntity(authoring.GameData.CameraPrefab, TransformUsageFlags.None);            
            Entity enemyEntity = GetEntity(authoring.GameData.EnemyPrefab, TransformUsageFlags.Dynamic);

            AddComponent(gameEntity, new SpawnerData{
                LastSpawnTime = 0,             
            });
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
                ProjectileShootCooldown = authoring.GameData.ProjectileShootCooldown,    

                // Spawn
                EnemyEntity = enemyEntity,                
                EnemyMoveSpeed = authoring.GameData.EnemyMoveSpeed,
                SpawnEnemyStartPosition = authoring.GameData.EnemyMoveSpeed,
                SpawnEnemyRate = authoring.GameData.EnemySpawnRate,
                
                //Camera
                CameraEntity = cameraEntity,
                CameraBoundsPadding = authoring.GameData.CameraBoundsPadding

            });            
            Debug.Log("game authoring baked");            
        }
    }    
}


