using Unity.Entities;
using UnityEngine;

public class HordeAuthoring : MonoBehaviour
{
    public float startTimer;
    public float spawnTimerMax;
    public int zombieAmountToSpawn;
    public float spawnAreaWidth;
    public float spawnAreaHeight;
    public GameObject minimapIconGameObject;
    public class Baker : Baker<HordeAuthoring>
    {


        public override void Bake(HordeAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            Entity minimapIconEntity = GetEntity(authoring.minimapIconGameObject, TransformUsageFlags.Dynamic);
            AddComponent(entity, new Horde
            {
                startTimer = authoring.startTimer,
                spawnTimerMax = authoring.spawnTimerMax,
                zombieAmountToSpawn = authoring.zombieAmountToSpawn,
                spawnAreaHeight = authoring.spawnAreaHeight,
                spawnAreaWidth = authoring.spawnAreaWidth,
                random = new Unity.Mathematics.Random((uint)entity.Index),
                minimapIconEntity = minimapIconEntity,
            });
        }
    }
}
public struct Horde : IComponentData
{
    public float startTimer;
    public float spawnTimer;
    public float spawnTimerMax;
    public int zombieAmountToSpawn;
    public float spawnAreaWidth;
    public float spawnAreaHeight;
    public Unity.Mathematics.Random random;
    public bool onStartSpawning;
    public bool onStartSpawningSoon;
    public bool isSetup;
    public Entity minimapIconEntity;
}