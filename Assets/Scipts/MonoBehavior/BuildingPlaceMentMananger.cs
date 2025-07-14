using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingPlaceMentMananger : MonoBehaviour
{
    public static BuildingPlaceMentMananger Instance {  get; private set; }
    public event EventHandler OnActiveBuildingTypeSOchanged;


    
    [SerializeField] private BuildingTypeSo buildingTypeSO;
    [SerializeField] private UnityEngine.Material ghostMaterial;


    private Transform ghostTransform;
    private void Awake()
    {
        Instance = this;
    }
    private void Update()
    {
        if(ghostTransform != null)
        {
            ghostTransform.position = MouseWorldPostion.Instance.GetPostion();
        }
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if(buildingTypeSO.IsNone())
        {
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {   
            SetActiveBuildingTypeSO(GameAssets.Instance.buildingTypeListSo.none);
        }
        

        if (Input.GetMouseButtonDown(0))
        {
            if (ResrouceManager.Instance.CanSpendResourceAmount(buildingTypeSO.buildCostResourceAmountArray))
            {
                if (CanPlaceBuilding())
                {
                    ResrouceManager.Instance.SpendResourceAmount(buildingTypeSO.buildCostResourceAmountArray);

                    Vector3 mouseWorldPosition = MouseWorldPostion.Instance.GetPostion();

                    EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

                    EntityQuery entityQuery = entityManager.CreateEntityQuery(typeof(EntitiesReferences));
                    EntitiesReferences entitiesReferences = entityQuery.GetSingleton<EntitiesReferences>();

                    //Entity spawnedEntity = entityManager.Instantiate(buildingTypeSO.GetPrefabEntity(entitiesReferences));
                    //entityManager.SetComponentData(spawnedEntity, LocalTransform.FromPosition(mouseWorldPosition));

                    Entity buildingConstructionViusalEntity = entityManager.Instantiate(buildingTypeSO.GetViusalPrefabEntity(entitiesReferences));
                    entityManager.SetComponentData(buildingConstructionViusalEntity, LocalTransform.FromPosition(mouseWorldPosition +new Vector3(0, buildingTypeSO.constructionYOffset, 0)));

                    Entity buildingConstructionEntity = entityManager.Instantiate(entitiesReferences.buildingConstructionPrefabEntity);
                    entityManager.SetComponentData(buildingConstructionEntity, LocalTransform.FromPosition(mouseWorldPosition));

                    entityManager.SetComponentData(buildingConstructionEntity, new BuildignConstruction
                    {
                        buildingType = buildingTypeSO.buildingType,
                        constructionTimer = 0f,
                        constructionTimerMax = buildingTypeSO.buildingConstructionTimerMax,
                        finalPrefabEntity = buildingTypeSO.GetPrefabEntity(entitiesReferences),
                        visualEntity = buildingConstructionViusalEntity,
                        starPosition = mouseWorldPosition + new Vector3(0, buildingTypeSO.constructionYOffset, 0),
                        endPosition = mouseWorldPosition,
                    });
                }
            }
        }
    }
    private bool CanPlaceBuilding()
    {
        Vector3 mouseWorldPosition = MouseWorldPostion.Instance.GetPostion();

        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery entityQuery = entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));

        PhysicsWorldSingleton physicsWorldSingleton = entityQuery.GetSingleton<PhysicsWorldSingleton>();

        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
        CollisionFilter collisionFilter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << GameAssets.BUILDINGS_LAYER | 1u << GameAssets.DEFAULT_LAYER,
            GroupIndex = 0,
        };

        UnityEngine.BoxCollider boxCollider = buildingTypeSO.prefab.GetComponent<UnityEngine.BoxCollider>();

        float bonusExtents = 1.1f;

        NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp);

        if (
        collisionWorld.OverlapBox(mouseWorldPosition,
            Quaternion.identity, boxCollider.size * .5f * bonusExtents,
            ref distanceHitList, collisionFilter))
        {
            return false;
        }
        
        distanceHitList.Clear();
        if(collisionWorld.OverlapSphere(
            mouseWorldPosition,
            buildingTypeSO.buildingDistanceMin,
            ref distanceHitList,
            collisionFilter))
        {
            foreach(DistanceHit distanceHit in distanceHitList)
            {
                if (entityManager.HasComponent<BuildingTypeSOHolder>(distanceHit.Entity))
                {
                    BuildingTypeSOHolder buildingTypeSOHolder = entityManager.GetComponentData<BuildingTypeSOHolder>(distanceHit.Entity);
                    if(buildingTypeSOHolder.buildingType == buildingTypeSO.buildingType)
                    {
                        return false;
                    }
                }
                if (entityManager.HasComponent<BuildignConstruction>(distanceHit.Entity))
                {
                    BuildignConstruction buildignConstruction = entityManager.GetComponentData<BuildignConstruction>(distanceHit.Entity);
                    if (buildignConstruction.buildingType == buildingTypeSO.buildingType)
                    {
                        return false;
                    }
                }
            }
        }

        if (buildingTypeSO is BuildingResourceHarvestTypeSo buildingResourceHarvestTypeSo)
        {

            bool hasVaildNearbyResourceNodes = false;
            if (collisionWorld.OverlapSphere(
            mouseWorldPosition,
            buildingResourceHarvestTypeSo.harvestDistance,
            ref distanceHitList,
            collisionFilter))
            {

                //检测资源点距离
                foreach (DistanceHit distanceHit in distanceHitList)
                {
                    if (entityManager.HasComponent<ResourceTypeSoHolder>(distanceHit.Entity))
                    {
                        ResourceTypeSoHolder resourceTypeSOHolder = entityManager.GetComponentData<ResourceTypeSoHolder>(distanceHit.Entity);
                        if (resourceTypeSOHolder.resourceType == buildingResourceHarvestTypeSo.harvestableResourceType)
                        { 
                            //在资源点旁边
                            hasVaildNearbyResourceNodes =true;
                            break;
                        }
                    }
                }
                
            }
            if (!hasVaildNearbyResourceNodes)
            {
                return false;
            }
        }

        return true;
    }

    public BuildingTypeSo GetActiveBuidlingTypeSO()
    {
        return buildingTypeSO;
    }

    public void SetActiveBuildingTypeSO(BuildingTypeSo buildingTypeSo)
    {
        this.buildingTypeSO = buildingTypeSo;

        if(ghostTransform!= null)
        {
            Destroy(ghostTransform.gameObject);
        }

        if (!buildingTypeSO.IsNone())
        {
            ghostTransform = Instantiate(buildingTypeSO.visualPrefab);
            foreach (MeshRenderer meshRenderer in ghostTransform.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material = ghostMaterial;
            }
        }


        OnActiveBuildingTypeSOchanged?.Invoke(this, EventArgs.Empty);
    }
}
