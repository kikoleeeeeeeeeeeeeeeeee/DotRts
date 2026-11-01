using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 兼容壳：早期版本由它负责创建 <see cref="GameOverUI"/> 与同步按钮引用，
/// 现在胜负面板已收敛到场景内的唯一 <see cref="GameOverUI"/>。
///
/// 保留本组件只为兼容既有场景里的挂载，不再创建对象、不再监听事件，避免双实例竞争。
/// </summary>
public class GameOverUi : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button restartButton;

    private void Start()
    {
        GameOverUI panel = GameOverUI.Instance;
        if (panel == null)
        {
            panel = FindObjectOfType<GameOverUI>();
        }

        if (panel == null)
        {
            Debug.LogWarning("[GameOverUi] 场景中没有 GameOverUI 面板，胜负界面将不会显示。请确认 GameScene 中存在挂载 GameOverUI 的对象。");
            return;
        }

        if (mainMenuButton != null)
        {
            panel.mainMenuButton = mainMenuButton;
        }

        if (restartButton != null)
        {
            panel.restartButton = restartButton;
        }
    }
}
