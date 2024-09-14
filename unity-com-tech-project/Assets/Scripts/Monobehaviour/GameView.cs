using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Entities;
using System.Linq.Expressions;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine.UI;
using Unity.Mathematics;

public class GameView : MonoBehaviour
{
    [SerializeField] private GameObject startUI;
    [SerializeField] private GameObject playUI;
    [SerializeField] private GameObject nextWaveUI;
    [SerializeField] private GameObject gameOverUI; 

    [SerializeField] private GameDataSO gameData;
    [SerializeField] private TextMeshProUGUI currentWaveText;
    [SerializeField] private TextMeshProUGUI killsToNextWave;
    [SerializeField] private Image hpRenderer;

    [SerializeField] private TextMeshProUGUI waveCompleteText;    
    [SerializeField] private TextMeshProUGUI gameOverText;    

    private EntityManager em;
    private Entity gameEntity;
    private Entity playerEntity;
        
    Texture2D whiteTexture;
    Texture2D greyTexture;
    Material hp;
    private static readonly int T = Shader.PropertyToID("_T");
    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;

        var gameStateQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GameStateComponent>());
        gameEntity = gameStateQuery.GetSingletonEntity();        
                
        var playerQuery = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerHealthData>());
        playerEntity = playerQuery.GetSingletonEntity();

        whiteTexture = GetTexture(2, 2, Color.white);
        greyTexture = GetTexture(2, 2, Color.grey);

        GameDataComponent gameData = em.GetComponentData<GameDataComponent>(gameEntity);                
        tempShotCount = gameData.PlayerNumberOfShots;
        tempEnemyCount = (int)gameData.SpawnCount.y;        
        hp = hpRenderer.material;        
    }

    void Update()
    {        
        GameStateComponent gameStateData = em.GetComponentData<GameStateComponent>(gameEntity);        
        playUI.SetActive(gameStateData.CurrentState == 1);
        startUI.SetActive(gameStateData.CurrentState == 0);
        nextWaveUI.SetActive(gameStateData.CurrentState == 3);
        gameOverUI.SetActive(gameStateData.CurrentState == 4 || gameStateData.CurrentState == 2);        
        
        switch (gameStateData.CurrentState)
        {
            case 4:
            gameOverText.text = "You win!";
            break;
            case 2:
            gameOverText.text = "Game Over!";
            break;
            case 3:
            GameDataComponent gameData1 = em.GetComponentData<GameDataComponent>(gameEntity);                
            
            var dt = Time.time - gameStateData.LastWaveTimeEnded;
            if (dt < gameData1.WaitTimeBetweenWaves / 2)
            {
                waveCompleteText.text = "Wave Complete!";
            }
            else
            {
                double ddt = gameData1.WaitTimeBetweenWaves - dt;                
                waveCompleteText.text = $"Next wave in {(int)math.floor(ddt) + 1}...";
            }            
            break;
            case 1:
            GameDataComponent gameData2 = em.GetComponentData<GameDataComponent>(gameEntity);                            
            int killsForCurrentWave = (int)math.floor(gameStateData.TargetKillCount);
            
            PlayerHealthData healthData = em.GetComponentData<PlayerHealthData>(playerEntity);

            currentWaveText.text = $"{gameStateData.CurrentWaveCount.ToString()} / {gameData2.TotalWaves}";
            killsToNextWave.text = $"{gameStateData.CurrentKills} / {killsForCurrentWave}";
            hp.SetFloat(T, (float)healthData.Value / (float)gameData2.PlayerStartHealth);
            break;        
        }        
    }    

    int tempShotCount;
    int tempEnemyCount;
    bool displayDebug = false;
    void OnGUI()
    {
        if (!displayDebug)
        {
            if (GUILayout.Button("Show Debug"))
            {
                displayDebug = true;
            }
        }
        else
        {
            GameDataComponent gameData = em.GetComponentData<GameDataComponent>(gameEntity);                
            GameStateComponent gameStateData = em.GetComponentData<GameStateComponent>(gameEntity);
            
            bool isGameDataModified = false; 
                
            GUI.skin.horizontalSlider.normal.background = whiteTexture;
            GUI.skin.horizontalSliderThumb.normal.background = greyTexture;
            
            GUILayout.Label($"Current State: {gameStateData.CurrentState}");

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Number Of Shots: ");
                tempShotCount = (int)GUILayout.HorizontalSlider(tempShotCount, 1, 1000, GUILayout.MinWidth(100));            
                int sc = int.Parse(GUILayout.TextField(tempShotCount.ToString()));
                if (sc != tempShotCount) tempShotCount = sc;

                if (GUILayout.Button("Update"))
                {
                    gameData.PlayerNumberOfShots = tempShotCount;
                    isGameDataModified = true;
                }            
                // GUILayout.Label($"{gameData.PlayerNumberOfShots}");            
            }
            
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Max spawn count: ");
                tempEnemyCount = (int)GUILayout.HorizontalSlider(tempEnemyCount, 1, 1000, GUILayout.MinWidth(100));
                int ec = int.Parse(GUILayout.TextField(tempEnemyCount.ToString()));
                if (ec != tempEnemyCount) tempEnemyCount = ec;

                if (GUILayout.Button("Update"))
                {
                    gameData.SpawnCount.y = tempEnemyCount;
                    isGameDataModified = true;
                }                                
                // GUILayout.Label($"{gameData.SpawnCount.y}");            
            }            

            if (isGameDataModified)
            {
                em.SetComponentData(gameEntity, gameData);
            }

            if (GUILayout.Button("Close Debug"))
            {
                displayDebug = false;
            }
        }
    }

    public static Texture2D GetTexture(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }            
}
