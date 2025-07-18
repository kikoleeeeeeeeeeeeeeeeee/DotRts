using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem.Android.LowLevel;

public class GameSceneTagAuthoring : MonoBehaviour
{
   public class Baker : Baker<GameSceneTagAuthoring>
    {
        public override void Bake(GameSceneTagAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new GameSceneTag());
        }
    }
}

public struct GameSceneTag : IComponentData
{

}