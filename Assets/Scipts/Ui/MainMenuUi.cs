using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUi : MonoBehaviour
{
    [SerializeField] private Button playbutton;
    [SerializeField] private Button quitbutton;

    private void Awake()
    {
        playbutton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(1);
        });
        quitbutton.onClick.AddListener(() =>
        {
            Application.Quit(); 
        });
    }

}
