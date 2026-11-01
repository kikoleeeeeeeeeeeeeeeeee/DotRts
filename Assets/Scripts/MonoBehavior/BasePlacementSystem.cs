using System;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;

/// <summary>
/// HQ（基地）放置的 Mono 侧入口。实际实体创建由 ECS 的 <see cref="BasePlacementECS"/> 消费 <see cref="PlaceBaseRequest"/> 完成。
///
/// 本类只负责：接收鼠标输入、做地面/障碍物/地图边界校验、发出放置请求。
/// 放置失败或按 ESC 取消时，<see cref="WaveManager"/> 会重新调用 <see cref="StartBasePlacement"/> 兜底。
/// </summary>
public class BasePlacementSystem : MonoBehaviour
{
    public static BasePlacementSystem Instance { get; private set; }

    public event EventHandler OnBasePlaced;
    public event EventHandler OnBasePlacementStarted;
    public event EventHandler OnBasePlacementCancelled;

    [Header("Base Placement Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("HQ 放置时的障碍物检测半径")]
    [SerializeField] private float placementCheckRadius = 2f;

    private bool isPlacingBase = false;
    private EntityManager entityManager;
    private bool hasPlacedBase = false;

    public bool IsPlacingBase => isPlacingBase;
    public bool HasPlacedBase => hasPlacedBase;

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
        EnsureEntityManager();
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
        if (hasPlacedBase)
        {
            // 本局已经放过 HQ，不再重复要求放置。
            return;
        }

        isPlacingBase = true;
        OnBasePlacementStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 供 WaveManager 超时兜底调用：跳过鼠标输入，直接把 HQ 放到指定位置。
    /// </summary>
    public bool TryPlaceBaseAt(Vector3 position, out Vector3 placedPosition)
    {
        placedPosition = position;
        if (hasPlacedBase) return false;

        position.y = 0f;
        placedPosition = position;

        if (!IsPlacementValid(position)) return false;

        PlaceBase(position);
        return true;
    }

    /// <summary>
    /// 自动兜底放置：从地图中心向外螺旋采样，避开障碍物与边界。
    /// 地图是随机生成的，中心点经常被障碍物占用，只试中心会永久失败。
    /// </summary>
    public bool TryAutoPlaceBase(out Vector3 placedPosition)
    {
        placedPosition = Vector3.zero;
        if (hasPlacedBase) return false;

        Vector3 center = Vector3.zero;
        if (MapManager.Instance != null)
        {
            center = MapManager.Instance.GetMapBounds().center;
        }

        center.y = 0f;
        if (IsPlacementValid(center))
        {
            return TryPlaceBaseAt(center, out placedPosition);
        }

        const int ringCount = 12;
        const float ringStep = 4f;
        for (int ring = 1; ring <= ringCount; ring++)
        {
            float radius = ring * ringStep;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (IsPlacementValid(candidate))
                {
                    return TryPlaceBaseAt(candidate, out placedPosition);
                }
            }
        }

        return false;
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int mask = ResolveGroundMask();

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mask))
            {
                Vector3 position = hit.point;
                position.y = 0f;

                if (IsPlacementValid(position))
                {
                    PlaceBase(position);
                }
                else
                {
                    Debug.Log("[BasePlacementSystem] 该位置不可放置（被障碍物占用或超出地图边界）。");
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 暂停时不响应取消，避免和暂停菜单抢同一个按键。
            if (Time.timeScale == 0f) return;

            CancelPlacement();
        }
    }

    private int ResolveGroundMask()
    {
        if (groundLayer.value != 0) return groundLayer.value;

        // groundLayer 未在 Inspector 配置时，退回到 Default 层，避免永远点不到地面导致卡死。
        Debug.LogWarning("[BasePlacementSystem] groundLayer 未配置，已退回到 Default 层。");
        return LayerMask.GetMask("Default");
    }

    public bool IsPlacementValid(Vector3 position)
    {
        if (obstacleLayer.value != 0)
        {
            Collider[] colliders = Physics.OverlapSphere(position, placementCheckRadius, obstacleLayer);
            if (colliders.Length > 0)
            {
                return false;
            }
        }

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
            Debug.LogWarning("[BasePlacementSystem] 本局已经放置过 HQ。");
            return;
        }

        if (!EnsureEntityManager())
        {
            Debug.LogError("[BasePlacementSystem] ECS World 尚未就绪，无法放置 HQ。");
            return;
        }

        EntityQuery query = entityManager.CreateEntityQuery(typeof(EntitiesReferences));
        if (query.CalculateEntityCount() == 0)
        {
            Debug.LogError("[BasePlacementSystem] 未找到 EntitiesReferences，无法放置 HQ。");
            query.Dispose();
            return;
        }

        Entity entitiesRefEntity = query.GetSingletonEntity();
        EntitiesReferences entitiesReferences = entityManager.GetComponentData<EntitiesReferences>(entitiesRefEntity);
        query.Dispose();

        if (entitiesReferences.buildingHQPrefabEntity == Entity.Null)
        {
            Debug.LogError("[BasePlacementSystem] HQ 预制体 Entity 为 Null，请在 EntitiesReferenceAuthoring 中分配 HQ 预制体。");
            return;
        }

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        Entity requestEntity = ecb.CreateEntity();
        ecb.AddComponent(requestEntity, new PlaceBaseRequest
        {
            position = position,
            hqPrefabEntity = entitiesReferences.buildingHQPrefabEntity
        });
        ecb.Playback(entityManager);
        ecb.Dispose();

        hasPlacedBase = true;
        isPlacingBase = false;

        OnBasePlaced?.Invoke(this, EventArgs.Empty);

        Debug.Log($"[BasePlacementSystem] 已发送 HQ 放置请求：{position}");
    }

    private void CancelPlacement()
    {
        if (hasPlacedBase) return;

        isPlacingBase = false;
        OnBasePlacementCancelled?.Invoke(this, EventArgs.Empty);
        Debug.Log("[BasePlacementSystem] 已取消放置，稍后会自动重新进入放置模式。");
    }

    public Vector3 GetBasePosition()
    {
        if (!EnsureEntityManager()) return Vector3.zero;

        EntityQuery hqQuery = entityManager.CreateEntityQuery(typeof(BuildingHQ), typeof(LocalTransform));
        Vector3 result = Vector3.zero;
        if (hqQuery.CalculateEntityCount() > 0)
        {
            Entity hqEntity = hqQuery.GetSingletonEntity();
            result = entityManager.GetComponentData<LocalTransform>(hqEntity).Position;
        }
        hqQuery.Dispose();
        return result;
    }

    public void RemoveBase()
    {
        if (!EnsureEntityManager()) return;

        EntityQuery hqQuery = entityManager.CreateEntityQuery(typeof(BuildingHQ));
        NativeArray<Entity> entities = hqQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            if (entityManager.Exists(entities[i]))
            {
                entityManager.DestroyEntity(entities[i]);
            }
        }
        entities.Dispose();
        hqQuery.Dispose();

        hasPlacedBase = false;
        isPlacingBase = false;
    }

    /// <summary>保留兼容：现在基地整局只放一次，不再每波重置。</summary>
    public void ResetPlacementFlag()
    {
        hasPlacedBase = false;
    }

    /// <summary>
    /// 每次现取 EntityManager。ECS World 在进入 Play / 场景重载后可能被重建，
    /// 缓存旧的 EntityManager（struct 内含 world 指针）会变成失效句柄，调用即空引用。
    /// </summary>
    private bool EnsureEntityManager()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return false;

        entityManager = world.EntityManager;
        return true;
    }
}
