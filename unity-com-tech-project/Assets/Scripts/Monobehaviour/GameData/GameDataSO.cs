using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "Data/GameData")]
public class GameDataSO : ScriptableObject
{
    [Header("Player settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector2 playerStartPosition;
    [SerializeField] private float playerMoveSpeed;
    [SerializeField] private float playerAngularSpeed = 10.0f;    

    [Header("Enemy settings")]
    [SerializeField] private Vector2 enemySpawnStartPosition;
    [SerializeField] private float enemySpawnRate;    

    [Header("Projectile settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed;
    [SerializeField] private double projectileShootCooldown = 1;

    [Header("Camera settings")]
    [SerializeField] private GameObject cameraPrefab;
    [SerializeField] private float cameraSpeed = 10.0f;
    [SerializeField] private Vector2 cameraBoundsPadding = new Vector2(50, 50);

    public GameObject PlayerPrefab { get { return playerPrefab; }} 
    public Vector2 PlayerStartPosition { get { return playerStartPosition; }} 
    public float PlayerMoveSpeed { get { return playerMoveSpeed; }} 
    public float PlayerAngularSpeed { get { return playerAngularSpeed; }} 
    
    public float EnemySpawnRate { get { return enemySpawnRate; }}     
    public Vector2 EnemySpawnStartPosition { get { return enemySpawnStartPosition; }}     

    public GameObject ProjectilePrefab { get { return projectilePrefab; }} 
    public float ProjectileSpeed { get { return projectileSpeed; }} 
    public double ProjectileShootCooldown { get { return projectileShootCooldown; }} 

    public GameObject CameraPrefab { get { return cameraPrefab; }} 
    public float CameraSpeed { get { return cameraSpeed; } }
    public Vector2 CameraBoundsPadding { get { return cameraBoundsPadding; } }
}
