using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

[UpdateInGroup(typeof(InitializationSystemGroup),OrderLast = true)]
public partial class GameSystem : SystemBase
{
    private GameInput InputActions;
    private Entity PlayerEntity;
    private Entity GameDataEntity;    

    protected override void OnCreate()
    {
        base.OnCreate();        
        
        InputActions = new GameInput();
        Debug.Log("created");        
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();            
        InputActions.Enable();        
        InputActions.Gameplay.Shoot.performed += OnShoot;
        InputActions.Gameplay.Shoot.canceled += OnShootCancel;                

        GameDataComponent gdc;
        foreach (var (gd, entity) in SystemAPI.Query<GameDataComponent>().WithEntityAccess())
        {
            GameDataEntity = entity;
            gdc = gd;

            var playerEntity = EntityManager.Instantiate(gd.PlayerEntity);            
            var playerTransform = SystemAPI.GetComponentRW<LocalTransform>(playerEntity);
            playerTransform.ValueRW.Position.xy = gd.PlayerStartPosition;

            PlayerEntity = playerEntity;            
            SystemAPI.SetSingleton(gd);
            
            var cameraEntity = EntityManager.Instantiate(gd.CameraEntity);
            var ce = SystemAPI.GetComponent<CameraData>(cameraEntity);                               
            SystemAPI.SetSingleton(ce);
        }
        
        Debug.Log("start running");                
    }

    protected override void OnUpdate()
    {                
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
                    
            // DynamicBuffer<CurveBufferData> cbd = SystemAPI.GetBuffer<CurveBufferData>(GameDataEntity);
            // curveutility.evaluate(0.3333f, cbd);
        }
        
        // TODO parallelize spawn job?
        var gameData = SystemAPI.GetComponent<GameDataComponent>(GameDataEntity);        
        var spawnerData = SystemAPI.GetComponentRW<SpawnerData>(GameDataEntity);
        var gameState = SystemAPI.GetComponent<GameStateComponent>(GameDataEntity);        
        var diffcultyCurve = SystemAPI.GetBuffer<CurveBufferData>(GameDataEntity);

        var cameraEntity = SystemAPI.GetSingletonEntity<CameraData>();
        var cameraData = SystemAPI.GetComponent<CameraData>(cameraEntity);

        double t = SystemAPI.Time.ElapsedTime - spawnerData.ValueRO.LastSpawnTime;
        if (t > gameData.SpawnEnemyRate)
        {
            float wt = (float)gameState.CurrentWaveCount / (float)gameData.TotalWaves;
            float n = gameData.SpawnCount.x + gameData.SpawnCount.y * curveutility.evaluate(wt, diffcultyCurve);            
            Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)gameState.SystemCurrentTime);            
            
            for (int i=0; i<n; i++)
            {
                var enemyEntity = EntityManager.Instantiate(gameData.EnemyEntity);                        
            
                float rng1 = (random.NextFloat() * 2) - 1; // gives a value between -1 to 1 
                float rng2 = (random.NextFloat() * 2) - 1; // gives a value between -1 to 1                
                float rng3 = (random.NextFloat() * 2) - 1; // gives a value between -1 to 1                
                
                int s1 = (int)math.sign(rng1); 
                int s2 = (int)math.sign(rng2);                
                float2 offset = new float2(0, 0);
                
                if (s2 > 0)
                {
                    offset = new float2(s1 * cameraData.Bounds.x/2 + rng1 * cameraData.BoundsPadding.x, rng3 * cameraData.Bounds.y/2);                    
                }
                else
                {
                    offset = new float2(rng3 * cameraData.Bounds.x/2, s1 * cameraData.Bounds.y/2 + rng1 * cameraData.BoundsPadding.y);
                }
                                
                // float2 randomPosition = cameraData.Position + nDir * math.length(cameraData.Bounds);
                float2 randomPosition = cameraData.Position + offset;            
                var lt = SystemAPI.GetComponent<LocalTransform>(enemyEntity);        
                SystemAPI.SetComponent(enemyEntity, new LocalTransform {
                    Position = new float3(randomPosition.xy, 0),
                    Rotation = lt.Rotation,
                    Scale = lt.Scale
                });                     
            }
            spawnerData.ValueRW.LastSpawnTime = SystemAPI.Time.ElapsedTime;                                
            // Debug.Log($"{n}, {gameState.CurrentKills}, {gameState.CurrentWaveCount}");                            
        }
    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();

        InputActions.Disable();
        PlayerEntity = Entity.Null;        
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

        RefRW<InputData> pmi = SystemAPI.GetComponentRW<InputData>(PlayerEntity);                
        pmi.ValueRW.InputState = 1;        
    }
}

public partial struct SpawnJob : IJobEntity
{
    public GameDataComponent GameData;
    public CameraData CameraData;
    public double ElapsedTime;
    public EntityCommandBuffer ECB;
    public void Execute()
    {
        // spawn enemy entities here
        // var enemyEntity = ECB.Instantiate(GameData.EnemyEntity);                        
        
        // Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)((ElapsedTime+1) * 10));            
        // float rng1 = random.NextFloat() * 2 - 1; // gives a value between -1 to 1 
        // float rng2 = random.NextFloat() * 2 - 1; // gives a value between -1 to 1             
        // int rngSign1 = (rng1 < 0) ? -1 : 1;
        // int rngSign2 = (rng2 < 0) ? -1 : 1;

        // float2 randomPosition = CameraData.Position + new float2(rngSign1 * CameraData.Bounds.x, rngSign2 * CameraData.Bounds.y) + new float2(rng1 * CameraData.BoundsPadding.x, rng2 * CameraData.BoundsPadding.x);                    

        // // float2 position = 
        // ECB.SetComponent(enemyEntity, new LocalTransform{
        //     Position = new float3(randomPosition.xy, 0),
        // });
    }
}
