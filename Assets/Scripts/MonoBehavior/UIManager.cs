using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private void Awake()
    {
        // 检查是否已经存在GameOverUI
        if (FindObjectOfType<GameOverUI>() == null)
        {
            // 创建Canvas
            GameObject canvas = new GameObject("GameOverCanvas");
            canvas.AddComponent<Canvas>();
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 设置Canvas参数
            Canvas canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;

            // 创建游戏结束面板
            GameObject gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(canvas.transform);

            // 设置面板大小和位置
            RectTransform panelRect = gameOverPanel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400, 300);
            panelRect.anchoredPosition = Vector2.zero;

            // 添加背景图片
            UnityEngine.UI.Image backgroundImage = gameOverPanel.AddComponent<UnityEngine.UI.Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.8f);

            // 添加标题文本
            GameObject titleText = new GameObject("TitleText");
            titleText.transform.SetParent(gameOverPanel.transform);
            UnityEngine.UI.Text titleTextComponent = titleText.AddComponent<UnityEngine.UI.Text>();
            titleTextComponent.text = "游戏结束";
            titleTextComponent.fontSize = 36;
            titleTextComponent.color = Color.white;
            titleTextComponent.alignment = TextAnchor.MiddleCenter;

            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(300, 50);
            titleRect.anchoredPosition = new Vector2(0, 80);

            // 创建重新开始按钮
            GameObject restartButton = new GameObject("RestartButton");
            restartButton.transform.SetParent(gameOverPanel.transform);
            UnityEngine.UI.Button restartButtonComponent = restartButton.AddComponent<UnityEngine.UI.Button>();

            // 添加按钮背景
            UnityEngine.UI.Image restartButtonImage = restartButton.AddComponent<UnityEngine.UI.Image>();
            restartButtonImage.color = new Color(0.2f, 0.6f, 0.2f);

            // 添加按钮文本
            GameObject restartButtonText = new GameObject("Text");
            restartButtonText.transform.SetParent(restartButton.transform);
            UnityEngine.UI.Text restartButtonTextComponent = restartButtonText.AddComponent<UnityEngine.UI.Text>();
            restartButtonTextComponent.text = "重新开始游戏";
            restartButtonTextComponent.fontSize = 20;
            restartButtonTextComponent.color = Color.white;
            restartButtonTextComponent.alignment = TextAnchor.MiddleCenter;

            RectTransform restartButtonRect = restartButton.GetComponent<RectTransform>();
            restartButtonRect.sizeDelta = new Vector2(200, 50);
            restartButtonRect.anchoredPosition = new Vector2(0, 20);

            RectTransform restartButtonTextRect = restartButtonText.GetComponent<RectTransform>();
            restartButtonTextRect.sizeDelta = new Vector2(180, 30);
            restartButtonTextRect.anchoredPosition = Vector2.zero;

            // 创建返回主菜单按钮
            GameObject mainMenuButton = new GameObject("MainMenuButton");
            mainMenuButton.transform.SetParent(gameOverPanel.transform);
            UnityEngine.UI.Button mainMenuButtonComponent = mainMenuButton.AddComponent<UnityEngine.UI.Button>();

            // 添加按钮背景
            UnityEngine.UI.Image mainMenuButtonImage = mainMenuButton.AddComponent<UnityEngine.UI.Image>();
            mainMenuButtonImage.color = new Color(0.2f, 0.2f, 0.6f);

            // 添加按钮文本
            GameObject mainMenuButtonText = new GameObject("Text");
            mainMenuButtonText.transform.SetParent(mainMenuButton.transform);
            UnityEngine.UI.Text mainMenuButtonTextComponent = mainMenuButtonText.AddComponent<UnityEngine.UI.Text>();
            mainMenuButtonTextComponent.text = "返回主菜单";
            mainMenuButtonTextComponent.fontSize = 20;
            mainMenuButtonTextComponent.color = Color.white;
            mainMenuButtonTextComponent.alignment = TextAnchor.MiddleCenter;

            RectTransform mainMenuButtonRect = mainMenuButton.GetComponent<RectTransform>();
            mainMenuButtonRect.sizeDelta = new Vector2(200, 50);
            mainMenuButtonRect.anchoredPosition = new Vector2(0, -40);

            RectTransform mainMenuButtonTextRect = mainMenuButtonText.GetComponent<RectTransform>();
            mainMenuButtonTextRect.sizeDelta = new Vector2(180, 30);
            mainMenuButtonTextRect.anchoredPosition = Vector2.zero;

            // 添加GameOverUI脚本
            GameOverUI gameOverUI = canvas.AddComponent<GameOverUI>();
            gameOverUI.gameOverPanel = gameOverPanel;
            gameOverUI.restartButton = restartButtonComponent;
            gameOverUI.mainMenuButton = mainMenuButtonComponent;

            // 设置Canvas为持久化对象
            DontDestroyOnLoad(canvas);
        }
    }

    private void Start()
    {
        // 监听WaveManager的游戏结束事件
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnGameOver += WaveManager_OnGameOver;
        }
    }

    private void WaveManager_OnGameOver(object sender, System.EventArgs e)
    {
        // 显示游戏结束UI
        GameOverUI gameOverUI = FindObjectOfType<GameOverUI>();
        if (gameOverUI != null)
        {
            gameOverUI.ShowGameOverUI();
        }
    }

    private void OnDestroy()
    {
        // 移除事件监听
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnGameOver -= WaveManager_OnGameOver;
        }
    }
}
