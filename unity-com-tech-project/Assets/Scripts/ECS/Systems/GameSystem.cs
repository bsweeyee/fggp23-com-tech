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
    private Entity Player;    

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

        foreach (var gd in SystemAPI.Query<GameDataComponent>())
        {
            gdc = gd;

            var playerEntity = EntityManager.Instantiate(gd.PlayerEntity);            
            var playerTransform = SystemAPI.GetComponentRW<LocalTransform>(playerEntity);
            playerTransform.ValueRW.Position.xy = gd.PlayerStartPosition;

            Player = playerEntity;            
            SystemAPI.SetSingleton(gd);
            
            var cameraEntity = EntityManager.Instantiate(gd.CameraEntity);
            var ce = SystemAPI.GetComponent<CameraData>(cameraEntity);                               
            SystemAPI.SetSingleton(ce);
        }

        
        Debug.Log("start running");                
    }

    protected override void OnUpdate()
    {                
        if (SystemAPI.Exists(Player))
        {
            Vector2 moveInput = InputActions.Gameplay.Move.ReadValue<Vector2>();        
            RefRW<InputData> pmi = SystemAPI.GetComponentRW<InputData>(Player);            
            pmi.ValueRW.Direction = moveInput;
            
            var s = pmi.ValueRO.InputState;
            if (s == 3)
            {
                s = 0;
            }
            if (s == 1)
            {
                s = 2;
            }
            pmi.ValueRW.InputState = s;                        
        }
        
        // TODO maybe use a dedicate spawn job?
        var gdEntity = SystemAPI.GetSingletonEntity<GameDataComponent>();
        var gameData = SystemAPI.GetComponent<GameDataComponent>(gdEntity);        
        var spawnerData = SystemAPI.GetComponentRW<SpawnerData>(gdEntity);

        var cameraEntity = SystemAPI.GetSingletonEntity<CameraData>();
        var cameraData = SystemAPI.GetComponent<CameraData>(cameraEntity);

        double t = SystemAPI.Time.ElapsedTime - spawnerData.ValueRO.LastSpawnTime;
        // Debug.Log($"{t}, {SystemAPI.Time.ElapsedTime}, {spawnerData.ValueRO.LastSpawnTime}");                 
        if (t > gameData.SpawnEnemyRate)
        {            
            var enemyEntity = EntityManager.Instantiate(gameData.EnemyEntity);                        
        
            Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)((SystemAPI.Time.ElapsedTime+1) * 10));            
            random.NextFloat();
            float rng1 = random.NextFloat() * 2 - 1; // gives a value between -1 to 1 
            float rng2 = random.NextFloat() * 2 - 1; // gives a value between -1 to 1

            float2 direction = new float2(rng1, rng2);
            float2 nDir = math.normalize(direction);

            float2 randomPosition = cameraData.Position + nDir * math.length(cameraData.Bounds);
            var lt = SystemAPI.GetComponent<LocalTransform>(enemyEntity);        
            SystemAPI.SetComponent(enemyEntity, new LocalTransform {
                Position = new float3(randomPosition.xy, 0),
                Rotation = lt.Rotation,
                Scale = lt.Scale
        });         
            
            spawnerData.ValueRW.LastSpawnTime = SystemAPI.Time.ElapsedTime;                                
        }        
    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();

        InputActions.Disable();
        Player = Entity.Null;        
    }

    private void OnShootCancel(InputAction.CallbackContext context)
    {
       if (!SystemAPI.Exists(Player)) return;

        RefRW<InputData> pmi = SystemAPI.GetComponentRW<InputData>(Player);        
        pmi.ValueRW.InputState = 3;
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        if (!SystemAPI.Exists(Player)) return;

        RefRW<InputData> pmi = SystemAPI.GetComponentRW<InputData>(Player);                
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
