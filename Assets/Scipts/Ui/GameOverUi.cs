using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUi : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(0);
        });
    }
    private void Start()
    {
        DotsEventsManager.Instance.OnHQDead += DotsEventsManager_OnHQDead;
        Hide();
    }

    private void DotsEventsManager_OnHQDead(object sender, System.EventArgs e)
    {
        Show();
        Time.timeScale = 0f;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
    private void Show()
    {
        gameObject.SetActive(true);
    }
}
