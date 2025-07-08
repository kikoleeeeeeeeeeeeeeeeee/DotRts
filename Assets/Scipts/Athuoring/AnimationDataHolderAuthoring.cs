using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public class AnimationDataHolderAuthoring : MonoBehaviour
{


    public AnimationDataListSo animationDataListSO;
    public Material defaultMaterial;


    public class Baker : Baker<AnimationDataHolderAuthoring>
    {


        public override void Bake(AnimationDataHolderAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AnimationDataHolder animationDataHolder = new AnimationDataHolder();

            int index = 0;
            foreach (AnimationDataSo.AnimationType animationType in System.Enum.GetValues(typeof(AnimationDataSo.AnimationType)))
            {
                AnimationDataSo animationDataSO = authoring.animationDataListSO.GetAnimationDataSo(animationType);

                for (int i = 0; i < animationDataSO.meshArray.Length; i++)
                {
                    Mesh mesh = animationDataSO.meshArray[i];

                    Entity additionalEntity = CreateAdditionalEntity(TransformUsageFlags.None, true);

                    AddComponent(additionalEntity, new MaterialMeshInfo());
                    AddComponent(additionalEntity, new RenderMeshUnmanaged
                    {
                        materialForSubMesh = authoring.defaultMaterial,
                        mesh = mesh,
                    });
                    AddComponent(additionalEntity, new AnimationDataHolderSubEntity
                    {
                        animationType = animationType,
                        meshIndex = i,
                    });
                }

                index++;
            }

            AddComponent(entity, new AnimationDataHolderObjectData
            {
                animationDataListSO = authoring.animationDataListSO,
            });

            AddComponent(entity, animationDataHolder);
        }
    }


}

public struct AnimationDataHolderObjectData : IComponentData
{

    public UnityObjectRef<AnimationDataListSo> animationDataListSO;

}

public struct AnimationDataHolderSubEntity : IComponentData
{

    public AnimationDataSo.AnimationType animationType;
    public int meshIndex;

}

public struct AnimationDataHolder : IComponentData
{

    public BlobAssetReference<BlobArray<AnimationData>> animationDataBlobArrayBlobAssetReference;

}

public struct AnimationData
{

    public float frameTimerMax;
    public int frameMax;
    public BlobArray<int> intMeshIdBlobArray;

}