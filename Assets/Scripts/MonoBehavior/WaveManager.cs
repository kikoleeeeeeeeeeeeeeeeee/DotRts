using System;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 波次权威（单一推进方）。
///
/// 职责划分：
/// - 本类负责：准备阶段计时、波次计时、波次索引推进、Buff 衔接、胜负判定、基地放置编排。
/// - ECS 侧（<see cref="WaveSystem"/>）只负责：按请求配置生成参数、生成僵尸、清理场上敌人。
///
/// 约定：<see cref="currentWaveIndex"/> 只允许在 <see cref="AdvanceToNextWave"/> 中自增一次。
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    public event EventHandler OnWaveStarted;
    public event EventHandler OnWaveEnded;
    public event EventHandler OnBuffSelectionStarted;
    public event EventHandler<int> OnZombieKilled;
    public event EventHandler<float> OnWaveTimerUpdated;
    public event EventHandler OnMapGenerated;
    public event EventHandler OnBasePlacementStarted;
    public event EventHandler OnGameOver;
    public event EventHandler OnVictory;
    public event EventHandler OnStateChanged;

    [Header("Wave Settings")]
    [SerializeField] private WaveDataSo waveDataSo;
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private BasePlacementSystem basePlacementSystem;
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private BuffManager buffManager;

    [Header("Guards")]
    [Tooltip("等待玩家放置 HQ 的最长秒数，超时后自动在地图中心放置，避免准备阶段永久卡死")]
    [SerializeField] private float basePlacementTimeout = 60f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = false;

    // ---- 运行时状态 ----
    private int currentWaveIndex = 0;
    private bool isWaveActive = false;
    private bool isPreparationPhase = false;
    private bool isBasePlaced = false;
    private bool awaitingBuffSelection = false;
    private bool isGameOver = false;
    private bool isVictory = false;
    private bool hasGeneratedMap = false;
    private bool isInitialized = false;

    private int zombiesKilledInCurrentWave = 0;
    private float waveTimer = 0f;
    private float preparationTimer = 0f;
    private float placementWaitTimer = 0f;
    private int killSyncFrameCounter = 0;

    // ---- 只读访问 ----
    public int CurrentWaveIndex => currentWaveIndex;
    public bool IsWaveActive => isWaveActive;
    public bool IsPreparationPhase => isPreparationPhase;
    public bool IsBasePlaced => isBasePlaced;
    public bool IsAwaitingBuffSelection => awaitingBuffSelection;
    public bool IsGameOver => isGameOver;
    public bool IsVictory => isVictory;
    public int ZombiesKilledInCurrentWave => zombiesKilledInCurrentWave;
    public float WaveTimer => waveTimer;
    public float PreparationTimer => preparationTimer;
    public bool IsInitialized => isInitialized;
    public WaveDataSo WaveDataSo => waveDataSo;
    public int TotalWaves => (waveDataSo != null && waveDataSo.waves != null) ? waveDataSo.waves.Length : 0;

    public WaveData CurrentWaveData
    {
        get
        {
            if (waveDataSo == null || waveDataSo.waves == null || waveDataSo.waves.Length == 0)
            {
                return default;
            }

            int index = Mathf.Clamp(currentWaveIndex, 0, waveDataSo.waves.Length - 1);
            return waveDataSo.waves[index];
        }
    }

    private void Awake()
    {
        // Instance 为空可能有两种情况：真没实例，或残留引用指向已销毁对象（Unity 的 == 会判为 null）。
        // 这里只禁用重复组件，不销毁整个 GameObject，避免把场景里的管理器对象误删。
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[WaveManager] 检测到重复实例（已有 {Instance.name}），禁用重复组件：{name}");
            Destroy(this);
            return;
        }

        Instance = this;

        int waveCount = (waveDataSo != null && waveDataSo.waves != null) ? waveDataSo.waves.Length : 0;
        Debug.Log($"[WaveManager] Awake 完成：Instance={name}，波次配置 {waveCount} 波");
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        // 场景重载不会重置 timeScale，这里统一恢复，
        // 避免上一局 GameOver/Buff 面板残留的 timeScale=0 让新一局直接冻结。
        GameTimeController.ClearAll();

        EnsureWaveData();
        EnsureBuffManager();
        HookBasePlacementSystem();

        // 重开一局时清空上一局残留的持久化 Buff 乘区（场景重载不会重建 ECS 世界）。
        if (buffManager != null)
        {
            buffManager.ClearGlobalBuffs();
        }

        isInitialized = true;
        StartPreparationPhase();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        var system = ResolveBasePlacementSystem();
        if (system != null)
        {
            system.OnBasePlaced -= BasePlacementSystem_OnBasePlaced;
            system.OnBasePlacementCancelled -= BasePlacementSystem_OnBasePlacementCancelled;
        }
    }

    private void Update()
    {
        if (!isInitialized || isGameOver || isVictory) return;
        if (awaitingBuffSelection) return;
        if (Time.timeScale <= 0f) return;

        if (isPreparationPhase)
        {
            if (isBasePlaced)
            {
                preparationTimer -= Time.deltaTime;
                if (preparationTimer <= 0f)
                {
                    EndPreparationPhase();
                }
            }
            else
            {
                placementWaitTimer += Time.deltaTime;
                if (placementWaitTimer >= basePlacementTimeout)
                {
                    TryAutoPlaceBase();
                }
            }
            return;
        }

        if (isWaveActive)
        {
            SyncKillCountFromECS();

            waveTimer -= Time.deltaTime;
            OnWaveTimerUpdated?.Invoke(this, waveTimer);
            if (waveTimer <= 0f)
            {
                EndCurrentWave();
            }
        }
    }

    /// <summary>
    /// 击杀统计以 ECS 侧 <see cref="HealthDeadSystem"/> 为准，这里按节流同步到 Mono 供 UI 使用。
    /// </summary>
    private void SyncKillCountFromECS()
    {
        killSyncFrameCounter++;
        if (killSyncFrameCounter % 10 != 0) return;

        if (!TryGetEntityManager(out EntityManager entityManager)) return;

        Entity waveStateEntity = GetWaveStateEntity(entityManager);
        if (waveStateEntity == Entity.Null) return;

        int killed = entityManager.GetComponentData<WaveState>(waveStateEntity).zombiesKilledInCurrentWave;
        if (killed == zombiesKilledInCurrentWave) return;

        zombiesKilledInCurrentWave = killed;
        OnZombieKilled?.Invoke(this, killed);
    }

    // ---------------------------------------------------------------- 初始化

    private void EnsureWaveData()
    {
        if (waveDataSo != null && waveDataSo.waves != null && waveDataSo.waves.Length > 0)
        {
            return;
        }

        Debug.LogError("[WaveManager] WaveDataSo 未指定或没有任何波次数据！已退回到临时兜底配置，请在场景中为 WaveManager 指定 WaveDataSo。");
        waveDataSo = CreateFallbackWaveData();
    }

    private WaveDataSo CreateFallbackWaveData()
    {
        WaveDataSo fallback = ScriptableObject.CreateInstance<WaveDataSo>();
        fallback.globalPreparationTime = 5f;
        fallback.baseSpawnInterval = 1f;
        fallback.spawnIntervalReductionPerWave = 0.05f;
        fallback.waves = new WaveData[1];
        fallback.waves[0] = new WaveData
        {
            waveNumber = 1,
            waveDuration = 300f,
            preparationTime = 5f,
            baseZombieCount = 10,
            additionalZombiePerWave = 5,
            spawnInterval = 1f,
            zombieHealthMultiplier = 1f,
            zombieDamageMultiplier = 1f,
            isSpecialWave = false,
            specialWaveType = SpecialWaveType.Normal,
            bossWaveConfig = new BossWaveConfig
            {
                bossHealth = 100,
                bossDamage = 10,
                bossSpawnDelay = 5f,
                minionsCount = 0
            }
        };
        return fallback;
    }

    private void EnsureBuffManager()
    {
        if (buffManager == null)
        {
            buffManager = BuffManager.Instance;
        }

        if (buffManager == null)
        {
            buffManager = FindObjectOfType<BuffManager>();
        }

        if (buffManager == null)
        {
            GameObject buffManagerObj = new GameObject("BuffManager");
            buffManager = buffManagerObj.AddComponent<BuffManager>();
        }
    }

    private BasePlacementSystem ResolveBasePlacementSystem()
    {
        if (basePlacementSystem != null)
        {
            return basePlacementSystem;
        }
        return BasePlacementSystem.Instance;
    }

    private void HookBasePlacementSystem()
    {
        var system = ResolveBasePlacementSystem();
        if (system == null)
        {
            Debug.LogError("[WaveManager] 场景中缺少 BasePlacementSystem，HQ 将无法放置。");
            return;
        }

        system.OnBasePlaced -= BasePlacementSystem_OnBasePlaced;
        system.OnBasePlaced += BasePlacementSystem_OnBasePlaced;
        system.OnBasePlacementCancelled -= BasePlacementSystem_OnBasePlacementCancelled;
        system.OnBasePlacementCancelled += BasePlacementSystem_OnBasePlacementCancelled;
    }

    // ---------------------------------------------------------------- 准备阶段

    /// <summary>
    /// 进入准备阶段。地图只在整局第一次调用时生成，后续波次保留 HQ、建筑与寻路 CostMap。
    /// </summary>
    public void StartPreparationPhase()
    {
        if (isGameOver || isVictory) return;

        if (waveDataSo == null || waveDataSo.waves == null || waveDataSo.waves.Length == 0)
        {
            Debug.LogError("[WaveManager] 没有可用的波次配置，无法进入准备阶段。");
            return;
        }

        if (currentWaveIndex >= waveDataSo.waves.Length)
        {
            Victory();
            return;
        }

        isWaveActive = false;
        isPreparationPhase = true;
        SetECSWaveActive(false);

        if (!hasGeneratedMap)
        {
            GenerateMapOnce();
        }

        if (!isBasePlaced)
        {
            placementWaitTimer = 0f;
            BeginBasePlacement();
        }

        preparationTimer = ResolvePreparationTime(currentWaveIndex);
        OnStateChanged?.Invoke(this, EventArgs.Empty);

        if (verboseLogging)
        {
            Debug.Log($"[WaveManager] 进入准备阶段：第 {currentWaveIndex + 1}/{waveDataSo.waves.Length} 波，准备 {preparationTimer:F1}s" +
                      (isBasePlaced ? "" : "（等待放置 HQ）"));
        }
    }

    /// <summary>开始/重新开始 HQ 放置（ESC 取消后由兜底逻辑再次调用）。</summary>
    public void BeginBasePlacement()
    {
        if (isBasePlaced || !isPreparationPhase) return;

        var system = ResolveBasePlacementSystem();
        if (system == null)
        {
            Debug.LogError("[WaveManager] No BasePlacementSystem found! Cannot start base placement.");
            return;
        }

        system.StartBasePlacement();
        OnBasePlacementStarted?.Invoke(this, EventArgs.Empty);
    }

    private float ResolvePreparationTime(int waveIndex)
    {
        float perWave = waveDataSo.waves[waveIndex].preparationTime;
        if (perWave > 0f)
        {
            return perWave;
        }
        return Mathf.Max(1f, waveDataSo.globalPreparationTime);
    }

    private void GenerateMapOnce()
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError("[WaveManager] MapManager.Instance is null! Map will not be generated.");
            return;
        }

        int seed = UnityEngine.Random.Range(1, int.MaxValue);
        MapManager.Instance.GenerateMap(seed);
        hasGeneratedMap = true;
        OnMapGenerated?.Invoke(this, EventArgs.Empty);

        if (verboseLogging)
        {
            Debug.Log($"[WaveManager] 地图已生成（seed={seed}），本局后续波次不再重建地图。");
        }
    }

    private void BasePlacementSystem_OnBasePlaced(object sender, EventArgs e)
    {
        isBasePlaced = true;
        placementWaitTimer = 0f;
        OnStateChanged?.Invoke(this, EventArgs.Empty);

        if (verboseLogging)
        {
            Debug.Log($"[WaveManager] HQ 已放置，准备阶段开始计时（{preparationTimer:F1}s）。");
        }
    }

    private void BasePlacementSystem_OnBasePlacementCancelled(object sender, EventArgs e)
    {
        if (!isInitialized || isGameOver || isVictory) return;
        if (!isPreparationPhase || isBasePlaced) return;

        // ESC 取消后自动重新进入放置模式，避免准备阶段永久卡死。
        placementWaitTimer = 0f;
        CancelInvoke(nameof(BeginBasePlacement));
        Invoke(nameof(BeginBasePlacement), 0.5f);
    }

    private void TryAutoPlaceBase()
    {
        var system = ResolveBasePlacementSystem();
        if (system == null) return;

        if (system.TryAutoPlaceBase(out Vector3 placedPosition))
        {
            placementWaitTimer = 0f;
            Debug.LogWarning($"[WaveManager] 等待放置 HQ 超过 {basePlacementTimeout:F0}s，已在 {placedPosition} 自动放置。");
        }
        else
        {
            // 整张地图都找不到可用点时下个检查点再试，避免每帧刷日志。
            placementWaitTimer = basePlacementTimeout - 1f;
            Debug.LogError("[WaveManager] 自动放置 HQ 失败：地图上没有可用位置。");
        }
    }

    private void EndPreparationPhase()
    {
        if (isWaveActive) return;

        isPreparationPhase = false;
        isWaveActive = true;
        zombiesKilledInCurrentWave = 0;
        waveTimer = CurrentWaveData.waveDuration;

        RequestECSWaveStart(currentWaveIndex);

        OnWaveStarted?.Invoke(this, EventArgs.Empty);
        OnStateChanged?.Invoke(this, EventArgs.Empty);

        if (verboseLogging)
        {
            Debug.Log($"[WaveManager] 第 {currentWaveIndex + 1} 波开始，持续 {waveTimer:F1}s，僵尸 {CurrentWaveData.baseZombieCount + CurrentWaveData.additionalZombiePerWave * currentWaveIndex} 只。");
        }
    }

    // ---------------------------------------------------------------- 波次结束

    /// <summary>
    /// 结束当前波次。幂等：非激活状态调用会被忽略，避免双计时导致的重复触发。
    /// </summary>
    public void EndCurrentWave()
    {
        if (!isWaveActive) return;
        if (isGameOver || isVictory) return;

        isWaveActive = false;
        waveTimer = 0f;

        SetECSWaveActive(false);
        RequestClearFieldEnemies();

        OnWaveEnded?.Invoke(this, EventArgs.Empty);

        bool isLastWave = currentWaveIndex + 1 >= waveDataSo.waves.Length;
        if (isLastWave)
        {
            Victory();
            return;
        }

        isPreparationPhase = true;
        preparationTimer = ResolvePreparationTime(currentWaveIndex + 1);

        if (buffManager != null)
        {
            awaitingBuffSelection = true;
            OnBuffSelectionStarted?.Invoke(this, EventArgs.Empty);
            buffManager.StartBuffSelection();
        }
        else
        {
            AdvanceToNextWave();
        }
    }

    /// <summary>
    /// Buff 面板确认后调用。只有处于等待选择状态时才推进，避免重复自增索引。
    /// </summary>
    public void ConfirmBuffSelectionAndNextWave()
    {
        if (!awaitingBuffSelection) return;

        awaitingBuffSelection = false;
        AdvanceToNextWave();
    }

    /// <summary>唯一的波次索引自增点。</summary>
    private void AdvanceToNextWave()
    {
        if (isGameOver || isVictory) return;

        currentWaveIndex++;
        StartPreparationPhase();
    }

    // ---------------------------------------------------------------- 胜负

    public void GameOver()
    {
        if (isGameOver || isVictory) return;

        isGameOver = true;
        isWaveActive = false;
        isPreparationPhase = false;
        GameTimeController.RequestPause(GameTimeController.GameOverReason);

        SetECSWaveActive(false);

        OnGameOver?.Invoke(this, EventArgs.Empty);

        if (gameOverUI != null)
        {
            gameOverUI.ShowGameOverUI("失败");
        }

        Debug.Log("[WaveManager] 游戏失败：HQ 已被摧毁。");
    }

    public void Victory()
    {
        if (isGameOver || isVictory) return;

        isVictory = true;
        isWaveActive = false;
        isPreparationPhase = false;
        GameTimeController.RequestPause(GameTimeController.GameOverReason);

        SetECSWaveActive(false);

        OnVictory?.Invoke(this, EventArgs.Empty);

        if (gameOverUI != null)
        {
            gameOverUI.ShowGameOverUI("胜利");
        }

        Debug.Log("[WaveManager] 全部波次已清除，游戏胜利！");
    }

    // ---------------------------------------------------------------- 其它

    public void StartBuffSelection()
    {
        if (buffManager != null)
        {
            awaitingBuffSelection = true;
            buffManager.StartBuffSelection();
        }
    }

    /// <summary>兼容旧调用：ECS 侧已不再驱动本方法，保留空实现避免外部引用报错。</summary>
    public void OnECSWaveStarted(int waveIndex, int totalZombies)
    {
        if (verboseLogging)
        {
            Debug.Log($"[WaveManager] ECS 已收到开波请求：wave={waveIndex}, zombies={totalZombies}");
        }
    }

    public void ResetWave()
    {
        currentWaveIndex = 0;
        isWaveActive = false;
        isPreparationPhase = false;
        isBasePlaced = false;
        awaitingBuffSelection = false;
        isGameOver = false;
        isVictory = false;
        hasGeneratedMap = false;
        zombiesKilledInCurrentWave = 0;
        waveTimer = 0f;
        preparationTimer = 0f;
        placementWaitTimer = 0f;
        GameTimeController.ClearAll();
    }

    public void OnZombieDeath()
    {
        if (!isWaveActive) return;

        // 手动模拟击杀（TestWaveSystem / WaveKillListener）时同步写回 ECS，
        // 避免下一帧轮询把计数覆盖回旧值。
        if (TryGetEntityManager(out EntityManager entityManager))
        {
            Entity waveStateEntity = GetWaveStateEntity(entityManager);
            if (waveStateEntity != Entity.Null)
            {
                WaveState waveState = entityManager.GetComponentData<WaveState>(waveStateEntity);
                waveState.zombiesKilledInCurrentWave++;
                entityManager.SetComponentData(waveStateEntity, waveState);
            }
        }

        zombiesKilledInCurrentWave++;
        OnZombieKilled?.Invoke(this, zombiesKilledInCurrentWave);
    }

    // ---------------------------------------------------------------- ECS 桥接

    private bool TryGetEntityManager(out EntityManager entityManager)
    {
        entityManager = default;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return false;

        entityManager = world.EntityManager;
        return true;
    }

    private Entity GetWaveStateEntity(EntityManager entityManager)
    {
        EntityQuery query = entityManager.CreateEntityQuery(typeof(WaveState));
        Entity result = Entity.Null;
        if (query.CalculateEntityCount() > 0)
        {
            result = query.GetSingletonEntity();
        }
        query.Dispose();
        return result;
    }

    /// <summary>请求 ECS 开始生成本波僵尸。</summary>
    private void RequestECSWaveStart(int waveIndex)
    {
        if (!TryGetEntityManager(out EntityManager entityManager)) return;

        Entity waveStateEntity = GetWaveStateEntity(entityManager);
        if (waveStateEntity == Entity.Null)
        {
            Debug.LogWarning("[WaveManager] 未找到 WaveState 实体，ECS 侧不会生成僵尸。");
            return;
        }

        Entity requestEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(requestEntity, new StartWaveEvent { waveIndex = waveIndex });
    }

    /// <summary>同步波次激活状态到 ECS，用于开关僵尸生成。</summary>
    private void SetECSWaveActive(bool active)
    {
        if (!TryGetEntityManager(out EntityManager entityManager)) return;

        Entity waveStateEntity = GetWaveStateEntity(entityManager);
        if (waveStateEntity == Entity.Null) return;

        WaveState waveState = entityManager.GetComponentData<WaveState>(waveStateEntity);
        if (waveState.isWaveActive == active) return;

        waveState.isWaveActive = active;
        if (!active)
        {
            waveState.zombiesSpawnedInCurrentWave = 0;
        }
        entityManager.SetComponentData(waveStateEntity, waveState);
    }

    /// <summary>请求 ECS 清理场上所有进攻 HQ 的敌人（波次结束回收）。</summary>
    private void RequestClearFieldEnemies()
    {
        if (!TryGetEntityManager(out EntityManager entityManager)) return;

        Entity requestEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(requestEntity, new ClearFieldEnemiesEvent());
    }
}
