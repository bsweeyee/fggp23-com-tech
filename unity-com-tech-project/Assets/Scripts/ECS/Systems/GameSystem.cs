using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Scenes;
using Unity.Transforms;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

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

        GameDataComponent gdc;

        foreach (var gd in SystemAPI.Query<GameDataComponent>())
        {
            gdc = gd;

            var playerEntity = EntityManager.Instantiate(gd.PlayerEntity);            
            var playerTransform = SystemAPI.GetComponentRW<LocalTransform>(playerEntity);
            playerTransform.ValueRW.Position.xy = gd.PlayerStartPosition;

            Player = playerEntity;            
            SystemAPI.SetSingleton(gd);
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
        }
    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();

        InputActions.Disable();
        Player = Entity.Null;        
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        if (!SystemAPI.Exists(Player)) return;        
                
        // TODO check if this cachable?
        var playerProjectileTag = SystemAPI.GetComponentRW<ProjectileShooterData>(Player);        
        playerProjectileTag.ValueRW.ShouldFire = true;
    }
}
