using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(InitializationSystemGroup),OrderLast = true)]
public partial class GameSystem : SystemBase
{
    private GameInput InputActions;
    private Entity PlayerEntity;
    private Entity GameDataEntity;

    private int currentProjectileEntityCount;    
    private int currentEnemyEntityCount;    

    protected override void OnCreate()
    {
        base.OnCreate();        
        
        InputActions = new GameInput();
    }    

    protected override void OnStartRunning()
    {
        base.OnStartRunning();            
        InputActions.Enable();        
        InputActions.Gameplay.Shoot.performed += OnShoot;
        InputActions.Gameplay.Shoot.canceled += OnShootCancel;

        GameDataComponent gameDataComponent = new GameDataComponent();
        CameraData cameraData = new CameraData();
        GameStateComponent gameStateComponent = new GameStateComponent();                

        foreach (var (gd, entity) in SystemAPI.Query<GameDataComponent>().WithEntityAccess())
        {
            GameDataEntity = entity;
            gameDataComponent = gd;
        }

        DynamicBuffer<CurveBufferData> cbd = SystemAPI.GetBuffer<CurveBufferData>(GameDataEntity);
        gameStateComponent = SystemAPI.GetComponent<GameStateComponent>(GameDataEntity);
        gameStateComponent.TargetKillCount = (int)(curveutility.evaluate((float)gameStateComponent.CurrentWaveCount/(float)gameDataComponent.TotalWaves, cbd) * gameDataComponent.KillsOnFinalWave);
        EntityManager.SetComponentData(GameDataEntity, gameStateComponent);

        var playerEntity = EntityManager.Instantiate(gameDataComponent.PlayerEntity);
        var playerHealth = SystemAPI.GetComponent<PlayerHealthData>(playerEntity);            
        var playerTransform = SystemAPI.GetComponentRW<LocalTransform>(playerEntity);
        playerTransform.ValueRW.Position.xy = gameDataComponent.PlayerStartPosition;
        PlayerEntity = playerEntity;            

        cameraData = SystemAPI.GetComponent<CameraData>(GameDataEntity);                               

        SystemAPI.SetSingleton(gameDataComponent);
        SystemAPI.SetSingleton(cameraData);
        SystemAPI.SetSingleton(gameStateComponent);
        SystemAPI.SetSingleton(playerHealth);

        // pre-instantiate enemy entities and set enabled component to false
        EntityManager.SetComponentEnabled<EnemyTag>(gameDataComponent.EnemyEntity, false);        
        EntityManager.SetComponentEnabled<ToSpawnFlag>(gameDataComponent.EnemyEntity, false);

        var lte = EntityManager.GetComponentData<LocalTransform>(gameDataComponent.EnemyEntity);
        EntityManager.SetComponentData<LocalTransform>(gameDataComponent.EnemyEntity, new LocalTransform
        {
            Position = new float3(0, 0, -100),
            Rotation = lte.Rotation,
            Scale = lte.Scale
        });

        currentEnemyEntityCount = (int)gameDataComponent.SpawnCount.y * 10;
        for(int i=0; i< currentEnemyEntityCount; i++)
        {
            EntityManager.Instantiate(gameDataComponent.EnemyEntity);
        }

        // pre-instantiate all projectile entities and set to false 
        EntityManager.SetComponentEnabled<ProjectileTag>(gameDataComponent.ProjectileEntity, false);
        EntityManager.SetComponentEnabled<ToSpawnFlag>(gameDataComponent.ProjectileEntity, false);

        var lt = EntityManager.GetComponentData<LocalTransform>(gameDataComponent.ProjectileEntity);
        EntityManager.SetComponentData(gameDataComponent.ProjectileEntity, new LocalTransform
        {
            Position = new float3(0, 0, -100),
            Rotation = lt.Rotation,
            Scale = lt.Scale
        });

        var dist = math.length(cameraData.Bounds) + math.length(cameraData.BoundsPadding); 
        var timeToDestruction = dist / gameDataComponent.ProjectileSpeed;
        var maxProjectileWave = timeToDestruction / gameDataComponent.ProjectileShootCooldown; 

        currentProjectileEntityCount = (int)(gameDataComponent.PlayerNumberOfShots * maxProjectileWave * 2); 
        for (int i=0; i<currentProjectileEntityCount; i++)
        {
            EntityManager.Instantiate(gameDataComponent.ProjectileEntity);
        }        

        Debug.Log("start running");                
    }

