using UnityEngine;
using Unity.Entities;

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
                PlayerNumberOfShots = authoring.GameData.InitialNumberOfShots,
                PlayerStartHealth = authoring.GameData.PlayerHP,

                // Projectile
                ProjectileEntity = projectileEntity,
                ProjectileSpeed = authoring.GameData.ProjectileSpeed,
                ProjectileShootCooldown = authoring.GameData.ProjectileShootCooldown,    

                // Spawn
                EnemyEntity = enemyEntity,                
                EnemyMoveSpeed = authoring.GameData.EnemyMoveSpeed,
                SpawnEnemyStartPosition = authoring.GameData.EnemyMoveSpeed,
                SpawnEnemyRate = authoring.GameData.EnemySpawnRate,
                                
                CameraBoundsPadding = authoring.GameData.CameraBoundsPadding,

                // Game
                SpawnCount = authoring.GameData.SpawnCount,
                KillsOnFinalWave = authoring.GameData.KillsOnFinalWave,
                TotalWaves = authoring.GameData.TotalWaves,
                WaitTimeBetweenWaves = authoring.GameData.WaitTimeBetweenWaves
            });
            AddComponent(gameEntity, new GameStateComponent 
            {
                CurrentState = 0,
                CurrentWaveCount = 1,
                CurrentKills = 0
            });
            AddComponent(gameEntity, new CameraData
            {
                BoundsPadding = authoring.GameData.CameraBoundsPadding,                
            });

            DynamicBuffer<CurveBufferData> difficultyCurveBuffer = AddBuffer<CurveBufferData>(gameEntity);            
            for (int i=0; i<256; i++)
            {
                float output = authoring.GameData.DifficultyCurve.Evaluate((float)i/256.0f);
                difficultyCurveBuffer.Add(new CurveBufferData{ Value = output });
            }                     
            Debug.Log("game authoring baked");            
        }
    }    
}


