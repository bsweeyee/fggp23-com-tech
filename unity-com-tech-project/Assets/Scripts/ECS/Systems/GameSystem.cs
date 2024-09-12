using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(InitializationSystemGroup),OrderLast = true)]
public partial class GameSystem : SystemBase
{
    private GameInput InputActions;
    private Entity PlayerEntity;
    private Entity GameDataEntity;
    private GameDataComponent GameDataComponent;    

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
        
        foreach (var (gd, entity) in SystemAPI.Query<GameDataComponent>().WithEntityAccess())
        {
            GameDataEntity = entity;
            GameDataComponent = gd;
        }

        var playerEntity = EntityManager.Instantiate(GameDataComponent.PlayerEntity);            
        var playerTransform = SystemAPI.GetComponentRW<LocalTransform>(playerEntity);
        playerTransform.ValueRW.Position.xy = GameDataComponent.PlayerStartPosition;
        PlayerEntity = playerEntity;            
                    
        var ce = SystemAPI.GetComponent<CameraData>(GameDataEntity);                               
        
        SystemAPI.SetSingleton(GameDataComponent);
        SystemAPI.SetSingleton(ce);
        
        // pre-instantiate enemy entities and set enabled component to false
        EntityManager.SetComponentEnabled<EnemyTag>(GameDataComponent.EnemyEntity, false);        
        EntityManager.SetComponentEnabled<ToSpawnFlag>(GameDataComponent.EnemyEntity, false);
        
        var lte = EntityManager.GetComponentData<LocalTransform>(GameDataComponent.EnemyEntity);
        EntityManager.SetComponentData<LocalTransform>(GameDataComponent.EnemyEntity, new LocalTransform
        {
            Position = new float3(0, 0, -100),
            Rotation = lte.Rotation,
            Scale = lte.Scale
        });
        for(int i=0; i< GameDataComponent.SpawnCount.y * 10; i++)
        {
            EntityManager.Instantiate(GameDataComponent.EnemyEntity);
        }

        // pre-instantiate all projectile entities and set to false 
        EntityManager.SetComponentEnabled<ProjectileTag>(GameDataComponent.ProjectileEntity, false);
        EntityManager.SetComponentEnabled<ToSpawnFlag>(GameDataComponent.ProjectileEntity, false);
        
        var lt = EntityManager.GetComponentData<LocalTransform>(GameDataComponent.ProjectileEntity);
        EntityManager.SetComponentData<LocalTransform>(GameDataComponent.ProjectileEntity, new LocalTransform
        {
            Position = new float3(0, 0, -100),
            Rotation = lt.Rotation,
            Scale = lt.Scale
        });

        var dist = math.length(ce.Bounds) + math.length(ce.BoundsPadding); 
        var timeToDestruction = dist / GameDataComponent.ProjectileSpeed;
        var maxProjectileWave = timeToDestruction / GameDataComponent.ProjectileShootCooldown; 
        for (int i=0; i<GameDataComponent.PlayerNumberOfShots * maxProjectileWave * 2; i++)
        {
            EntityManager.Instantiate(GameDataComponent.ProjectileEntity);
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
