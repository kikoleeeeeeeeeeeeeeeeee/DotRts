using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

partial struct ActiveAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AnimationDataHolder>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        AnimationDataHolder animationDataHolder = SystemAPI.GetSingleton<AnimationDataHolder>();

        ActiveAnimationJob activeAnimationJob = new ActiveAnimationJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            animationDataBlobArrayBlobAssetReference= animationDataHolder.animationDataBlobArrayBlobAssetReference,

        };
        activeAnimationJob.ScheduleParallel();
        /*
        foreach ((
            RefRW<ActiveAnimation> activeAnimation,
            RefRW<MaterialMeshInfo> materialMeshInfo
            )
            in SystemAPI.Query<
                RefRW<ActiveAnimation>,
                RefRW<MaterialMeshInfo>>()){

            ref AnimationData animationData =
                ref animationDataHolder.animationDataBlobAssetReference.Value[(int)activeAnimation.ValueRW.activeAnimationType];
            activeAnimation.ValueRW.frameTimer += SystemAPI.Time.DeltaTime;
            if (activeAnimation.ValueRW.frameTimer > animationData.frameTimerMax)
            {
                activeAnimation.ValueRW.frameTimer -=animationData.frameTimerMax;
                activeAnimation.ValueRW.frame = (activeAnimation.ValueRW.frame + 1) % animationData.frameMax;

                materialMeshInfo.ValueRW.MeshID = animationData.intMeshIdBlobArray[activeAnimation.ValueRW.frame];
            
                if(activeAnimation.ValueRO.frame == 0 &&
                    activeAnimation.ValueRO.activeAnimationType == AnimationDataSo.AnimationType.SoldierShoot)
                {
                    activeAnimation.ValueRW.activeAnimationType = AnimationDataSo.AnimationType.None;
                }
                if (activeAnimation.ValueRO.frame == 0 &&
                   activeAnimation.ValueRO.activeAnimationType == AnimationDataSo.AnimationType.ZombieAttack)
                {
                    activeAnimation.ValueRW.activeAnimationType = AnimationDataSo.AnimationType.None;
                }
            }
        }*/
    }
}


[BurstCompile]
public partial struct ActiveAnimationJob : IJobEntity {
    public float deltaTime;
    public BlobAssetReference<BlobArray<AnimationData>> animationDataBlobArrayBlobAssetReference;
    public void Execute(ref ActiveAnimation activeAnimation, ref MaterialMeshInfo materialMeshInfo)
    {

        ref AnimationData animationData =
              ref animationDataBlobArrayBlobAssetReference.Value[(int)activeAnimation.activeAnimationType];
        activeAnimation.frameTimer += deltaTime;
        if (activeAnimation.frameTimer > animationData.frameTimerMax)
        {
            activeAnimation.frameTimer -= animationData.frameTimerMax;
            activeAnimation.frame = (activeAnimation.frame + 1) % animationData.frameMax;

            materialMeshInfo.Mesh = animationData.intMeshIdBlobArray[activeAnimation.frame];

            if (activeAnimation.frame == 0 &&
                AnimationDataSo.IsAnimationUninterruptible(activeAnimation.activeAnimationType))
            {
                activeAnimation.activeAnimationType = AnimationDataSo.AnimationType.None;
            }
           
        }
    }
}
