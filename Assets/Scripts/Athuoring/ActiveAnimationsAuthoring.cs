using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public class ActiveAnimationsAuthoring : MonoBehaviour
{

    public AnimationDataSo.AnimationType nextAnimationType;

    public class Baker : Baker<ActiveAnimationsAuthoring>
    {
        public override void Bake(ActiveAnimationsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new ActiveAnimation
            {
              nextAnimationType=authoring.nextAnimationType,
            });

        }
    }
}
public struct ActiveAnimation : IComponentData
{
    public int frame;
    public float frameTimer;
    public AnimationDataSo.AnimationType activeAnimationType;
    public AnimationDataSo.AnimationType nextAnimationType;
}