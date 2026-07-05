using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class RandomWalkingAuthoring : MonoBehaviour
{
    public float3 targetPosition;
    public float3 originPosition;
    public float distanceMin;
    public float distanceMax;
    public uint randomSeed;

    public class Baker : Baker<RandomWalkingAuthoring>
    {
        public override void Bake(RandomWalkingAuthoring authoring)
        {
            // ·ÀÖ¹ÖÖ×ÓÎª 0
            uint safeSeed = authoring.randomSeed == 0
                ? (uint)UnityEngine.Random.Range(1, int.MaxValue)
                : authoring.randomSeed;

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new RandomWalking
            {
                targetPosition = authoring.targetPosition,
                originPosition = authoring.originPosition,
                distanceMin = authoring.distanceMin,
                distanceMax = authoring.distanceMax,
                random = new Unity.Mathematics.Random(safeSeed), 
            });
        }

    }
}
public struct RandomWalking : IComponentData
{
    public float3 targetPosition;
    public float3 originPosition;
    public float distanceMin;
    public float distanceMax;
    public Unity.Mathematics.Random random;
}