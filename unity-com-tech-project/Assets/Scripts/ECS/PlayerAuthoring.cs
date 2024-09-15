using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    [SerializeField] public GameDataSO GameData;
    public class PlayerAuthoringBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            if (authoring.GameData == null) 
            {
                Debug.LogWarning($"{authoring.GetType().ToString()} Missing Game Data scriptable Object");
                return;
            }

            SpriteRenderer sr = authoring.GameData.PlayerPrefab.GetComponent<SpriteRenderer>();
            Vector3 srScale = authoring.GameData.PlayerPrefab.transform.lossyScale;
            Vector3 size = sr.bounds.size; 

            Entity playerEntity = GetEntity(TransformUsageFlags.Dynamic);            
                        
            AddComponent<PlayerTag>(playerEntity);            
            AddComponent<InputData>(playerEntity);
            AddComponent(playerEntity, new PlayerHealthData {
                Value = authoring.GameData.PlayerHP,
            });           
            AddComponent(playerEntity, new MovementData {
                Speed = authoring.GameData.PlayerMoveSpeed,
                AngularSpeed = 0
            });
            AddComponent(playerEntity, new ProjectileShooterData {
                LastFireTime = 0,
            });
            AddComponent(playerEntity, new AABBData {
                Min = new float2(-size.x/2 * srScale.x/2, -size.y/2 * srScale.y/2),
                Max = new float2(size.x/2 * srScale.x/2, size.y/2 * srScale.y/2),
                OriginalSize = new float2(size.x * srScale.x/2, size.y * srScale.x/2),
            });
            
            Debug.Log("player authoring baked");
        }
    }
}