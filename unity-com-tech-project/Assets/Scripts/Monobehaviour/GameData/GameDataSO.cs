using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "Data/GameData")]
public class GameDataSO : ScriptableObject
{
    [Header("Game setting")]
    [SerializeField] private AnimationCurve diffultyCurve = AnimationCurve.Constant(0, 1, 1);
    [SerializeField] private Vector2 spawnCount = new Vector2(100, 1000); // x == min, y == max
    [SerializeField] private int killsOnFinalWave = 2000;
    [SerializeField] private int totalWaves = 5;

    [Header("Player settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector2 playerStartPosition;
    [SerializeField] private float playerMoveSpeed;
    [SerializeField] private float playerAngularSpeed = 10.0f;    
    [Range(1, 1000)][SerializeField] private int initialNumOfShots = 3;    

    [Header("Enemy settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float enemySpawnRate;    
    [SerializeField] private float enemySpeed;

    [Header("Projectile settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed;
    [SerializeField] private double projectileShootCooldown = 1;

    [Header("Camera settings")]
    [SerializeField] private float cameraSpeed = 10.0f;
    [SerializeField] private Vector2 cameraBoundsPadding = new Vector2(50, 50);

    public AnimationCurve DifficultyCurve { get { return diffultyCurve; } }
    public Vector2 SpawnCount { get { return spawnCount; } } // x == min, y == max
    public int KillsOnFinalWave { get { return killsOnFinalWave; } }
    public int TotalWaves { get { return totalWaves; } }
    
    public GameObject PlayerPrefab { get { return playerPrefab; }} 
    public Vector2 PlayerStartPosition { get { return playerStartPosition; }} 
    public float PlayerMoveSpeed { get { return playerMoveSpeed; }} 
    public float PlayerAngularSpeed { get { return playerAngularSpeed; }} 
    public int InitialNumberOfShots { get { return initialNumOfShots; }} 
    
    public GameObject EnemyPrefab { get { return enemyPrefab; }} 
    public float EnemySpawnRate { get { return enemySpawnRate; }}     
    public float EnemyMoveSpeed { get { return enemySpeed; }}     

    public GameObject ProjectilePrefab { get { return projectilePrefab; }} 
    public float ProjectileSpeed { get { return projectileSpeed; }} 
    public double ProjectileShootCooldown { get { return projectileShootCooldown; }} 

    public float CameraSpeed { get { return cameraSpeed; } }
    public Vector2 CameraBoundsPadding { get { return cameraBoundsPadding; } }
}
