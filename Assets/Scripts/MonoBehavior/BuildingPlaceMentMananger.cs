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
                    if (!entityQuery.TryGetSingleton<EntitiesReferences>(out EntitiesReferences entitiesReferences))
                    {
                        Debug.LogError("No EntitiesReferences found in the world. Make sure you have an EntitiesReferenceAuthoring component in your scene.");
                        return;
                    }

                    //Entity spawnedEntity = entityManager.Instantiate(buildingTypeSO.GetPrefabEntity(entitiesReferences));
                    //entityManager.SetComponentData(spawnedEntity, LocalTransform.FromPosition(mouseWorldPosition));

                    Entity buildingConstructionViusalEntity = entityManager.Instantiate(buildingTypeSO.GetViusalPrefabEntity(entitiesReferences));
                    entityManager.AddComponent<RuntimeEntity>(buildingConstructionViusalEntity);
                    entityManager.SetComponentData(buildingConstructionViusalEntity, LocalTransform.FromPosition(mouseWorldPosition +new Vector3(0, buildingTypeSO.constructionYOffset, 0)));

                    Entity buildingConstructionEntity = entityManager.Instantiate(entitiesReferences.buildingConstructionPrefabEntity);
                    entityManager.AddComponent<RuntimeEntity>(buildingConstructionEntity);
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

        // 检查是否为资源采集建筑
        bool isResourceBuilding = false;
        switch (buildingTypeSO.buildingType)
        {
            case BuildingTypeSo.BuildingType.GoldHarvester:
            case BuildingTypeSo.BuildingType.IronHarvester:
            case BuildingTypeSo.BuildingType.OilHarvester:
                isResourceBuilding = true;
                break;
        }
        Debug.Log($"Building type: {buildingTypeSO.buildingType}, isResourceBuilding: {isResourceBuilding}");

        // 碰撞过滤条件 - 添加 ground layer
        int groundLayer = LayerMask.NameToLayer("Ground");
        CollisionFilter collisionFilter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << GameAssets.BUILDINGS_LAYER | 1u << GameAssets.DEFAULT_LAYER | (groundLayer >= 0 ? 1u << groundLayer : 0u),
            GroupIndex = 0,
        };

        // 检查 prefab 是否存在
        if (buildingTypeSO.prefab == null)
        {
            Debug.LogError("BuildingTypeSO prefab is not assigned for: " + buildingTypeSO.buildingType);
            return false;
        }
        Debug.Log("Prefab is valid: " + buildingTypeSO.prefab.name);

        UnityEngine.BoxCollider boxCollider = buildingTypeSO.prefab.GetComponent<UnityEngine.BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogError("BoxCollider not found on prefab for: " + buildingTypeSO.buildingType);
            return false;
        }
        Debug.Log("BoxCollider found, size: " + boxCollider.size);

        float bonusExtents = 1.1f;

        NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp);

        Debug.Log("Before OverlapBox, position: " + mouseWorldPosition);
        // 为资源建筑创建特殊的碰撞过滤器，允许与资源点碰撞
        CollisionFilter checkCollisionFilter = collisionFilter;
        if (isResourceBuilding)
        {
            // 资源建筑需要检测所有层，确保能检测到资源点
            checkCollisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u, // 检测所有层
                GroupIndex = 0,
            };
        }

        if (
        collisionWorld.OverlapBox(mouseWorldPosition,
            Quaternion.identity, boxCollider.size * .5f * bonusExtents,
            ref distanceHitList, checkCollisionFilter))
        {
            Debug.Log("OverlapBox collision detected, hit count: " + distanceHitList.Length);
            // 检查是否为资源采集建筑，且碰撞的是资源点
            if (isResourceBuilding)
            {
                bool hasResourceCollision = false;
                foreach (DistanceHit distanceHit in distanceHitList)
                {
                    if (entityManager.HasComponent<ResourceTypeSoHolder>(distanceHit.Entity))
                    {
                        hasResourceCollision = true;
                        Debug.Log("OverlapBox collision with resource (allowed for resource building)");
                        break;
                    }
                }
                if (!hasResourceCollision)
                {
                    Debug.Log("OverlapBox collision with non-resource object");
                    return false;
                }
            }
            else
            {
                Debug.Log("OverlapBox collision detected");
                return false;
            }
        }
        else
        {
            Debug.Log("No OverlapBox collision detected");
        }

        distanceHitList.Clear();
        Debug.Log("Before OverlapSphere, distance: " + buildingTypeSO.buildingDistanceMin);
        if(collisionWorld.OverlapSphere(
            mouseWorldPosition,
            buildingTypeSO.buildingDistanceMin,
            ref distanceHitList,
            collisionFilter))
        {
            Debug.Log("OverlapSphere collision detected, hit count: " + distanceHitList.Length);
            foreach(DistanceHit distanceHit in distanceHitList)
            {
                // 资源采集建筑应该忽略与资源点的碰撞
                if (isResourceBuilding && entityManager.HasComponent<ResourceTypeSoHolder>(distanceHit.Entity))
                {
                    Debug.Log("Ignoring resource collision for resource building");
                    continue;
                }

                if (entityManager.HasComponent<BuildingTypeSOHolder>(distanceHit.Entity))
                {
                    BuildingTypeSOHolder buildingTypeSOHolder = entityManager.GetComponentData<BuildingTypeSOHolder>(distanceHit.Entity);
                    if(buildingTypeSOHolder.buildingType == buildingTypeSO.buildingType)
                    {
                        Debug.Log("Found same building type, returning false");
                        return false;
                    }
                }
                if (entityManager.HasComponent<BuildignConstruction>(distanceHit.Entity))
                {
                    BuildignConstruction buildignConstruction = entityManager.GetComponentData<BuildignConstruction>(distanceHit.Entity);
                    if (buildignConstruction.buildingType == buildingTypeSO.buildingType)
                    {
                        Debug.Log("Found same building construction, returning false");
                        return false;
                    }
                }
            }
        }
        else
        {
            Debug.Log("No OverlapSphere collision detected");
        }
        
        Debug.Log("After OverlapSphere, proceeding to resource check");

        // 检查资源采集建筑的特殊条件
        if (isResourceBuilding)
        {
            bool hasVaildNearbyResourceNodes = false;

            Debug.Log($"Checking for resources near position: {mouseWorldPosition}");

            // 使用传统的 Physics.OverlapSphere 来检测资源点 GameObject
            // 使用资源建筑的 buildingDistanceMin * 2 作为检测半径
            UnityEngine.Collider[] colliders = UnityEngine.Physics.OverlapSphere(mouseWorldPosition, buildingTypeSO.buildingDistanceMin * 2);
            Debug.Log($"Found {colliders.Length} objects in radius");

            foreach (UnityEngine.Collider collider in colliders)
            {
                Debug.Log($"Hit object: {collider.gameObject.name}");

                // 检查是否有ResourceTypeSoHolderAuthoring组件
                ResourceTypeSoHolderAuthoring resourceTypeSoHolder = collider.GetComponent<ResourceTypeSoHolderAuthoring>();
                if (resourceTypeSoHolder != null)
                {
                    Debug.Log($"Found resource with type: {resourceTypeSoHolder.resourceType}");

                    // 根据建筑类型检查对应资源
                    bool hasMatchingResource = false;
                    switch (buildingTypeSO.buildingType)
                    {
                        case BuildingTypeSo.BuildingType.GoldHarvester:
                            hasMatchingResource = resourceTypeSoHolder.resourceType == ResourceTypeSo.ResourceType.Gold;
                            Debug.Log($"Checking for Gold resource: {hasMatchingResource}");
                            break;
                        case BuildingTypeSo.BuildingType.IronHarvester:
                            hasMatchingResource = resourceTypeSoHolder.resourceType == ResourceTypeSo.ResourceType.Iron;
                            Debug.Log($"Checking for Iron resource: {hasMatchingResource}");
                            break;
                        case BuildingTypeSo.BuildingType.OilHarvester:
                            hasMatchingResource = resourceTypeSoHolder.resourceType == ResourceTypeSo.ResourceType.Oil;
                            Debug.Log($"Checking for Oil resource: {hasMatchingResource}");
                            break;
                    }

                    if (hasMatchingResource)
                    {
                        hasVaildNearbyResourceNodes = true;
                        Debug.Log("Found matching resource!");
                        break;
                    }
                }
                else
                {
                    Debug.Log("Hit object is not a resource");
                }
            }

            if (!hasVaildNearbyResourceNodes)
            {
                Debug.Log("No matching resources found, cannot place building");
                return false;
            }
            else
            {
                Debug.Log("Matching resources found, can place building");
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