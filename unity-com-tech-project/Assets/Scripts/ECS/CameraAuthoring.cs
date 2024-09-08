using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class CameraAuthoring : MonoBehaviour
{
    [SerializeField] private GameDataSO GameData;
    public class CameraAuthoringBaker : Baker<CameraAuthoring>
    {
        public override void Bake(CameraAuthoring authoring)
        {
            if (authoring.GameData == null) 
            {
                Debug.LogWarning($"{authoring.GetType().ToString()} Missing Game Data scriptable Object");
                return;
            }

            Entity entity = GetEntity(TransformUsageFlags.None);            
            AddComponent(entity, new CameraData {
                BoundsPadding = authoring.GameData.CameraBoundsPadding
            });

            Debug.Log("camera authoring baked");
        }
    }
}
