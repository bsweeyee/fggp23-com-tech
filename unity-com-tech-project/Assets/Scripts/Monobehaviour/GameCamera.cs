using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor.Rendering;

public class GameCamera : MonoBehaviour
{
    [SerializeField] private GameDataSO gameData;

    private EntityManager em;
    private Entity pe;
    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerTag>());
        pe = query.GetSingletonEntity();
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
    }
}
