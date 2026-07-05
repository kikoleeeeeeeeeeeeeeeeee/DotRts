using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class BuildingBarrackUi : MonoBehaviour
{
    [SerializeField] private Button soldierButton;
    [SerializeField] private Button scoutButton;

    [SerializeField] private Image progressBarImage;
    [SerializeField] private RectTransform unitQueueContainer;
    [SerializeField] private RectTransform unitQueueTemplate;


    private Entity buildingBarracksEntity;
    private EntityManager entityManager;
    private void Awake()
    {
        soldierButton.onClick.AddListener(() =>
        {
            UnitTypeSo unitTypeSo = GameAssets.Instance.unitTypeListSO.GetUnitTypeSO(UnitTypeSo.UnitType.Soldier);
            if (!ResrouceManager.Instance.CanSpendResourceAmount(unitTypeSo.spawnCostResourceAmountArray))
            {
                return;
            }
            ResrouceManager.Instance.SpendResourceAmount(unitTypeSo.spawnCostResourceAmountArray);

            entityManager.SetComponentData(buildingBarracksEntity, new BuildingBarracksUnitEnqueue
            {
                unitType = UnitTypeSo.UnitType.Soldier
            });

            entityManager.SetComponentEnabled<BuildingBarracksUnitEnqueue>(buildingBarracksEntity, true);
        });

        scoutButton.onClick.AddListener(() =>
        {
            UnitTypeSo unitTypeSo = GameAssets.Instance.unitTypeListSO.GetUnitTypeSO(UnitTypeSo.UnitType.Scout);
            if (!ResrouceManager.Instance.CanSpendResourceAmount(unitTypeSo.spawnCostResourceAmountArray))
            {
                return;
            }
            ResrouceManager.Instance.SpendResourceAmount(unitTypeSo.spawnCostResourceAmountArray);

            entityManager.SetComponentData(buildingBarracksEntity, new BuildingBarracksUnitEnqueue
            {
                unitType = UnitTypeSo.UnitType.Scout
            });

            entityManager.SetComponentEnabled<BuildingBarracksUnitEnqueue>(buildingBarracksEntity, true);
        });
        unitQueueTemplate.gameObject.SetActive(false);
    }




    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        UnitSelectionManager.Instance.OnSelectdEntitiesChanged += UnitSelectManager_OnSelectdEntitiesChanged;
        DotsEventsManager.Instance.OnBarracksUnitQueueChanged += DotsEventsMananger_OnBarracksUnitQueueChanged;

        Hide();
    }

    private void DotsEventsMananger_OnBarracksUnitQueueChanged(object sender, System.EventArgs e)
    {
        Entity entity = (Entity)sender;
        if(entity == buildingBarracksEntity)
        {
            UpdateUnitQueueVisual();

        }
    }

    private void Update()
    {
        UpdateProgressBarVisual();
    }
    private void UnitSelectManager_OnSelectdEntitiesChanged(object sender, System.EventArgs e)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery entityQuery =   new EntityQueryBuilder(Allocator.Temp).WithAll<Selected,BuildingBarracks>().Build(entityManager);
    
        NativeArray<Entity> entityArray = entityQuery.ToEntityArray(Allocator.Temp);

        if (entityArray.Length > 0)
        {
            buildingBarracksEntity = entityArray[0];

            Show();
            UpdateProgressBarVisual();
        }
        else
        {
            buildingBarracksEntity = Entity.Null;
            Hide() ;
        }
    }

    private void UpdateProgressBarVisual()
    {
        if (buildingBarracksEntity == Entity.Null)
        { 
            progressBarImage.fillAmount = 0f;
            return;
        }

        BuildingBarracks buildingBarracks = 
         entityManager.GetComponentData<BuildingBarracks>(buildingBarracksEntity); 

        if(buildingBarracks.activeUnitType == UnitTypeSo.UnitType.None)
        {
            progressBarImage.fillAmount = 0f;
        }
        else
        {
            progressBarImage.fillAmount = buildingBarracks.progress / buildingBarracks.progressMax;
        }


    }

    private void UpdateUnitQueueVisual()
    {
        foreach(Transform child in unitQueueContainer)
        {
            if(child == unitQueueTemplate)
            {
                continue;
            }
            Destroy(child.gameObject);
        }
        DynamicBuffer<SpawnUnitTypeBuffer> spawnUnitTypeDynamicBuffer
                = entityManager.GetBuffer<SpawnUnitTypeBuffer>(buildingBarracksEntity, true);
        foreach(SpawnUnitTypeBuffer spawnUnitTypeBuffer in spawnUnitTypeDynamicBuffer)
        {
            RectTransform unitQueueRectTransform = Instantiate(unitQueueTemplate, unitQueueContainer);
            unitQueueRectTransform.gameObject.SetActive(true);

            UnitTypeSo unitTypeSo = GameAssets.Instance.unitTypeListSO.GetUnitTypeSO(spawnUnitTypeBuffer.unitType);
            unitQueueRectTransform.GetComponent<Image>().sprite = unitTypeSo.sprite;

        }
    }


    private void Show()
    {
        gameObject.SetActive(true);
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }

}
