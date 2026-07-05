using System;
using UnityEngine;
using UnityEngine.UI;

public class WaveManagerUi : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text waveNumberText;
    [SerializeField] private Text timeRemainingText;

    private WaveManager waveManager;

    private void Awake()
    {
        waveManager = FindObjectOfType<WaveManager>();
    }

    private void Start()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveStarted += WaveManager_OnWaveStarted;
            waveManager.OnWaveTimerUpdated += WaveManager_OnWaveTimerUpdated;
        }
    }

    private void OnDestroy()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveStarted -= WaveManager_OnWaveStarted;
            waveManager.OnWaveTimerUpdated -= WaveManager_OnWaveTimerUpdated;
        }
    }

    private void WaveManager_OnWaveTimerUpdated(object sender, float timerValue)
    {
        UpdateWaveUI();
    }

    private void Update()
    {
        if (waveManager != null)
        {
            UpdateWaveUI();
        }
    }

    private void UpdateWaveUI()
    {
        // 检查UI引用是否存在
        if (waveNumberText == null || timeRemainingText == null)
        {
            return;
        }

        // 显示波次数（使用WaveData中的waveNumber）
        waveNumberText.text = $"第 {waveManager.CurrentWaveData.waveNumber} 波";

        // 显示剩余时间
        if (waveManager.IsPreparationPhase)
        {
            timeRemainingText.text = $"准备时间: {waveManager.PreparationTimer:F1}s";
        }
        else if (waveManager.IsWaveActive)
        {
            // 直接使用waveTimer的值（现在是递减的剩余时间）
            timeRemainingText.text = $"剩余时间: {Mathf.Max(0, waveManager.WaveTimer):F1}s";
        }
    }

    private void WaveManager_OnWaveStarted(object sender, EventArgs e)
    {
    }
}