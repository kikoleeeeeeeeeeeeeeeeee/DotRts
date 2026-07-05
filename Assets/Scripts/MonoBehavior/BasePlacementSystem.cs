using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

public class BasePlacementSystem : MonoBehaviour
{
    public static BasePlacementSystem Instance { get; private set; }

    public event EventHandler OnBasePlaced;
    public event EventHandler OnBasePlacementStarted;
    public event EventHandler OnBasePlacementCancelled;

    [Header("Base Placement Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;
    
    private bool isPlacingBase = false;
    private EntityManager entityManager;
    private bool hasPlacedBase = false; // 每波只能放置一次

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    private void Update()
    {
        if (isPlacingBase)
        {
            HandleInput();
        }
    }

    public void StartBasePlacement()
    {
        isPlacingBase = true;
        OnBasePlacementStarted?.Invoke(this, EventArgs.Empty);
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main == null)
            {
                return;
            }
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                Vector3 position = hit.point;
                position.y = 0;

                if (IsPlacementValid(position))
                {
                    PlaceBase(position);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 检查游戏是否暂停
            if (Time.timeScale == 0f)
            {
                return;
            }
            
            CancelPlacement();
        }
    }

    private bool IsPlacementValid(Vector3 position)
    {
        // 检查障碍物
        Collider[] colliders = Physics.OverlapSphere(position, 2f, obstacleLayer);
        if (colliders.Length > 0)
        {
            return false;
        }

        // 从 MapManager 获取地图边界
        if (MapManager.Instance != null)
        {
            Bounds mapBounds = MapManager.Instance.GetMapBounds();
            if (!mapBounds.Contains(position))
            {
                return false;
            }
        }

        return true;
    }

    private void PlaceBase(Vector3 position)
    {
        if (hasPlacedBase)
        {
            Debug.LogWarning("[BasePlacementSystem] 每波只能放置一次基地！");
            return;
        }

        // 从 EntitiesReferenceAuthoring 获取 HQ 预制体 Entity
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            Debug.LogError("[BasePlacementSystem] ECS World 不存在！");
            return;
        }

        var query = entityManager.CreateEntityQuery(typeof(EntitiesReferences));
        if (query.CalculateEntityCount() == 0)
        {
            Debug.LogError("[BasePlacementSystem] 未找到 EntitiesReferences！");
            return;
        }

        var entitiesRefEntity = query.GetSingletonEntity();
        var entitiesReferences = entityManager.GetComponentData<EntitiesReferences>(entitiesRefEntity);

        if (entitiesReferences.buildingHQPrefabEntity == Entity.Null)
        {
            Debug.LogError("[BasePlacementSystem] HQ 预制体 Entity 为 Null！请在 EntitiesReferenceAuthoring 中分配 HQ 预制体。");
            return;
        }

        // 使用 EntityCommandBuffer 创建放置请求
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        Entity requestEntity = ecb.CreateEntity();
        ecb.AddComponent<PlaceBaseRequest>(requestEntity);
        ecb.SetComponent(requestEntity, new PlaceBaseRequest
        {
            position = position,
            hqPrefabEntity = entitiesReferences.buildingHQPrefabEntity
        });
        
        // 立即执行 ECB
        ecb.Playback(entityManager);
        ecb.Dispose();

        hasPlacedBase = true;
        isPlacingBase = false;

        OnBasePlaced?.Invoke(this, EventArgs.Empty);
        
        Debug.Log($"[BasePlacementSystem] 已发送基地放置请求到位置 {position}");
    }

    private void CancelPlacement()
    {
        isPlacingBase = false;
        OnBasePlacementCancelled?.Invoke(this, EventArgs.Empty);
    }

    public Vector3 GetBasePosition()
    {
        // 从 ECS 获取 HQ 位置
        var hqQuery = entityManager.CreateEntityQuery(typeof(BuildingHQ), typeof(LocalTransform));
        if (hqQuery.CalculateEntityCount() > 0)
        {
            var hqEntity = hqQuery.GetSingletonEntity();
            return entityManager.GetComponentData<LocalTransform>(hqEntity).Position;
        }
        return Vector3.zero;
    }

    public void RemoveBase()
    {
        // 清理 ECS HQ Entity
        var hqQuery = entityManager.CreateEntityQuery(typeof(BuildingHQ));
        foreach (var entity in hqQuery.ToEntityArray(Allocator.Temp))
        {
            if (entityManager.Exists(entity))
            {
                entityManager.DestroyEntity(entity);
            }
        }
        hasPlacedBase = false;
    }
    
    /// <summary>
    /// 重置放置标志（每波开始时调用）
    /// </summary>
    public void ResetPlacementFlag()
    {
        hasPlacedBase = false;
    }
}
