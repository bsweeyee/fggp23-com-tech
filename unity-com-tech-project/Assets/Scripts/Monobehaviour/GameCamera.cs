using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using System;

public class GameCamera : MonoBehaviour
{
    [SerializeField] private GameDataSO gameData;

    private EntityManager em;
    private Entity pe;
    private Entity cd;
    private Entity gs;

    private bool gameEntityInitialized;
    private bool camearaEntityInitialized;
    private bool playerEntityInitialized;

    private Camera cam;
    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;       
        cam = GetComponent<Camera>();                    
    }    

    void Update()
    {
        if (pe == Entity.Null) 
        {
            var playerQuery = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerTag>());        
            if (playerQuery.HasSingleton<PlayerTag>())
            {
                pe = playerQuery.GetSingletonEntity();    
            }
            return;
        }
        if (cd == Entity.Null) 
        {
            var cameraQuery = em.CreateEntityQuery(ComponentType.ReadOnly<CameraData>());
            if (cameraQuery.HasSingleton<CameraData>())
            {
                cd = cameraQuery.GetSingletonEntity();
            }
            return;
        }
        if (gs == Entity.Null) 
        {
            var gameStateQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GameStateComponent>());
            if (gameStateQuery.HasSingleton<GameStateComponent>())
            {
                gs = gameStateQuery.GetSingletonEntity();
            }
            return;
        }

        if (!playerEntityInitialized)
        {
           playerEntityInitialized = true;
        }

        if (!gameEntityInitialized)
        {           
            GameStateComponent gameStateData = em.GetComponentData<GameStateComponent>(gs);
            gameStateData.SystemTimeWhenGameStarted = DateTime.Now.Ticks;
            em.SetComponentData(gs, gameStateData);

            gameEntityInitialized = true;
        }
        if (!camearaEntityInitialized)
        {            
            CameraData cData = em.GetComponentData<CameraData>(cd);
            cData.Position = new float2(transform.position.x, transform.position.y);

             float aspect = (float)Screen.width / (float) Screen.height;
            float worldHeight = cam.orthographicSize * 2;
            float worldWidth = worldHeight * aspect;
            cData.Bounds = new float2(worldWidth, worldHeight);

            em.SetComponentData(cd, cData);

            camearaEntityInitialized = true;
        }

        LocalTransform localTransform = em.GetComponentData<LocalTransform>(pe);
        
        Vector2 camPosition = transform.position;
        Vector2 direction = localTransform.Position.xy - new float2(camPosition.x, camPosition.y);
        var length = direction.magnitude;        

        transform.position += Vector3.Lerp(Vector3.zero, direction * gameData.CameraSpeed * Time.deltaTime, Mathf.InverseLerp(0, 2.0f, length));
        // Debug.Log($"player transform: {localTransform.Position}"); 
        
        CameraData camData = em.GetComponentData<CameraData>(cd);
        var position = new float2(transform.position.x, transform.position.y);
        em.SetComponentData(cd, new CameraData
        {
            Position = position,
            Bounds = camData.Bounds,
            BoundsPadding = camData.BoundsPadding                            
        });            
    }    
}
