using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 补全计划的场景/资产修补工具。
///
/// 用途：把原本需要手动在 Inspector 里改的序列化字段一次性写入 GameScene。
/// 可用两种方式触发：
///   1. Unity 菜单：Tools/DotRts/应用补全计划场景修补
///   2. 命令行：Unity.exe -batchmode -quit -projectPath &lt;项目&gt; -executeMethod DotRtsScenePatch.Apply
/// </summary>
public static class DotRtsScenePatch
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string WaveDataAssetPath = "Assets/ScriptsableObjects/WaveSo/WaveData.asset";

    private static readonly int ObstacleMask =
        (1 << GameAssets.PATHFINDING_WALL) | (1 << GameAssets.PATHFINDING_HEAVY);

    [MenuItem("Tools/DotRts/Diagnose Scene")]
    public static void Diagnose()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("=== DotRts 场景配置检查 ===");

        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        MapManager mapManager = Object.FindFirstObjectByType<MapManager>(FindObjectsInactive.Include);
        BasePlacementSystem basePlacement = Object.FindFirstObjectByType<BasePlacementSystem>(FindObjectsInactive.Include);
        WaveManager waveManager = Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
        TestWaveSystem testWave = Object.FindFirstObjectByType<TestWaveSystem>(FindObjectsInactive.Include);
        GameOverUI gameOverUI = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
        GameOverUi gameOverUi = Object.FindFirstObjectByType<GameOverUi>(FindObjectsInactive.Include);

        report.AppendLine($"MapManager: {(mapManager != null ? mapManager.name : "缺失")}");
        report.AppendLine($"BasePlacementSystem: {(basePlacement != null ? basePlacement.name : "缺失")}");
        report.AppendLine($"WaveManager: {(waveManager != null ? waveManager.name : "缺失")}");
        report.AppendLine($"TestWaveSystem: {(testWave != null ? testWave.name : "未挂载")}");
        report.AppendLine($"GameOverUI: {(gameOverUI != null ? gameOverUI.name : "缺失")}");
        report.AppendLine($"GameOverUi(兼容壳): {(gameOverUi != null ? gameOverUi.name : "未挂载")}");
        report.AppendLine(scene.IsValid() ? $"当前场景: {scene.name}" : "场景未打开");

        Debug.Log(report.ToString());
    }

    /// <summary>菜单入口。</summary>
    [MenuItem("Tools/DotRts/Apply Scene Patch")]
    public static void ApplyFromMenu()
    {
        Apply();
    }

    /// <summary>命令行入口。</summary>
    public static void Apply()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("=== DotRts 场景修补开始 ===");

        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        PatchMapManager(report);
        PatchBasePlacement(report);
        PatchWaveManager(report);
        PatchTestWaveSystem(report);
        PatchGameOverUi(report);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        report.AppendLine(saved ? "场景已保存。" : "场景保存失败！");
        report.AppendLine("=== DotRts 场景修补结束 ===");

        Debug.Log(report.ToString());

        if (Application.isBatchMode)
        {
            AssetDatabase.SaveAssets();
        }
    }

    private static void PatchMapManager(StringBuilder report)
    {
        MapManager mapManager = Object.FindFirstObjectByType<MapManager>(FindObjectsInactive.Include);
        if (mapManager == null)
        {
            report.AppendLine("! 场景中缺少 MapManager，跳过。");
            return;
        }

        SetProperty(mapManager, "obstacleLayer", property => property.intValue = ObstacleMask, report);

        MapConfigSo configSo = AssetDatabase.LoadAssetAtPath<MapConfigSo>("Assets/ScriptsableObjects/MAP/New Map Config So.asset");
        if (configSo != null)
        {
            SetProperty(mapManager, "mapConfigSo", property => property.objectReferenceValue = configSo, report);
        }
    }

    private static void PatchBasePlacement(StringBuilder report)
    {
        BasePlacementSystem basePlacement = Object.FindFirstObjectByType<BasePlacementSystem>(FindObjectsInactive.Include);
        if (basePlacement == null)
        {
            report.AppendLine("! 场景中缺少 BasePlacementSystem，跳过。");
            return;
        }

        // 地面是代码生成的 Terrain，位于 Default(0) 层。
        SetProperty(basePlacement, "groundLayer", property => property.intValue = 1 << 0, report);
        SetProperty(basePlacement, "obstacleLayer", property => property.intValue = ObstacleMask, report);
    }

    private static void PatchWaveManager(StringBuilder report)
    {
        WaveManager waveManager = Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
        if (waveManager == null)
        {
            report.AppendLine("! 场景中缺少 WaveManager，跳过。");
            return;
        }

        WaveDataSo waveData = AssetDatabase.LoadAssetAtPath<WaveDataSo>(WaveDataAssetPath);
        if (waveData != null)
        {
            SetProperty(waveManager, "waveDataSo", property => property.objectReferenceValue = waveData, report);
        }
        else
        {
            report.AppendLine($"! 未找到 {WaveDataAssetPath}");
        }

        BasePlacementSystem basePlacement = Object.FindFirstObjectByType<BasePlacementSystem>(FindObjectsInactive.Include);
        if (basePlacement != null)
        {
            SetProperty(waveManager, "basePlacementSystem", property => property.objectReferenceValue = basePlacement, report);
        }

        GameOverUI gameOverUI = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
        if (gameOverUI != null)
        {
            SetProperty(waveManager, "gameOverUI", property => property.objectReferenceValue = gameOverUI, report);
        }

        BuffManager buffManager = Object.FindFirstObjectByType<BuffManager>(FindObjectsInactive.Include);
        if (buffManager != null)
        {
            SetProperty(waveManager, "buffManager", property => property.objectReferenceValue = buffManager, report);
        }
    }

    private static void PatchTestWaveSystem(StringBuilder report)
    {
        TestWaveSystem testWave = Object.FindFirstObjectByType<TestWaveSystem>(FindObjectsInactive.Include);
        if (testWave == null)
        {
            report.AppendLine("- TestWaveSystem 未挂载，无需处理。");
            return;
        }

        SetProperty(testWave, "enableTestMode", property => property.boolValue = false, report);
    }

    private static void PatchGameOverUi(StringBuilder report)
    {
        GameOverUI panel = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
        GameOverUi shell = Object.FindFirstObjectByType<GameOverUi>(FindObjectsInactive.Include);

        if (panel == null)
        {
            report.AppendLine("! 场景中没有 GameOverUI 面板，胜负界面不会显示。");
            return;
        }

        if (shell == null)
        {
            report.AppendLine("- 没有旧版 GameOverUi 兼容壳，跳过。");
            return;
        }

        // 旧壳只负责把按钮引用转交给 GameOverUI，保留挂载即可；
        // 如果它指向的按钮为空，补上面板上的按钮，避免点击无响应。
        SetProperty(shell, "mainMenuButton",
            property =>
            {
                if (property.objectReferenceValue == null && panel.mainMenuButton != null)
                {
                    property.objectReferenceValue = panel.mainMenuButton;
                }
            }, report);

        SetProperty(shell, "restartButton",
            property =>
            {
                if (property.objectReferenceValue == null && panel.restartButton != null)
                {
                    property.objectReferenceValue = panel.restartButton;
                }
            }, report);
    }

    private static void SetProperty(Object target, string propertyName,
        System.Action<SerializedProperty> setter, StringBuilder report)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            report.AppendLine($"  ! 未找到字段 {target.GetType().Name}.{propertyName}");
            return;
        }

        setter(property);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        report.AppendLine($"  ✓ {target.GetType().Name}.{propertyName}");
    }
}
