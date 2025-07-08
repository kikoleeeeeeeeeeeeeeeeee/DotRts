using System.Diagnostics;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.VisualScripting;
using static UnityEngine.GraphicsBuffer;

[UpdateAfter(typeof(ShootAttackSystem))]
partial struct AnimationStateSystem : ISystem
{
    private ComponentLookup<ActiveAnimation> activeAnimationComponentLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        activeAnimationComponentLookup = state.GetComponentLookup<ActiveAnimation>(false);
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        activeAnimationComponentLookup.Update(ref state);

        IdleWalkingAnimationStateJob idleWalkingAnimationStateJob = new IdleWalkingAnimationStateJob { 
            activeAnimationComponentLookup =  activeAnimationComponentLookup, 
        };
        idleWalkingAnimationStateJob.ScheduleParallel();

        activeAnimationComponentLookup.Update(ref state);

        AimShootAnimationStateJob aimShootAnimationStateJob = new AimShootAnimationStateJob
        {
            activeAnimationComponentLookup = activeAnimationComponentLookup,
        };
        aimShootAnimationStateJob.ScheduleParallel();

        MeleeAttackAnimationStateJob meleeAttackAnimationStateJob = new MeleeAttackAnimationStateJob
        {
            activeAnimationComponentLookup = activeAnimationComponentLookup,
        };
        meleeAttackAnimationStateJob.ScheduleParallel();

        /*  foreach ((
              RefRW<AnimatedMesh> animatedMesh,
              RefRO<UnityMover> unityMover,
              RefRO<UnitAnimation> unitAnimation)
          in SystemAPI.Query<
              RefRW<AnimatedMesh>,
              RefRO<UnityMover>,
               RefRO<UnitAnimation>>())
          {
              RefRW<ActiveAnimation> activeAnimation = SystemAPI.GetComponentRW<ActiveAnimation>(animatedMesh.ValueRO.meshEntity);
              if (unityMover.ValueRO.isMoving)
              {
                  activeAnimation.ValueRW.nextAnimationType = unitAnimation.ValueRO.walkAnimationType;
              }
              else
              {
                  activeAnimation.ValueRW.nextAnimationType = unitAnimation.ValueRO.idleAnimationType;
              }
          }*/
        /*
          foreach ((
             RefRW<AnimatedMesh> animatedMesh,
             RefRO<ShootAttack> shootAttack,
             RefRO < UnityMover > unityMover,
             RefRO<Target> target,
             RefRO <UnitAnimation> unitAnimation)
         in SystemAPI.Query<
             RefRW<AnimatedMesh>,
             RefRO<ShootAttack>,
             RefRO<UnityMover>,
             RefRO<Target>,
              RefRO <UnitAnimation>>())
          {
              if (!unityMover.ValueRO.isMoving && target.ValueRO.targetEntity != Entity.Null)
              {
                  RefRW<ActiveAnimation> activeAnimation = SystemAPI.GetComponentRW<ActiveAnimation>(animatedMesh.ValueRO.meshEntity);
                  activeAnimation.ValueRW.nextAnimationType = unitAnimation.ValueRO.aimAnimationType;

              }
              if (shootAttack.ValueRO.onShoot.isTriggered)
              {
                  RefRW<ActiveAnimation> activeAnimation = SystemAPI.GetComponentRW<ActiveAnimation>(animatedMesh.ValueRO.meshEntity);
                  activeAnimation.ValueRW.nextAnimationType = unitAnimation.ValueRO.shootAnimationType;

              }
          }

          foreach ((
             RefRW<AnimatedMesh> animatedMesh,
             RefRO<MeleeAttack> meleeAttack,
             RefRO<UnitAnimation> unitAnimation)
         in SystemAPI.Query<
             RefRW<AnimatedMesh>,
             RefRO<MeleeAttack>,
             RefRO<UnitAnimation>>())
          {

              if (meleeAttack.ValueRO.onAttacked)
              {
                  RefRW<ActiveAnimation> activeAnimation = SystemAPI.GetComponentRW<ActiveAnimation>(animatedMesh.ValueRO.meshEntity);
                  activeAnimation.ValueRW.nextAnimationType = unitAnimation.ValueRO.meleeAttackAnimationType;

              }
          }*/
    }
}
public partial  struct IdleWalkingAnimationStateJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<ActiveAnimation> activeAnimationComponentLookup;
    public void Execute(in AnimatedMesh animatedMesh,
            in UnityMover unityMover,
            in UnitAnimation unitAnimation)
    {
        RefRW<ActiveAnimation> activeAnimation = activeAnimationComponentLookup.GetRefRW(animatedMesh.meshEntity);
         if (unityMover.isMoving)
            {
                activeAnimation.ValueRW.nextAnimationType = unitAnimation.walkAnimationType;
            }
            else
            {
                activeAnimation.ValueRW.nextAnimationType = unitAnimation.idleAnimationType;
            }
    }

}
public partial struct AimShootAnimationStateJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<ActiveAnimation> activeAnimationComponentLookup;
    public void Execute(ref AnimatedMesh animatedMesh,
           in ShootAttack shootAttack,
           in UnityMover unityMover,
           in Target target,
           in UnitAnimation unitAnimation)
    {
        if (!unityMover.isMoving && target.targetEntity != Entity.Null)
        {
            RefRW<ActiveAnimation> activeAnimation = activeAnimationComponentLookup.GetRefRW(animatedMesh.meshEntity);
            activeAnimation.ValueRW.nextAnimationType = unitAnimation.aimAnimationType;

        }
        if (shootAttack.onShoot.isTriggered)
        {
            RefRW<ActiveAnimation> activeAnimation = activeAnimationComponentLookup.GetRefRW(animatedMesh.meshEntity);
            activeAnimation.ValueRW.nextAnimationType = unitAnimation.shootAnimationType;

        }
    }

}

public partial struct MeleeAttackAnimationStateJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<ActiveAnimation> activeAnimationComponentLookup;
    public void Execute(ref AnimatedMesh animatedMesh,
             in MeleeAttack meleeAttack,
             in UnitAnimation unitAnimation)
    {
        if (meleeAttack.onAttacked)
        {
            RefRW<ActiveAnimation> activeAnimation = activeAnimationComponentLookup.GetRefRW(animatedMesh.meshEntity);
            activeAnimation.ValueRW.nextAnimationType = unitAnimation.meleeAttackAnimationType;

        }
    }

}