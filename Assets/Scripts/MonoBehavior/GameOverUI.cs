using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 胜负面板（唯一入口）。失败与胜利复用同一个面板，仅标题文案不同。
///
/// 事件来源：
/// - <see cref="DotsEventsManager.OnHQDead"/>：HQ 血量归零，统一转交 WaveManager.GameOver()。
/// - <see cref="WaveManager.OnGameOver"/> / <see cref="WaveManager.OnVictory"/>：自身显示面板。
/// </summary>
public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject gameOverPanel;
    [SerializeField] private Text titleText;
    [SerializeField] private string gameOverTitle = "游戏结束";
    [SerializeField] private string victoryTitle = "胜利";

    [Header("Buttons")]
    public Button restartButton;
    public Button mainMenuButton;

    private WaveManager waveManager;
    private bool isSubscribed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            // 保留场景内第一个实例，不再销毁场景内真实面板对象。
            enabled = false;
            return;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Subscribe()
    {
        if (isSubscribed) return;

        waveManager = WaveManager.Instance != null ? WaveManager.Instance : FindObjectOfType<WaveManager>();

        if (DotsEventsManager.Instance != null)
        {
            DotsEventsManager.Instance.OnHQDead -= DotsManager_OnHQDead;
            DotsEventsManager.Instance.OnHQDead += DotsManager_OnHQDead;
        }

        if (waveManager != null)
        {
            waveManager.OnGameOver -= WaveManager_OnGameOver;
            waveManager.OnGameOver += WaveManager_OnGameOver;
            waveManager.OnVictory -= WaveManager_OnVictory;
            waveManager.OnVictory += WaveManager_OnVictory;
        }

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed) return;

        if (DotsEventsManager.Instance != null)
        {
            DotsEventsManager.Instance.OnHQDead -= DotsManager_OnHQDead;
        }

        if (waveManager != null)
        {
            waveManager.OnGameOver -= WaveManager_OnGameOver;
            waveManager.OnVictory -= WaveManager_OnVictory;
        }

        isSubscribed = false;
    }

    private void DotsManager_OnHQDead(object sender, System.EventArgs e)
    {
        if (waveManager != null)
        {
            // 统一走 WaveManager 的失败流程，保证状态与 UI 只有一条路径。
            waveManager.GameOver();
            return;
        }

        ResetBuffState();
        Show(gameOverTitle);
        GameTimeController.RequestPause(GameTimeController.GameOverReason);
    }

    private void WaveManager_OnGameOver(object sender, System.EventArgs e)
    {
        ResetBuffState();
        Show(gameOverTitle);
        GameTimeController.RequestPause(GameTimeController.GameOverReason);
    }

    private void WaveManager_OnVictory(object sender, System.EventArgs e)
    {
        ResetBuffState();
        Show(victoryTitle);
        GameTimeController.RequestPause(GameTimeController.GameOverReason);
    }

    public void ShowGameOverUI()
    {
        Show(gameOverTitle);
    }

    /// <summary>
    /// 由兼容壳 <see cref="GameOverUi"/> 在按钮引用缺失时补注入。
    /// 统一在这里注册监听，避免「壳换掉按钮但监听还留在旧按钮上」导致点击无响应。
    /// </summary>
    public void BindButtons(Button restart, Button mainMenu)
    {
        if (restart != null && restart != restartButton)
        {
            if (restartButton != null) restartButton.onClick.RemoveListener(RestartGame);
            restartButton = restart;
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenu != null && mainMenu != mainMenuButton)
        {
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            mainMenuButton = mainMenu;
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    public void ShowGameOverUI(string title)
    {
        Show(string.IsNullOrEmpty(title) ? gameOverTitle : title);
    }

    private void Show(string title)
    {
        Text target = EnsureTitleText();
        if (target != null && !string.IsNullOrEmpty(title))
        {
            target.text = title;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public void HideGameOverUI()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 取标题文本对象。场景里的胜负面板原本没有标题节点（只能显示同一套按钮），
    /// 这里在首次显示时按面板内已有字体动态补一个，保证「胜利 / 失败」文案可区分，
    /// 且不依赖 Inspector 手工绑定。
    /// </summary>
    private Text EnsureTitleText()
    {
        if (titleText != null) return titleText;

        Transform parent = gameOverPanel != null ? gameOverPanel.transform : transform;
        if (parent == null) return null;

        Text sample = parent.GetComponentInChildren<Text>(true);
        Font font = sample != null ? sample.font : null;
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject titleObject = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
        titleObject.transform.SetParent(parent, false);

        RectTransform rect = titleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -80f);
        rect.sizeDelta = new Vector2(800f, 140f);

        Text text = titleObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = 80;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = gameOverTitle;

        titleText = text;
        return titleText;
    }

    private void ResetBuffState()
    {
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.ResetBuffs();
        }
    }

    private void RestartGame()
    {
        GameTimeController.ClearAll();

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.ResetWave();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReturnToMainMenu()
    {
        GameTimeController.ClearAll();
        SceneManager.LoadScene("MainMenuScene");
    }
}
