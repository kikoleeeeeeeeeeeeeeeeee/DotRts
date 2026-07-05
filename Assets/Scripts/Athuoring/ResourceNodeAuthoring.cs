using UnityEngine;
using Unity.Entities;

public class ResourceNodeAuthoring : MonoBehaviour
{
    class Baker : Baker<ResourceNodeAuthoring>
    {
        public override void Bake(ResourceNodeAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ResourceNodeTag>(entity);
        }
    }
}