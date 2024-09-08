using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor.Rendering;
using UnityEngine.U2D.IK;

public class GameCamera : MonoBehaviour
{
    [SerializeField] private GameDataSO gameData;

    private EntityManager em;
    private Entity pe;
    private Entity cd;
    private Camera cam;
    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var playerQuery = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerTag>());        
        pe = playerQuery.GetSingletonEntity();

        var cameraQuery = em.CreateEntityQuery(ComponentType.ReadOnly<CameraData>());
        cd = cameraQuery.GetSingletonEntity();        

        cam = GetComponent<Camera>();
    }    

    void Update()
    {
        if (pe == Entity.Null) return;

        LocalTransform localTransform = em.GetComponentData<LocalTransform>(pe);
        
        Vector2 camPosition = transform.position;
        Vector2 direction = localTransform.Position.xy - new float2(camPosition.x, camPosition.y);
        var length = direction.magnitude;        

        transform.position += Vector3.Lerp(Vector3.zero, direction * gameData.CameraSpeed * Time.deltaTime, Mathf.InverseLerp(0, 2.0f, length));
        // Debug.Log($"player transform: {localTransform.Position}");

        CameraData camData = em.GetComponentData<CameraData>(cd);
        camData.Position = new float2(transform.position.x, transform.position.y);
        
        float aspect = (float)Screen.width / (float) Screen.height;
        float worldHeight = cam.orthographicSize * 2;
        float worldWidth = worldHeight * aspect;
        camData.Bounds = new float2(worldWidth, worldHeight);

        em.SetComponentData(cd, camData);
    }    
}
