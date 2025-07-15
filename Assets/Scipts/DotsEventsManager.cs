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
    public event EventHandler OnHealthDead;

    public event EventHandler OnHordeStartedSpawning;
    public event EventHandler OnHordeStartSpawningSoon;

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

    public void TriggerOnHealthDead(NativeList<Entity> entitiyNativeList)
    {
        foreach (Entity entity in entitiyNativeList)
        {
            OnHealthDead?.Invoke(entity, EventArgs.Empty);

        }
    }
    public void TriggerOnHordeStartedSpawning(NativeList<Entity> entityNativeList)
    {
        foreach (Entity entity in entityNativeList)
        {
            OnHordeStartedSpawning?.Invoke(entity, EventArgs.Empty);
        }
    }

    public void TriggerOnHordeStartSpawningSoon(NativeList<Entity> entityNativeList)
    {
        foreach (Entity entity in entityNativeList)
        {
            OnHordeStartSpawningSoon?.Invoke(entity, EventArgs.Empty);
        }
    }

}
