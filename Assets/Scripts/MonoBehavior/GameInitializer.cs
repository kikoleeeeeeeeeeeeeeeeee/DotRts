using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Unity.Collections;
using Unity.Entities;


public class GameInitializer : MonoBehaviour
{
    [Header("初始化设置")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float initializationDelay = 0.5f;

    private void Start()
    {
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        // 等待一小段时间，确保所有 GameObject 都已加载
        yield return new WaitForSeconds(initializationDelay);

        // 初始化 ECS 相关的 EntitiesReferences
        InitializeEntitiesReferences();
        yield return null;

        // 初始化 WaveManager
        InitializeWaveManager();
        yield return null;

        // 初始化 BuffManager
        InitializeBuffManager();
        yield return null;

        // 初始化 BuffSelectionUI
        InitializeBuffSelectionUI();
        yield return null;

        // 初始化 BasePlacementSystem
        InitializeBasePlacementSystem();
        yield return null;

        // 初始化 MapManager
        InitializeMapManager();
        yield return null;
    }

    private void InitializeEntitiesReferences()
    {
        UnityEngine.Debug.Log("[GameInitializer] 初始化 EntitiesReferences...");
        
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            UnityEngine.Debug.LogError("[GameInitializer] ECS World 不存在！");
            return;
        }
        
        var entityManager = world.EntityManager;
        
        // 等待一帧让 ECS 完成烘焙
        var query = entityManager.CreateEntityQuery(typeof(EntitiesReferences));
        if (query.CalculateEntityCount() == 0)
        {
            UnityEngine.Debug.LogWarning("[GameInitializer] 未找到烘焙的 EntitiesReferences，等待烘焙...");
            return;
        }
        
        var entitiesRefEntity = query.GetSingletonEntity();
        var entitiesReferences = entityManager.GetComponentData<EntitiesReferences>(entitiesRefEntity);
        
        UnityEngine.Debug.Log($"[GameInitializer] EntitiesReferences 初始化完成，zombiePrefabEntity: {entitiesReferences.zombiePrefabEntity.Index}");
        
        if (entitiesReferences.zombiePrefabEntity == Entity.Null)
        {
            UnityEngine.Debug.LogError("[GameInitializer] zombiePrefabEntity 为 Null！");
        }
    }
    
    private Entity GetPrefabEntity(GameObject prefabGO, EntityManager entityManager)
    {
        if (prefabGO == null)
        {
            UnityEngine.Debug.LogError("[GetPrefabEntity] prefabGO 为 null!");
            return Entity.Null;
        }
        
        var prefabQuery = entityManager.CreateEntityQuery(typeof(Unity.Entities.PrefabRoot), typeof(Unity.Entities.Prefab));
        var prefabEntities = prefabQuery.ToEntityArray(Allocator.Temp);
        
        // 遍历所有 PrefabRoot，通过 GameObject 来匹配
        foreach (var prefabEntity in prefabEntities)
        {
            try
            {
                var go = entityManager.GetComponentObject<UnityEngine.GameObject>(prefabEntity);
                if (go != null && go == prefabGO)
                {
                    return prefabEntity;
                }
            }
            catch
            {
                continue;
            }
        }
        
        // 通过名称匹配
        foreach (var prefabEntity in prefabEntities)
        {
            try
            {
                var go = entityManager.GetComponentObject<UnityEngine.GameObject>(prefabEntity);
                if (go != null && go.name == prefabGO.name)
                {
                    return prefabEntity;
                }
            }
            catch
            {
                continue;
            }
        }
        
        UnityEngine.Debug.LogWarning($"[GetPrefabEntity] 未找到预制体 {prefabGO.name} 的 Entity!");
        prefabEntities.Dispose();
        return Entity.Null;
    }

