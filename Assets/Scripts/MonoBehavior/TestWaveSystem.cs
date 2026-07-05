using UnityEngine;

public class TestWaveSystem : MonoBehaviour
{
    [Header("测试控制")]
    [SerializeField] private bool enableTestMode = true;

    private void Update()
    {
        if (!enableTestMode) return;

        // 按 K 键模拟僵尸死亡
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnZombieDeath();
                Debug.Log($"[Test] 模拟僵尸死亡 | 当前击杀数: {WaveManager.Instance.ZombiesKilledInCurrentWave}");
            }
        }
        // 按 N 键进入下一波
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (WaveManager.Instance != null)
            {
                // 先结束当前波次
                WaveManager.Instance.EndCurrentWave();
                Debug.Log($"[Test] 强制结束当前波次 | 当前波次索引：{WaveManager.Instance.CurrentWaveIndex}");
            }
            else
            {
                Debug.LogError("[Test] WaveManager.Instance is null!");
            }
        }
        // 按 R 键重置波次
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.ResetWave();
                WaveManager.Instance.StartPreparationPhase();
                Debug.Log("[Test] 重置波次系统");
            }
        }
    }
}
