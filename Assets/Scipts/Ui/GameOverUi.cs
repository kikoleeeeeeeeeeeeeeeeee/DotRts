using UnityEngine;

public class GameOverUi : MonoBehaviour
{

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