    private void InitializeWaveManager()
    {
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager == null)
        {
            GameObject waveManagerObj = new GameObject("WaveManager");
            waveManager = waveManagerObj.AddComponent<WaveManager>();
        }
    }

    private void InitializeBuffManager()
    {
        BuffManager buffManager = FindObjectOfType<BuffManager>();
        if (buffManager == null)
        {
            GameObject buffManagerObj = new GameObject("BuffManager");
            buffManager = buffManagerObj.AddComponent<BuffManager>();
        }
    }

    private void InitializeBuffSelectionUI()
    {
        BuffSelectionUI buffSelectionUI = FindObjectOfType<BuffSelectionUI>();
        if (buffSelectionUI == null)
        {
            GameObject uiCanvas = FindObjectOfType<Canvas>()?.gameObject;
            if (uiCanvas == null)
            {
                uiCanvas = new GameObject("Canvas");
                uiCanvas.AddComponent<Canvas>();
                uiCanvas.AddComponent<CanvasScaler>();
                uiCanvas.AddComponent<GraphicRaycaster>();
                uiCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            }

            GameObject buffSelectionObj = new GameObject("BuffSelectionUI");
            buffSelectionObj.transform.SetParent(uiCanvas.transform);
            buffSelectionUI = buffSelectionObj.AddComponent<BuffSelectionUI>();

            // 创建基本的 UI 组件
            CreateBuffSelectionUIComponents(buffSelectionObj);
        }
    }

    private void CreateBuffSelectionUIComponents(GameObject parent)
    {
        // 创建 buffPanel
        GameObject buffPanel = new GameObject("BuffPanel");
        buffPanel.transform.SetParent(parent.transform);
        buffPanel.AddComponent<RectTransform>();
        buffPanel.AddComponent<CanvasRenderer>();
        buffPanel.AddComponent<Image>();
        buffPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);

        RectTransform panelRect = buffPanel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(800, 600);
        panelRect.anchoredPosition = Vector2.zero;

        // 创建标题文本
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(buffPanel.transform);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "选择加成";
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 36;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(800, 50);
        titleRect.anchoredPosition = new Vector2(0, 250);

        // 创建三个按钮
        for (int i = 0; i < 3; i++)
        {
            GameObject buttonObj = new GameObject($"BuffButton{i+1}");
            buttonObj.transform.SetParent(buffPanel.transform);
            Button button = buttonObj.AddComponent<Button>();
            buttonObj.AddComponent<CanvasRenderer>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.4f, 0.6f);

            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(700, 120);
            buttonRect.anchoredPosition = new Vector2(0, 100 - i * 150);

            // 创建按钮文本
            GameObject buttonTextObj = new GameObject($"ButtonText{i+1}");
            buttonTextObj.transform.SetParent(buttonObj.transform);
            Text buttonText = buttonTextObj.AddComponent<Text>();
            buttonText.text = $"加成选项 {i+1}";
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 18;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = buttonTextObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(700, 120);
            textRect.anchoredPosition = Vector2.zero;
        }

        buffPanel.SetActive(false);
    }

    private void InitializeBasePlacementSystem()
    {
        BasePlacementSystem basePlacementSystem = FindObjectOfType<BasePlacementSystem>();
        if (basePlacementSystem == null)
        {
            GameObject basePlacementObj = new GameObject("BasePlacementSystem");
            basePlacementSystem = basePlacementObj.AddComponent<BasePlacementSystem>();
        }
    }

    private void InitializeMapManager()
    {
        MapManager mapManager = FindObjectOfType<MapManager>();
        if (mapManager == null)
        {
            GameObject mapManagerObj = new GameObject("MapManager");
            mapManager = mapManagerObj.AddComponent<MapManager>();
        }
    }

    private void InitializeGameOverUI()
    {
        GameOverUI gameOverUI = FindObjectOfType<GameOverUI>();
        if (gameOverUI == null)
        {
            GameObject uiCanvas = FindObjectOfType<Canvas>()?.gameObject;
            if (uiCanvas == null)
            {
                uiCanvas = new GameObject("Canvas");
                uiCanvas.AddComponent<Canvas>();
                uiCanvas.AddComponent<CanvasScaler>();
                uiCanvas.AddComponent<GraphicRaycaster>();
                uiCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            }

            GameObject gameOverObj = new GameObject("GameOverUI");
            gameOverObj.transform.SetParent(uiCanvas.transform);
            gameOverUI = gameOverObj.AddComponent<GameOverUI>();

            // 创建基本的 UI 组件
            CreateGameOverUIComponents(gameOverObj);
        }
    }

    private void CreateGameOverUIComponents(GameObject parent)
    {
        GameOverUI gameOverUI = parent.GetComponent<GameOverUI>();
        
        // 创建游戏结束面板
        GameObject gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(parent.transform);
        gameOverPanel.AddComponent<RectTransform>();
        gameOverPanel.AddComponent<CanvasRenderer>();
        gameOverPanel.AddComponent<Image>();
        gameOverPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);

        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(800, 600);
        panelRect.anchoredPosition = Vector2.zero;

        // 设置面板引用
        gameOverUI.gameOverPanel = gameOverPanel;

        // 创建标题文本
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(gameOverPanel.transform);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "游戏结束";
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 48;
        titleText.color = Color.red;
        titleText.alignment = TextAnchor.MiddleCenter;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(800, 100);
        titleRect.anchoredPosition = new Vector2(0, 200);

        // 创建主菜单按钮
        GameObject mainMenuButtonObj = new GameObject("MainMenuButton");
        mainMenuButtonObj.transform.SetParent(gameOverPanel.transform);
        Button mainMenuButton = mainMenuButtonObj.AddComponent<Button>();
        mainMenuButtonObj.AddComponent<CanvasRenderer>();
        Image mainMenuButtonImage = mainMenuButtonObj.AddComponent<Image>();
        mainMenuButtonImage.color = new Color(0.6f, 0.2f, 0.2f);

        RectTransform mainMenuButtonRect = mainMenuButtonObj.GetComponent<RectTransform>();
        mainMenuButtonRect.sizeDelta = new Vector2(300, 80);
        mainMenuButtonRect.anchoredPosition = new Vector2(-175, 0);

        // 设置主菜单按钮引用
        gameOverUI.mainMenuButton = mainMenuButton;

        // 创建主菜单按钮文本
        GameObject mainMenuButtonTextObj = new GameObject("MainMenuButtonText");
        mainMenuButtonTextObj.transform.SetParent(mainMenuButtonObj.transform);
        Text mainMenuButtonText = mainMenuButtonTextObj.AddComponent<Text>();
        mainMenuButtonText.text = "主菜单";
        mainMenuButtonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        mainMenuButtonText.fontSize = 24;
        mainMenuButtonText.color = Color.white;
        mainMenuButtonText.alignment = TextAnchor.MiddleCenter;

        RectTransform mainMenuButtonTextRect = mainMenuButtonTextObj.GetComponent<RectTransform>();
        mainMenuButtonTextRect.sizeDelta = new Vector2(300, 80);
        mainMenuButtonTextRect.anchoredPosition = Vector2.zero;

        // 创建重新开始按钮
        GameObject restartButtonObj = new GameObject("RestartButton");
        restartButtonObj.transform.SetParent(gameOverPanel.transform);
        Button restartButton = restartButtonObj.AddComponent<Button>();
        restartButtonObj.AddComponent<CanvasRenderer>();
        Image restartButtonImage = restartButtonObj.AddComponent<Image>();
        restartButtonImage.color = new Color(0.2f, 0.6f, 0.2f);

        RectTransform restartButtonRect = restartButtonObj.GetComponent<RectTransform>();
        restartButtonRect.sizeDelta = new Vector2(300, 80);
        restartButtonRect.anchoredPosition = new Vector2(175, 0);

        // 设置重新开始按钮引用
        gameOverUI.restartButton = restartButton;

        // 创建重新开始按钮文本
        GameObject restartButtonTextObj = new GameObject("RestartButtonText");
        restartButtonTextObj.transform.SetParent(restartButtonObj.transform);
        Text restartButtonText = restartButtonTextObj.AddComponent<Text>();
        restartButtonText.text = "重新开始";
        restartButtonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        restartButtonText.fontSize = 24;
        restartButtonText.color = Color.white;
        restartButtonText.alignment = TextAnchor.MiddleCenter;

        RectTransform restartButtonTextRect = restartButtonTextObj.GetComponent<RectTransform>();
        restartButtonTextRect.sizeDelta = new Vector2(300, 80);
        restartButtonTextRect.anchoredPosition = Vector2.zero;

        gameOverPanel.SetActive(false);
    }


}
