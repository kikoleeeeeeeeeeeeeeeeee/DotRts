using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

public class DotsEventsManager : MonoBehaviour
{
    public static DotsEventsManager Instance { get; private set; }

    public event EventHandler OnBarracksUnitQueueChanged;
    public event EventHandler OnHQDead;

    private void Awake()
    {
        Instance = this;
    }

    public void TriggerOnBarrackUnitQueueChanged(NativeList<Entity> entitiyNativeList)
    {
        foreach (Entity entity in entitiyNativeList)
        {
            OnBarracksUnitQueueChanged?.Invoke(entity, EventArgs. Empty);
                
        }
    }

    public void TriggerOnHQDead()
    {
        OnHQDead?.Invoke(this, EventArgs. Empty); 
    }    
}
