using System;
using Unity.Collections;
using UnityEngine;

public class WaveKillListener : MonoBehaviour
{
    private void Start()
    {
        DotsEventsManager.Instance.OnZombieDead += DotsEventsManager_OnZombieDead;
    }

    private void OnDestroy()
    {
        if (DotsEventsManager.Instance != null)
        {
            DotsEventsManager.Instance.OnZombieDead -= DotsEventsManager_OnZombieDead;
        }
    }

    private void DotsEventsManager_OnZombieDead(object sender, EventArgs e)
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnZombieDeath();
        }
    }
}
