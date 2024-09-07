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
    }
}

// [BurstCompile]
public partial struct PlayerMoveJob : IJobEntity
{
    public float DeltaTime;
    public GameDataComponent GameData;
    // [BurstCompile]
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
                {
                    // rotate clockwise
                    a.z -= DeltaTime * movementData.AngularSpeed;            
                }
                else
                {
                    a.z += DeltaTime * movementData.AngularSpeed;            
                }
            }
            movementData.AngularSpeed = math.lerp(movementData.AngularSpeed, 0, DeltaTime * 2.0f);            
            movementData.Direction = math.lerp(movementData.Direction, new float2(0,0), DeltaTime * 2.0f); 
        }
        else
        {
            // calculate rotation
            float iDotRight = math.dot(inputDirection, math.right().xy);
            if (math.abs(iDotRight) > 0.99f)
            {
                if (math.dot(inputDirection, math.right().xy) >= 0)
                {
                    // rotate clockwise
                    a.z -= DeltaTime * movementData.AngularSpeed;            
                }
                else
                {
                    a.z += DeltaTime * movementData.AngularSpeed;            
                }
                movementData.AngularSpeed = math.lerp(0, GameData.PlayerAngularSpeed, DeltaTime * 200.0f);
                movementData.Direction = math.lerp(movementData.Direction, inputDirection, DeltaTime * 2.0f);                             
            }
            else
            {
                float iDotUp = math.dot(inputDirection, math.up().xy);
                if (iDotUp <= 0) 
                {
                    movementData.Direction -= transform.Up().xy * 0.01f;
                }
                else
                {
                    movementData.Direction = transform.Up().xy;                                    
                }                
                
                movementData.AngularSpeed = 0;
                movementData.Direction = math.normalize(movementData.Direction);
            }            
        }        
                
        float2 velocity = movementData.Direction * movementData.Speed;
        transform.Position.xy += velocity * DeltaTime;
        transform.Rotation = quaternion.Euler(a);
        
        // add friction to move direction
    }
}

