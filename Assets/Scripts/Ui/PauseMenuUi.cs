using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUi : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject pauseMenuPanel;

    private void Awake()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(() =>
            {
                ResumeGame();
            });
        }
        else
        {
        }
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(() =>
            {
                QuitGame();
            });
        }
        else
        {
        }
        if (pauseMenuPanel != null)
        {
        }
        else
        {
        }
    }

    private void Start()
    {
        // 初始时隐藏暂停菜单
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // 按下ESC键切换暂停菜单显示状态
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pauseMenuPanel != null)
            {
                if (pauseMenuPanel.activeSelf)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
            else
            {
            }
        }
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }
}