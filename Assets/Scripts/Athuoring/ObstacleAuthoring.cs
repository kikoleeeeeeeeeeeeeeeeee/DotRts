using UnityEngine;
using Unity.Entities;

public class ObstacleAuthoring : MonoBehaviour
{
    class Baker : Baker<ObstacleAuthoring>
    {
        public override void Bake(ObstacleAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ObstacleTag>(entity);
        }
    }
}