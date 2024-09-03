using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(InitializationSystemGroup),OrderLast = true)]
public partial class PlayerInputSystem : SystemBase
{
    private GameInput InputActions;
    private Entity Player;
    private RefRW<ProjectileShooterData> PlayerProjectileTag;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<PlayerTag>();
        RequireForUpdate<PlayerMoveInput>();
        InputActions = new GameInput();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        InputActions.Enable();
        
        InputActions.Gameplay.Shoot.performed += OnShoot;
        
        Player = SystemAPI.GetSingletonEntity<PlayerTag>();
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        if (!SystemAPI.Exists(Player)) return;        
                
        // TODO check if this cachable?
        PlayerProjectileTag = SystemAPI.GetComponentRW<ProjectileShooterData>(Player);        
        PlayerProjectileTag.ValueRW.ShouldFire = true;
    }

    protected override void OnUpdate()
    {        
        Vector2 moveInput = InputActions.Gameplay.Move.ReadValue<Vector2>();        
        SystemAPI.SetSingleton(new PlayerMoveInput { Value = moveInput });            
    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();

        InputActions.Disable();
        Player = Entity.Null;        
    }
}
