using Unity.Entities;
using UnityEditorInternal;
using UnityEngine;

public class EnemyAttackHqAuthoring : MonoBehaviour
{
    public class Baker : Baker<EnemyAttackHqAuthoring>
    {
        public override void Bake(EnemyAttackHqAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EnemyAttackHQ { });
        }
    }

}

public struct EnemyAttackHQ : IComponentData
{

}