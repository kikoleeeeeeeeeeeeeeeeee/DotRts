using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class RagDollManager : MonoBehaviour
{
    [SerializeField] private UnitTypeSoList unitTypeSoList;
    private void Start()
    {
        DotsEventsManager.Instance.OnHealthDead += DotsEventMananger_OnHealthDead;
    }

    private void DotsEventMananger_OnHealthDead(object sender, System.EventArgs e)
    {
        Entity entity = (Entity)sender;
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;


        if (entityManager.HasComponent<UnitTypeHolder>(entity))
        {
            LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);
            UnitTypeHolder unitTypeHolder = entityManager.GetComponentData<UnitTypeHolder>(entity);

            UnitTypeSo  unitTypeSo= unitTypeSoList.GetUnitTypeSO(unitTypeHolder.unitType);
            Instantiate(unitTypeSo.ragdollPrefab, localTransform.Position, Quaternion.identity);
        }

    }
}
