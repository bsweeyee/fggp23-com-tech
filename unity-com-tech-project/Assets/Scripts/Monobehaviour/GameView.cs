using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Entities;
using System.Linq.Expressions;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;

public class GameView : MonoBehaviour
{
    [SerializeField] private GameDataSO gameData;
    [SerializeField] private TextMeshProUGUI currentWaveText;
    [SerializeField] private TextMeshProUGUI killsToNextWave;

    private EntityManager em;
    private Entity gameEntity;
        
    Texture2D whiteTexture;
    Texture2D greyTexture;
    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;

        var gameStateQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GameStateComponent>());
        gameEntity = gameStateQuery.GetSingletonEntity();        
        
        whiteTexture = GetTexture(2, 2, Color.white);
        greyTexture = GetTexture(2, 2, Color.grey);

        GameDataComponent gameData = em.GetComponentData<GameDataComponent>(gameEntity);                
        tempShotCount = gameData.PlayerNumberOfShots;
        tempEnemyCount = (int)gameData.SpawnCount.y;        
    }
    void Update()
    {        
        GameStateComponent gameStateData = em.GetComponentData<GameStateComponent>(gameEntity);        
        int killsForCurrentWave = Mathf.FloorToInt(gameStateData.TargetKillCount);
        
        currentWaveText.text = $"{gameStateData.CurrentWaveCount.ToString()} / {gameData.TotalWaves}";
        killsToNextWave.text = $"{gameStateData.CurrentKills} / {killsForCurrentWave}";
    }    

    int tempShotCount;
    int tempEnemyCount;
    void OnGUI()
    {                                   
        GameDataComponent gameData = em.GetComponentData<GameDataComponent>(gameEntity);                
        bool isGameDataModified = false; 
               
        GUI.skin.horizontalSlider.normal.background = whiteTexture;
        GUI.skin.horizontalSliderThumb.normal.background = greyTexture;
        
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
