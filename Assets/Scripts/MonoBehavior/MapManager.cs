using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    public event System.EventHandler OnMapGenerated;
    public event System.EventHandler OnMapClear;

    [Header("Map Configuration")]
    [SerializeField] private MapConfigSo mapConfigSo;

    [Header("Override Prefabs (leave empty to use MapConfigSo)")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private GameObject[] resourcePrefabs;
    [SerializeField] private Material[] terrainMaterials;
    [SerializeField] private Material defaultTerrainMaterial;

    [Header("Scene References")]
    [SerializeField] private Transform mapContainer;
    [SerializeField] private LayerMask obstacleLayer;

    private List<GameObject> generatedObjects = new List<GameObject>();
    private GameObject currentTerrain;
    private EntityManager entityManager;
    private bool entityManagerValid;

    public MapConfigSo MapConfigSoInstance => mapConfigSo;

    private GameObject[] EffectiveObstaclePrefabs => 
        (obstaclePrefabs != null && obstaclePrefabs.Length > 0) ? obstaclePrefabs : 
        (mapConfigSo != null ? mapConfigSo.obstaclePrefabs : null);

    private GameObject[] EffectiveResourcePrefabs => 
        (resourcePrefabs != null && resourcePrefabs.Length > 0) ? resourcePrefabs : 
        (mapConfigSo != null ? mapConfigSo.resourcePrefabs : null);

    private Material[] EffectiveTerrainMaterials => 
        (terrainMaterials != null && terrainMaterials.Length > 0) ? terrainMaterials : 
        (mapConfigSo != null ? mapConfigSo.terrainMaterials : null);

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
        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            entityManager = world.EntityManager;
            entityManagerValid = true;
        }
    }

    private void EnsureEntityManager()
    {
        if (!entityManagerValid)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                entityManager = world.EntityManager;
                entityManagerValid = true;
            }
        }
    }

    public void GenerateMap()
    {
        GenerateMap(UnityEngine.Random.Range(1, int.MaxValue));
    }

    public void GenerateMap(int seed)
    {
        ClearMap();

        MapConfig config;
        if (mapConfigSo != null)
        {
            config = mapConfigSo.mapConfig;
            Debug.Log($"[MapManager] Generating map from MapConfigSo: {config.mapWidth}x{config.mapHeight}, obstacleDensity={config.obstacleDensity}, resourceCount={config.resourceCount}, seed={seed}");
        }
        else
        {
            config = GetConfigFromECS();
            if (config.mapWidth == 0)
            {
                config = new MapConfig { mapWidth = 200, mapHeight = 200, obstacleDensity = 0.003f, resourceCount = 15 };
                Debug.LogWarning("[MapManager] No MapConfigSo and no ECS config found! Using default 200x200 map.");
            }
            else
            {
                Debug.Log($"[MapManager] Generating map from ECS config: {config.mapWidth}x{config.mapHeight}, seed={seed}");
            }
        }

        GenerateTerrain(config);
        GenerateObstacles(config, seed);
        GenerateResources(config, seed);

        OnMapGenerated?.Invoke(this, System.EventArgs.Empty);

        UpdateECSMapConfig(config);

        StartCoroutine(NotifyECSAfterPhysicsSync());
    }

    private MapConfig GetConfigFromECS()
    {
        EnsureEntityManager();
        if (!entityManagerValid) return default;

        var query = entityManager.CreateEntityQuery(typeof(MapConfigData));
        if (query.CalculateEntityCount() > 0)
        {
            var mapConfig = query.GetSingleton<MapConfigData>();
            query.Dispose();
            return new MapConfig
            {
                mapWidth = mapConfig.mapWidth,
                mapHeight = mapConfig.mapHeight,
                obstacleDensity = mapConfig.obstacleDensity,
                resourceCount = mapConfig.resourceCount,
                basePlacementRadius = mapConfig.basePlacementRadius,
                terrainType = TerrainType.Grassland,
            };
        }
        query.Dispose();
        return default;
    }

    private void UpdateECSMapConfig(MapConfig config)
    {
        EnsureEntityManager();
        if (!entityManagerValid) return;

        var query = entityManager.CreateEntityQuery(typeof(MapConfigData));
        if (query.CalculateEntityCount() > 0)
        {
            var mapConfig = query.GetSingleton<MapConfigData>();
            mapConfig.mapWidth = config.mapWidth;
            mapConfig.mapHeight = config.mapHeight;
            mapConfig.obstacleDensity = config.obstacleDensity;
            mapConfig.resourceCount = config.resourceCount;
            mapConfig.isGenerated = true;
            entityManager.SetComponentData(query.GetSingletonEntity(), mapConfig);
        }
        query.Dispose();
    }

    private IEnumerator NotifyECSAfterPhysicsSync()
    {
        yield return new WaitForFixedUpdate();

        EnsureEntityManager();
        if (!entityManagerValid) yield break;

        var query = entityManager.CreateEntityQuery(typeof(MapConfigData));
        if (query.CalculateEntityCount() > 0)
        {
            var mapConfig = query.GetSingleton<MapConfigData>();
            mapConfig.needRecalculateCostMap = true;
            entityManager.SetComponentData(query.GetSingletonEntity(), mapConfig);
        }
        query.Dispose();
    }

    private void GenerateTerrain(MapConfig config)
    {
        if (mapContainer != null)
        {
            GameObject terrain = mapContainer.Find("Terrain")?.gameObject;
            if (terrain != null)
            {
                Destroy(terrain);
            }

            terrain = new GameObject("Terrain");
            terrain.transform.parent = mapContainer;
            terrain.transform.position = Vector3.zero;
            terrain.transform.rotation = Quaternion.Euler(180, 0, 0);

            MeshRenderer meshRenderer = terrain.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = terrain.AddComponent<MeshFilter>();
            BoxCollider boxCollider = terrain.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(config.mapWidth, 0.1f, config.mapHeight);

            Mesh mesh = new Mesh();
            float halfWidth = config.mapWidth / 2f;
            float halfHeight = config.mapHeight / 2f;

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-halfWidth, 0, -halfHeight),
                new Vector3(halfWidth, 0, -halfHeight),
                new Vector3(-halfWidth, 0, halfHeight),
                new Vector3(halfWidth, 0, halfHeight)
            };

            int[] triangles = new int[6] { 0, 1, 2, 2, 1, 3 };
            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            var mats = EffectiveTerrainMaterials;
            if (mats != null && mats.Length > 0)
            {
                int materialIndex = Random.Range(0, mats.Length);
                meshRenderer.material = mats[materialIndex];
            }
            else
            {
                meshRenderer.material = defaultTerrainMaterial != null ? defaultTerrainMaterial : new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.4f, 0.6f, 0.3f)
                };
            }

            currentTerrain = terrain;
            generatedObjects.Add(terrain);
        }
    }

    private void GenerateObstacles(MapConfig config, int seed)
    {
        var prefabs = EffectiveObstaclePrefabs;
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[MapManager] No obstacle prefabs assigned!");
            return;
        }

        Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)seed);
        int obstacleCount = Mathf.RoundToInt(config.mapWidth * config.mapHeight * config.obstacleDensity);
        float halfWidth = config.mapWidth / 2f;
        float halfHeight = config.mapHeight / 2f;

        float minDistance = 2f;
        List<Vector3> spawnedPositions = new List<Vector3>();
        int maxAttempts = obstacleCount * 5;
        int obstaclesSpawned = 0;

        for (int i = 0; i < maxAttempts && obstaclesSpawned < obstacleCount; i++)
        {
            float x = random.NextFloat(-halfWidth, halfWidth);
            float z = random.NextFloat(-halfHeight, halfHeight);
            Vector3 position = new Vector3(x, 0, z);

            if (!IsPositionValid(position, spawnedPositions, minDistance, halfWidth, halfHeight))
            {
                continue;
            }

            GameObject obstaclePrefab = prefabs[Random.Range(0, prefabs.Length)];
            GameObject obstacle = Instantiate(obstaclePrefab, position, Quaternion.Euler(0, random.NextFloat(0, 360f), 0), mapContainer);

            obstacle.layer = GameAssets.PATHFINDING_WALL;
            foreach (Transform child in obstacle.transform)
            {
                child.gameObject.layer = GameAssets.PATHFINDING_WALL;
            }

            generatedObjects.Add(obstacle);
            spawnedPositions.Add(position);
            obstaclesSpawned++;
        }
    }

    private void GenerateResources(MapConfig config, int seed)
    {
        var prefabs = EffectiveResourcePrefabs;
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[MapManager] No resource prefabs assigned!");
            return;
        }

        Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)(seed + 12345));
        float halfWidth = config.mapWidth / 2f;
        float halfHeight = config.mapHeight / 2f;

        float minDistance = 2.5f;
        List<Vector3> spawnedPositions = new List<Vector3>();
        int maxAttempts = config.resourceCount * 5;
        int resourcesSpawned = 0;

        for (int i = 0; i < maxAttempts && resourcesSpawned < config.resourceCount; i++)
        {
            float x = random.NextFloat(-halfWidth, halfWidth);
            float z = random.NextFloat(-halfHeight, halfHeight);
            Vector3 position = new Vector3(x, 0, z);

            if (!IsPositionValid(position, spawnedPositions, minDistance, halfWidth, halfHeight))
            {
                continue;
            }

            GameObject resourcePrefab = prefabs[Random.Range(0, prefabs.Length)];
            GameObject resource = Instantiate(resourcePrefab, position, Quaternion.identity, mapContainer);

            resource.layer = GameAssets.PATHFINDING_HEAVY;
            foreach (Transform child in resource.transform)
            {
                child.gameObject.layer = GameAssets.PATHFINDING_HEAVY;
            }

            generatedObjects.Add(resource);
            spawnedPositions.Add(position);
            resourcesSpawned++;
        }
    }

    private bool IsPositionValid(Vector3 position, List<Vector3> existingPositions, float minDistance, float halfWidth, float halfHeight)
    {
        float margin = 3f;
        if (Mathf.Abs(position.x) > halfWidth - margin || Mathf.Abs(position.z) > halfHeight - margin)
        {
            return false;
        }

        for (int i = 0; i < existingPositions.Count; i++)
        {
            if (Vector3.Distance(position, existingPositions[i]) < minDistance)
            {
                return false;
            }
        }

        Collider[] colliders = Physics.OverlapSphere(position, 1f, obstacleLayer);
        if (colliders.Length > 0)
        {
            return false;
        }

        return true;
    }

    public void ClearMap()
    {
        foreach (GameObject obj in generatedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        generatedObjects.Clear();
        currentTerrain = null;

        if (BasePlacementSystem.Instance != null)
        {
            BasePlacementSystem.Instance.RemoveBase();
        }

        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            var em = world.EntityManager;
            var runtimeQuery = em.CreateEntityQuery(typeof(RuntimeEntity));
            var runtimeEntities = runtimeQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            em.DestroyEntity(runtimeEntities);
            runtimeEntities.Dispose();
        }

        EnsureEntityManager();
        if (entityManagerValid)
        {
            var mapConfigQuery = entityManager.CreateEntityQuery(typeof(MapConfigData));
            if (mapConfigQuery.CalculateEntityCount() > 0)
            {
                var mapConfig = mapConfigQuery.GetSingleton<MapConfigData>();
                mapConfig.isGenerated = false;
                entityManager.SetComponentData(mapConfigQuery.GetSingletonEntity(), mapConfig);
            }
            mapConfigQuery.Dispose();
        }

        OnMapClear?.Invoke(this, System.EventArgs.Empty);
        Debug.Log("[MapManager] Map cleared");
    }

    public Bounds GetMapBounds()
    {
        MapConfig config;
        if (mapConfigSo != null)
        {
            config = mapConfigSo.mapConfig;
        }
        else
        {
            config = GetConfigFromECS();
            if (config.mapWidth == 0)
            {
                config = new MapConfig { mapWidth = 200, mapHeight = 200 };
            }
        }
        return new Bounds(Vector3.zero, new Vector3(config.mapWidth, 10, config.mapHeight));
    }
}