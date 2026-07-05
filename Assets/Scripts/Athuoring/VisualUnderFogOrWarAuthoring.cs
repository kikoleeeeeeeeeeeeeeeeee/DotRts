using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Rendering;

public class VisualUnderFogOrWarAuthoring : MonoBehaviour
{


    public float sphereCastSize;
    public GameObject parentGameObject; 
    public class Baker : Baker<VisualUnderFogOrWarAuthoring>
    {
        public override void Bake(VisualUnderFogOrWarAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new VisualUnderFogOfWar
            {
                isVisible = false,
                parentEntity = GetEntity(authoring.parentGameObject, TransformUsageFlags.Dynamic),
                sphereCastSize = authoring.sphereCastSize,
                timer = 0f,
                timerMax = .2f,
            });
            // DisableRendering 已移除：战争迷雾系统(VisualUnderFogOrWarSystems)当前已禁用(return;跳过更新)，
            // 添加 DisableRendering 会导致实体永远不可见。需要时由运行时系统自行管理。
        }
    }
}

public struct VisualUnderFogOfWar : IComponentData
{
    public bool isVisible;
    public Entity parentEntity;
    public float sphereCastSize;
    public float timer;
    public float timerMax;
}
