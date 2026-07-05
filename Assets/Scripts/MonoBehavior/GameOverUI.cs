using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    public GameObject gameOverPanel;
    public UnityEngine.UI.Button restartButton;
    public UnityEngine.UI.Button mainMenuButton;

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

        // 隐藏游戏结束面板
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // 注册按钮点击事件
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    // 显示游戏结束UI
    public void ShowGameOverUI()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    // 隐藏游戏结束UI
    public void HideGameOverUI()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    // 重新开始游戏
    private void RestartGame()
    {
        // 重置波次管理器
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.ResetWave();
        }

        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 返回主菜单
    private void ReturnToMainMenu()
    {
        // 加载主菜单场景
        SceneManager.LoadScene("MainMenuScene");
    }
}
