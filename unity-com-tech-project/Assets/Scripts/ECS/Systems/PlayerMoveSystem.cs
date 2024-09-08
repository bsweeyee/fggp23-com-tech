using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor.Rendering;
using Unity.Burst;
using Unity.Mathematics;
using NUnit.Framework.Constraints;
using Unity.Entities.UniversalDelegates;
using UnityEditor.MPE;
using UnityEngine.EventSystems;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct PlayerMoveSystem : ISystem
{
    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        GameDataComponent gc = SystemAPI.GetSingleton<GameDataComponent>();
        new PlayerMoveJob
        {
            DeltaTime = deltaTime,
            GameData = gc
        }.Schedule();

        new PlayerCalculateAABB
        {
        }.Schedule();
    }
}

[BurstCompile]
public partial struct PlayerMoveJob : IJobEntity
{
    public float DeltaTime;
    public GameDataComponent GameData;
    [BurstCompile]
    private void Execute( ref LocalTransform transform, in PlayerTag pt, in InputData inputData, ref MovementData movementData )    
    {
        float2 inputDirection = inputData.Direction;
        float3 a = math.Euler(transform.Rotation);        
        float l = math.lengthsq(inputDirection);
        if (l <= 0.01f) 
        {            
            float ls = math.lerp(movementData.AngularSpeed, 0, DeltaTime);
            
            if (math.lengthsq(movementData.Direction) >= 0.01)
            {
                if (math.dot(movementData.Direction, math.right().xy) >= 0) 
                    a.z -= DeltaTime * movementData.AngularSpeed;                            
                else                
                    a.z += DeltaTime * movementData.AngularSpeed;                        
            }
            movementData.AngularSpeed = math.lerp(movementData.AngularSpeed, 0, DeltaTime * 2.0f);            
            movementData.Direction = math.lerp(movementData.Direction, new float2(0,0), DeltaTime * 2.0f); 
        }
        else
        {
            // calculate rotation
            inputDirection = math.normalize(inputDirection);
            float iDotRight = math.dot(inputDirection, math.right().xy);
            if (math.abs(iDotRight) > 0.1f)
            {
                if (math.dot(inputDirection, math.right().xy) >= 0)                                
                    a.z -= DeltaTime * movementData.AngularSpeed;                            
                else                
                    a.z += DeltaTime * movementData.AngularSpeed;                            
                movementData.AngularSpeed = math.lerp(0, GameData.PlayerAngularSpeed, DeltaTime * 100.0f);
                movementData.Direction = math.lerp(movementData.Direction, inputDirection, DeltaTime * 2.0f);                             
            }
            else
            {
                float iDotUp = math.dot(inputDirection, math.up().xy);
                if (iDotUp <= 0) 
                    movementData.Direction = -transform.Up().xy;                
                else                
                    movementData.Direction = transform.Up().xy;                             
                
                movementData.AngularSpeed = 0;
                movementData.Direction = math.normalize(movementData.Direction);
            }            
        }        
                
        float2 velocity = movementData.Direction * movementData.Speed + movementData.ExternalVelocity;        
        transform.Position.xy += velocity * DeltaTime;
        transform.Rotation = quaternion.Euler(a);        
        movementData.ExternalVelocity = math.lerp(movementData.ExternalVelocity, new float2(0,0), DeltaTime);
    }
}

public partial struct PlayerCalculateAABB : IJobEntity
{
    private void Execute(in PlayerTag playerTag, in LocalToWorld playerLTW, ref AABBData aabb)
    {                
        float nR = math.abs(math.dot(math.normalize(playerLTW.Up), math.normalize(math.right())));
        float newY = math.lerp(aabb.OriginalSize.y, aabb.OriginalSize.x, nR);                        
        float newX = math.lerp(aabb.OriginalSize.x, aabb.OriginalSize.y, nR);

        aabb.Min = new float2(-newX/2, -newY/2);
        aabb.Max = new float2(newX/2, newY/2);
    }
}