using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUi : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        // 确保 GameOverUI 实例存在
        if (GameOverUI.Instance == null)
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
            gameOverObj.AddComponent<GameOverUI>();
        }

        // 同步按钮引用
        if (mainMenuButton != null)
        {
            GameOverUI.Instance.mainMenuButton = mainMenuButton;
        }
        if (restartButton != null)
        {
            GameOverUI.Instance.restartButton = restartButton;
        }
    }

    private void Start()
    {
        DotsEventsManager.Instance.OnHQDead += DotsEventsManager_OnHQDead;
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnGameOver += WaveManager_OnGameOver;
        }
        Hide();
    }

    private void DotsEventsManager_OnHQDead(object sender, System.EventArgs e)
    {
        // 重置 buff 系统
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.ResetBuffs();
        }
        Show();
        Time.timeScale = 0f;
    }

    private void WaveManager_OnGameOver(object sender, System.EventArgs e)
    {
        // 重置 buff 系统
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.ResetBuffs();
        }
        Show();
        Time.timeScale = 0f;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.HideGameOverUI();
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
        if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.ShowGameOverUI();
        }
    }

    private void OnDestroy()
    {
        if (DotsEventsManager.Instance != null)
        {
            DotsEventsManager.Instance.OnHQDead -= DotsEventsManager_OnHQDead;
        }
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnGameOver -= WaveManager_OnGameOver;
        }
    }
}