    protected override void OnUpdate()
    {                
        CameraData cameraData = SystemAPI.GetComponent<CameraData>(GameDataEntity);
        GameDataComponent gameDataComponent = SystemAPI.GetComponent<GameDataComponent>(GameDataEntity);
        GameStateComponent gameStateComponent = SystemAPI.GetComponent<GameStateComponent>(GameDataEntity);
        SpawnerData spawnerData = SystemAPI.GetComponent<SpawnerData>(GameDataEntity);

        switch (gameStateComponent.CurrentState)
        {
            case 0:
            PlayerHealthData phd = SystemAPI.GetComponent<PlayerHealthData>(PlayerEntity);
            var pmd1 = SystemAPI.GetComponent<MovementData>(PlayerEntity);

            gameStateComponent.CurrentState = 0;
            gameStateComponent.CurrentWaveCount = 1;
            gameStateComponent.CurrentKills = 0;

            DynamicBuffer<CurveBufferData> cbd = SystemAPI.GetBuffer<CurveBufferData>(GameDataEntity);
            gameStateComponent.TargetKillCount = (int)(curveutility.evaluate((float)gameStateComponent.CurrentWaveCount/(float)gameDataComponent.TotalWaves, cbd) * gameDataComponent.KillsOnFinalWave);            
            gameStateComponent.LastWaveTimeEnded = SystemAPI.Time.ElapsedTime;

            phd.Value = gameDataComponent.PlayerStartHealth;            
            pmd1.AngularSpeed = 0;

            spawnerData.LastSpawnTime = SystemAPI.Time.ElapsedTime;

            SystemAPI.SetComponent(PlayerEntity, pmd1); 
            SystemAPI.SetComponent(GameDataEntity, gameStateComponent);
            SystemAPI.SetComponent(PlayerEntity, phd);
            SystemAPI.SetComponent(GameDataEntity, spawnerData);
            break;
            case 3:
            var pmd2 = SystemAPI.GetComponent<MovementData>(PlayerEntity);            
            pmd2.AngularSpeed = 0;
            SystemAPI.SetComponent(PlayerEntity, pmd2); 

            var dt = SystemAPI.Time.ElapsedTime - gameStateComponent.LastWaveTimeEnded;

            if (dt > gameDataComponent.WaitTimeBetweenWaves)
            {
                gameStateComponent.CurrentState = 1;
                spawnerData.LastSpawnTime = SystemAPI.Time.ElapsedTime;                
                SystemAPI.SetComponent(GameDataEntity, gameStateComponent);
                SystemAPI.SetComponent(GameDataEntity, spawnerData);
            }                        
            break;
            case 1:
            if (SystemAPI.Exists(PlayerEntity))
            {
                Vector2 moveInput = InputActions.Gameplay.Move.ReadValue<Vector2>();        
                RefRW<InputData> pmi = SystemAPI.GetComponentRW<InputData>(PlayerEntity);            
                pmi.ValueRW.PreviousDirection = pmi.ValueRW.Direction;
                pmi.ValueRW.Direction = moveInput;

                var s = pmi.ValueRO.InputState;
                if (s == 3) s = 0;            
                if (s == 1) s = 2;            
                pmi.ValueRW.InputState = s;
            }

            // update shot amount
            var dist = math.length(cameraData.Bounds) + math.length(cameraData.BoundsPadding); 
            var timeToDestruction = dist / gameDataComponent.ProjectileSpeed;
            var maxProjectileWave = timeToDestruction / gameDataComponent.ProjectileShootCooldown;                                         

            int newCount = (int)(gameDataComponent.PlayerNumberOfShots * maxProjectileWave * 2); 
            int diff = newCount - currentProjectileEntityCount;

            if (diff < 0)
            {
                int toDestroy = math.abs(diff);
                EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
                foreach(var (pTag, entity) in SystemAPI.Query<EnabledRefRW<ProjectileTag>>().WithEntityAccess().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
                {
                    if (pTag.ValueRO == false)
                    {
                        ecb.DestroyEntity(entity);
                        toDestroy--;
                    }
                    if (toDestroy == 0) break;
                }
                ecb.Playback(EntityManager);
                ecb.Dispose();
                currentProjectileEntityCount = newCount;            
            }
            else if (diff > 0)
            {
                for(int i=0; i<diff; i++)
                {
                    EntityManager.Instantiate(gameDataComponent.ProjectileEntity);                
                }
                currentProjectileEntityCount = newCount;            
            }

            // update enemy amount
            int newEnemyCount = (int)gameDataComponent.SpawnCount.y * 10;
            int enemyDiff = newEnemyCount - currentEnemyEntityCount;
            if (enemyDiff < 0)
            {
                int toDestroy = math.abs(enemyDiff);
                EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
                foreach(var (eTag, entity) in SystemAPI.Query<EnabledRefRW<EnemyTag>>().WithEntityAccess().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
                {
                    if (eTag.ValueRO == false)
                    {
                        ecb.DestroyEntity(entity);
                        toDestroy--;
                    }
                    if (toDestroy == 0) break;
                }
                ecb.Playback(EntityManager);
                ecb.Dispose();
                currentEnemyEntityCount = newEnemyCount;  
            }
            else if(enemyDiff > 0)
            {
                for(int i=0; i<enemyDiff; i++)
                {
                    EntityManager.Instantiate(gameDataComponent.EnemyEntity);                
                }
                currentEnemyEntityCount = newEnemyCount;
            }
            break;
        }
    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();

        InputActions.Disable();
        PlayerEntity = Entity.Null;        
        GameDataEntity = Entity.Null;
    }

    private void OnShootCancel(InputAction.CallbackContext context)
    {
       if (!SystemAPI.Exists(PlayerEntity)) return;

        RefRW<InputData> pmi = SystemAPI.GetComponentRW<InputData>(PlayerEntity);        
        pmi.ValueRW.InputState = 3;
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        if (!SystemAPI.Exists(PlayerEntity)) return;
        GameStateComponent gsc = SystemAPI.GetComponent<GameStateComponent>(GameDataEntity);
        if (gsc.CurrentState == 2 || gsc.CurrentState == 4)
        {
            gsc.CurrentState = 0;
            SystemAPI.SetComponent(GameDataEntity, gsc);
        }
        else if (gsc.CurrentState == 0)
        {
            gsc.CurrentState = 1;
            SystemAPI.SetComponent(GameDataEntity, gsc);
        }
        else
        {
            RefRW<InputData> pmi = SystemAPI.GetComponentRW<InputData>(PlayerEntity);                
            pmi.ValueRW.InputState = 1;        
        }    
    }
}
