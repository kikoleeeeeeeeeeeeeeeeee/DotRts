using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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

    [Header("Wave Settings")]
    [SerializeField] private WaveDataSo waveDataSo;
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private BasePlacementSystem basePlacementSystem;
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private BuffManager buffManager;

    [Header("Current Wave State")]
    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private bool isWaveActive = false;
    [SerializeField] private bool isPreparationPhase = false;
    [SerializeField] private bool isBasePlaced = false;
    [SerializeField] private int zombiesKilledInCurrentWave = 0;
    [SerializeField] private float waveTimer = 0f;
    [SerializeField] private float preparationTimer = 0f;
    [SerializeField] private bool isInitialized = false;

    public int CurrentWaveIndex => currentWaveIndex;
    public bool IsWaveActive => isWaveActive;
    public bool IsPreparationPhase => isPreparationPhase;
    public bool IsBasePlaced => isBasePlaced;
    public WaveData CurrentWaveData
    {
        get
        {
            if (currentWaveIndex >= waveDataSo.waves.Length || currentWaveIndex < 0)
            {
                return waveDataSo.waves[waveDataSo.waves.Length - 1];
            }
            return waveDataSo.waves[currentWaveIndex];
        }
    }
    public int ZombiesKilledInCurrentWave => zombiesKilledInCurrentWave;
    public float WaveTimer => waveTimer;
    public float PreparationTimer => preparationTimer;
    public bool IsInitialized => isInitialized;

    private EntityManager entityManager;
    private bool entityManagerValid;
    private Entity zombiePrefabEntity;

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

        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            entityManager = world.EntityManager;
            entityManagerValid = true;
        }
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        Invoke(nameof(InitializeZombiePrefabEntity), 0.1f);

        if (BuffManager.Instance == null)
        {
            GameObject buffManagerObj = new GameObject("BuffManager");
            buffManagerObj.AddComponent<BuffManager>();
        }

        if (waveDataSo == null)
        {
            waveDataSo = ScriptableObject.CreateInstance<WaveDataSo>();
            waveDataSo.globalPreparationTime = 5f;
            waveDataSo.baseSpawnInterval = 1f;
            waveDataSo.spawnIntervalReductionPerWave = 0.05f;
            waveDataSo.waves = new WaveData[1];
            waveDataSo.waves[0] = new WaveData
            {
                waveNumber = 1,
                waveDuration = Mathf.Infinity,
                preparationTime = 5f,
                baseZombieCount = 10,
                additionalZombiePerWave = 5,
                spawnInterval = 1f,
                zombieHealthMultiplier = 1f,
                zombieDamageMultiplier = 1f,
                isSpecialWave = false,
                specialWaveType = global::SpecialWaveType.Normal
            };
        }

        if (BasePlacementSystem.Instance != null)
        {
            BasePlacementSystem.Instance.OnBasePlaced += BasePlacementSystem_OnBasePlaced;
        }

        isInitialized = true;
        StartPreparationPhase();
    }

    private void InitializeZombiePrefabEntity()
    {
        if (zombiePrefab == null)
        {
            UnityEngine.Debug.LogError("[WaveManager] zombiePrefab not assigned!");
            return;
        }

        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(EntitiesReferences));
            if (query.CalculateEntityCount() > 0)
            {
                Entity entitiesRefEntity = query.GetSingletonEntity();
                EntitiesReferences entitiesReferences = em.GetComponentData<EntitiesReferences>(entitiesRefEntity);
                zombiePrefabEntity = entitiesReferences.zombiePrefabEntity;
            }
            query.Dispose();
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (isPreparationPhase && isBasePlaced)
        {
            UpdatePreparationTimer();
        }
        else if (isWaveActive)
        {
            UpdateWaveTimer();
        }
    }

    private void UpdatePreparationTimer()
    {
        preparationTimer -= Time.deltaTime;
        if (preparationTimer <= 0f)
        {
            EndPreparationPhase();
        }
    }

    private void BasePlacementSystem_OnBasePlaced(object sender, EventArgs e)
    {
        isBasePlaced = true;
    }

    private void UpdateWaveTimer()
    {
        waveTimer -= Time.deltaTime;
        OnWaveTimerUpdated?.Invoke(this, waveTimer);
        if (waveTimer <= 0f)
        {
            EndCurrentWave();
        }
    }

    public void StartPreparationPhase()
    {
        if (currentWaveIndex >= waveDataSo.waves.Length)
        {
            Debug.Log($"[WaveManager] All waves completed (wave {currentWaveIndex} >= max {waveDataSo.waves.Length}). Game Over!");
            OnGameOver?.Invoke(this, EventArgs.Empty);
            if (gameOverUI != null) gameOverUI.gameObject.SetActive(true);
            return;
        }

        if (MapManager.Instance != null)
        {
            int seed = UnityEngine.Random.Range(1, int.MaxValue);
            MapManager.Instance.GenerateMap(seed);
            OnMapGenerated?.Invoke(this, EventArgs.Empty);
            Debug.Log($"[WaveManager] Map generated with seed {seed}");
        }
        else
        {
            Debug.LogError("[WaveManager] MapManager.Instance is null! Map will not be generated.");
        }

        if (basePlacementSystem != null)
        {
            basePlacementSystem.StartBasePlacement();
            OnBasePlacementStarted?.Invoke(this, EventArgs.Empty);
        }
        else if (BasePlacementSystem.Instance != null)
        {
            BasePlacementSystem.Instance.StartBasePlacement();
            OnBasePlacementStarted?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Debug.LogError("[WaveManager] No BasePlacementSystem found! Cannot start base placement.");
        }

        preparationTimer = waveDataSo.globalPreparationTime;
        isPreparationPhase = true;
        isWaveActive = false;
        isBasePlaced = false;
    }

    private void EndPreparationPhase()
    {
        isPreparationPhase = false;
        OnWaveStarted?.Invoke(this, EventArgs.Empty);
    }

    public void OnECSWaveStarted(int waveIndex, int totalZombies)
    {
        isWaveActive = true;
        zombiesKilledInCurrentWave = 0;

        if (waveIndex < waveDataSo.waves.Length)
        {
            WaveData waveData = waveDataSo.waves[waveIndex];
            waveTimer = waveData.waveDuration;
        }

        OnWaveStarted?.Invoke(this, EventArgs.Empty);
    }

    public void EndCurrentWave()
    {
        isWaveActive = false;
        OnWaveEnded?.Invoke(this, EventArgs.Empty);

        if (currentWaveIndex + 1 >= waveDataSo.waves.Length)
        {
            GameOver();
        }
        else
        {
            currentWaveIndex++;
            isPreparationPhase = true;
            preparationTimer = waveDataSo.globalPreparationTime;

            if (BuffManager.Instance != null)
            {
                BuffManager.Instance.StartBuffSelection();
            }
        }
    }

    public void StartBuffSelection() { }

    public void ConfirmBuffSelectionAndNextWave()
    {
        currentWaveIndex++;
        StartPreparationPhase();
    }

    public void ResetWave()
    {
        currentWaveIndex = 0;
        isWaveActive = false;
        isPreparationPhase = false;
        zombiesKilledInCurrentWave = 0;
        waveTimer = 0f;
        preparationTimer = 0f;
    }

    public void OnZombieDeath()
    {
        if (!isWaveActive) return;
        zombiesKilledInCurrentWave++;
        OnZombieKilled?.Invoke(this, zombiesKilledInCurrentWave);
    }

    public void GameOver()
    {
        OnGameOver?.Invoke(this, EventArgs.Empty);
        if (gameOverUI != null) gameOverUI.gameObject.SetActive(true);
    }
}