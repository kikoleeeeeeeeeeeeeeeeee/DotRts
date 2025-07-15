using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Events;
using static UnityEngine.EventSystems.EventTrigger;

[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
partial struct ResetEventsSystem : ISystem
{
    private NativeArray<JobHandle> jobHandleNativeArray;
    private NativeList<Entity> onBarracksUnitQueueChangeEntityList;
    private NativeList<Entity> onHealthDeadEntityList;

    private NativeList<Entity> onHordeStartedSpawningEntityList;
    private NativeList<Entity> onHordeStartSpawningSoonEntityList;



    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        jobHandleNativeArray = new NativeArray<JobHandle>(3, Allocator.Persistent);
        onBarracksUnitQueueChangeEntityList = new NativeList<Entity>(Allocator.Persistent);
        onHealthDeadEntityList = new NativeList<Entity>(64,Allocator.Persistent);
        onHordeStartedSpawningEntityList = new NativeList<Entity>(Allocator.Persistent);
        onHordeStartSpawningSoonEntityList = new NativeList<Entity>(Allocator.Persistent);

    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<BuildingHQ>())
        {
            
            Health hqhealth =  SystemAPI.GetComponent<Health>(SystemAPI.GetSingletonEntity<BuildingHQ>());

            if (hqhealth.onDead)
            {
                DotsEventsManager.Instance.TriggerOnHQDead();
            }
        }

        jobHandleNativeArray[0] = new ResetSelectedEventsJob().ScheduleParallel(state.Dependency);
        jobHandleNativeArray[1] = new ResetShootAttackEventsJob().ScheduleParallel(state.Dependency);
        jobHandleNativeArray[2] = new ResetMeleeAttackEventsJob().ScheduleParallel(state.Dependency);


        onHordeStartedSpawningEntityList.Clear();
        onHordeStartSpawningSoonEntityList.Clear();
        new ResetHordeEventsJob()
        {
            onHordeStartedSpawningEntityList = onHordeStartedSpawningEntityList.AsParallelWriter(),
            onHordeStartSpawningSoonEntityList = onHordeStartSpawningSoonEntityList.AsParallelWriter(),
        }.ScheduleParallel(state.Dependency).Complete();

        DotsEventsManager.Instance?.TriggerOnHordeStartedSpawning(onHordeStartedSpawningEntityList);
        DotsEventsManager.Instance?.TriggerOnHordeStartSpawningSoon(onHordeStartSpawningSoonEntityList);


        onHealthDeadEntityList.Clear();
        new ResetHealthEventsJob()
        {
            onHealthDeadEntityList = onHealthDeadEntityList.AsParallelWriter()
        }.ScheduleParallel(state.Dependency).Complete();

        DotsEventsManager.Instance?.TriggerOnHealthDead(onHealthDeadEntityList);

        onBarracksUnitQueueChangeEntityList.Clear();
        new ResetBuildingBarracksEventsJob()
        {
            onUnitQueueChangedEntityList = onBarracksUnitQueueChangeEntityList.AsParallelWriter()
        }.ScheduleParallel(state.Dependency).Complete();

        DotsEventsManager.Instance?.TriggerOnBarrackUnitQueueChanged(onBarracksUnitQueueChangeEntityList);


        state.Dependency = JobHandle.CombineDependencies(jobHandleNativeArray);
    }

    public void OnDestroy(ref SystemState state)
    {
        jobHandleNativeArray.Dispose();
        onBarracksUnitQueueChangeEntityList.Dispose();
        onHealthDeadEntityList.Dispose();

    }
}


[BurstCompile]
public partial struct ResetShootAttackEventsJob : IJobEntity {
    public void Execute(ref ShootAttack shootAttack)
    {
        shootAttack.onShoot.isTriggered = false;
    }
}

[BurstCompile]
[WithAll(typeof(Health))]
public partial struct ResetHealthEventsJob : IJobEntity
{
    public NativeList<Entity>.ParallelWriter onHealthDeadEntityList;

    public void Execute(ref Health health,Entity entity )
    {
        if (health.onDead)
        {
            UnityEngine.Debug.Log("have dead");
            onHealthDeadEntityList.AddNoResize(entity);
        }

        health.onHealthChanged = false;
        health.onDead = false;
    }
}


[BurstCompile]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct ResetSelectedEventsJob : IJobEntity {
    public void Execute(ref Selected selected)
    {
        selected.onSelected = false;
        selected.onDeselected = false;
    }

}

[BurstCompile]
public partial struct ResetMeleeAttackEventsJob : IJobEntity
{
    public void Execute(ref MeleeAttack meleeAttack)
    {
        meleeAttack.onAttacked = false;
    }

}


[BurstCompile]
public partial struct ResetBuildingBarracksEventsJob : IJobEntity
{
    public NativeList<Entity>.ParallelWriter onUnitQueueChangedEntityList;
    public void Execute(ref BuildingBarracks buildingBarracks,Entity entity)   
    {
        if (buildingBarracks.onUnitQueueChanged)
        {
            onUnitQueueChangedEntityList.AddNoResize(entity);

        }

        buildingBarracks.onUnitQueueChanged = false;

    }

}


[BurstCompile]

public partial struct ResetHordeEventsJob : IJobEntity
{

    public NativeList<Entity>.ParallelWriter onHordeStartedSpawningEntityList;
    public NativeList<Entity>.ParallelWriter onHordeStartSpawningSoonEntityList;


    public void Execute(ref Horde horde, Entity entity)
    {
        if (horde.onStartSpawningSoon)
        {
            onHordeStartSpawningSoonEntityList.AddNoResize(entity);
        }
        if (horde.onStartSpawning)
        {
            onHordeStartedSpawningEntityList.AddNoResize(entity);
        }
        horde.onStartSpawning = false;
        horde.onStartSpawningSoon = false;
    }

}