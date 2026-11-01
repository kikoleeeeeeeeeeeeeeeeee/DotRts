using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 统一的游戏暂停控制。
///
/// 暂停菜单、Buff 选择面板、胜负面板都可能请求暂停，如果各自直接写 Time.timeScale，
/// 会出现「关掉 A 面板把 B 面板的暂停也解除」的互相踩踏。这里用来源集合统一裁决：
/// 只要还有一个来源在请求暂停，timeScale 就保持 0。
/// </summary>
public static class GameTimeController
{
    public const string PauseMenuReason = "PauseMenu";
    public const string BuffSelectionReason = "BuffSelection";
    public const string GameOverReason = "GameOver";
    public const string DebugReason = "Debug";

    private static readonly HashSet<string> activePauseReasons = new HashSet<string>();

    public static bool IsPaused => activePauseReasons.Count > 0;

    public static void RequestPause(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return;

        if (activePauseReasons.Add(reason))
        {
            Apply();
        }
    }

    public static void ReleasePause(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return;

        if (activePauseReasons.Remove(reason))
        {
            Apply();
        }
    }

    /// <summary>清空所有暂停来源并恢复时间（场景重载、重开一局时调用）。</summary>
    public static void ClearAll()
    {
        if (activePauseReasons.Count == 0)
        {
            Time.timeScale = 1f;
            return;
        }

        activePauseReasons.Clear();
        Apply();
    }

    private static void Apply()
    {
        Time.timeScale = activePauseReasons.Count > 0 ? 0f : 1f;
    }
}
