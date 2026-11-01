using UnityEngine;

/// <summary>
/// 兼容壳：早期版本由它监听 <see cref="DotsEventsManager.OnZombieDead"/> 回调 <see cref="WaveManager.OnZombieDeath"/>。
///
/// 现在击杀统计由 ECS 侧 <see cref="HealthDeadSystem"/> 直接写入 <see cref="WaveState.zombiesKilledInCurrentWave"/>，
/// WaveManager 再按节流同步到 Mono。保留本组件只为兼容既有场景挂载，避免 Missing Script。
/// </summary>
public class WaveKillListener : MonoBehaviour
{
    private void Start()
    {
        if (DotsEventsManager.Instance == null)
        {
            Debug.LogWarning("[WaveKillListener] DotsEventsManager 不存在，击杀统计不影响波次流程。");
        }
    }
}
