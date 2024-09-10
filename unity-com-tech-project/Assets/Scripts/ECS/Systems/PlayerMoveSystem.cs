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
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        GameDataComponent gc = SystemAPI.GetSingleton<GameDataComponent>();
        new PlayerMoveJob
        {
            DeltaTime = deltaTime,
            GameData = gc
        }.ScheduleParallel();

        new PlayerCalculateAABB
        {
        }.ScheduleParallel();
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
            // we want to reset Angular speed to 0 immediately if previous movement direction is a forward or backwards otherwise the unfinished AngularSpeeds will carry over and make movement feel weird
            if (math.abs(math.dot(inputData.PreviousDirection, math.up().xy)) >= 0.99f)
            {
                movementData.AngularSpeed = 0;
            }
            
            if (math.lengthsq(movementData.Direction) >= 0.01)
            {
                if (math.dot(movementData.Direction, math.right().xy) >= 0) 
                    a.z -= DeltaTime * movementData.AngularSpeed;                            
                else                
                    a.z += DeltaTime * movementData.AngularSpeed;                        
            }
            // TODO we want Angular speed to reduce by a better frictional based coefficient
            movementData.AngularSpeed = math.lerp(movementData.AngularSpeed, 0, DeltaTime * 5.0f);            
            
            // TODO we want to reduce direction by a better frictional based coefficient instead of DeltaTime maybe
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
                
                // TODO: we want Angular Speed to increase slowly at start and ramp up as player hold the button longer
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
                
                // TODO reduce Angular speed by some frictional based coefficient
                movementData.AngularSpeed = math.lerp(movementData.AngularSpeed, 0, DeltaTime * 5.0f);
                movementData.Direction = math.normalize(movementData.Direction);
            }            
        }        
                
        float2 velocity = movementData.Direction * movementData.Speed + movementData.ExternalVelocity;        
        transform.Position.xy += velocity * DeltaTime;
        transform.Rotation = quaternion.Euler(a);

        // TODO Reduce external velocity by some coefficient        
        movementData.ExternalVelocity = math.lerp(movementData.ExternalVelocity, new float2(0,0), DeltaTime);
    }
}

[BurstCompile]
public partial struct PlayerCalculateAABB : IJobEntity
{
    [BurstCompile]
    private void Execute(in PlayerTag playerTag, in LocalToWorld playerLTW, ref AABBData aabb)
    {                
        float nR = math.abs(math.dot(math.normalize(playerLTW.Up), math.normalize(math.right())));
        float newY = math.lerp(aabb.OriginalSize.y, aabb.OriginalSize.x, nR);                        
        float newX = math.lerp(aabb.OriginalSize.x, aabb.OriginalSize.y, nR);

        aabb.Min = new float2(-newX/2, -newY/2);
        aabb.Max = new float2(newX/2, newY/2);
    }
}