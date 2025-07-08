using UnityEngine;
using Unity.Entities;
public class FindTargetAuthoring : MonoBehaviour
{

    public float range;
    public FactionType targetFaction;
    public float timeMax;


    public class Baker : Baker<FindTargetAuthoring>
    {

        public override void Bake(FindTargetAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FindTarget
            {  
                range = authoring.range,
                targetFaction = authoring.targetFaction,
                timeMax = authoring.timeMax,
            });
        }
    }
}
public struct FindTarget : IComponentData
{
    public float range;
    public FactionType targetFaction;
    public float timer;
    public float timeMax;
}