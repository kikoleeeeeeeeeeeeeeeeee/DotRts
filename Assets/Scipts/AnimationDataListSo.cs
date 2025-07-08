using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


[CreateAssetMenu()]
public class AnimationDataListSo : ScriptableObject
{
    public List<AnimationDataSo> animationDataSoList;

    public AnimationDataSo GetAnimationDataSo(AnimationDataSo.AnimationType animationType)
    {
        foreach(AnimationDataSo animationDataSo in animationDataSoList)
        {
            if(animationDataSo.animationType == animationType)
            {
                return animationDataSo;
            }
        }
        return null;
    }
}
   
