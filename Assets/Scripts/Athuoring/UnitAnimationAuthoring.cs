using Unity.Entities;
using UnityEngine;

public class UnitAnimationAuthoring : MonoBehaviour
{
    public AnimationDataSo.AnimationType idleAnimationType;
    public AnimationDataSo.AnimationType walkAnimationType;
    public AnimationDataSo.AnimationType shootAnimationType;
    public AnimationDataSo.AnimationType aimAnimationType;
    public AnimationDataSo.AnimationType meleeAttackAnimationType;

    public class Baker : Baker<UnitAnimationAuthoring>
    {
        public override void Bake(UnitAnimationAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitAnimation { 
                idleAnimationType = authoring.idleAnimationType,
                walkAnimationType = authoring.walkAnimationType, 
                shootAnimationType = authoring.shootAnimationType,
                aimAnimationType = authoring.aimAnimationType,  
                meleeAttackAnimationType = authoring.meleeAttackAnimationType,
            });
        }
    }
}
public struct UnitAnimation : IComponentData
{
    public AnimationDataSo.AnimationType idleAnimationType;
    public AnimationDataSo.AnimationType walkAnimationType;
    public AnimationDataSo.AnimationType shootAnimationType;
    public AnimationDataSo.AnimationType aimAnimationType;
    public AnimationDataSo.AnimationType meleeAttackAnimationType;


}