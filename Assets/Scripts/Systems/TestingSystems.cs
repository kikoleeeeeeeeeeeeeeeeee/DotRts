
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

partial struct TestingSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
       /* int unitcount = 0;

        foreach(
            RefRW<Zombie> friendly
            in SystemAPI.Query<RefRW<Zombie>>())
        {
            unitcount++;
        }

        Debug.Log("unitcount:" + unitcount);*/

    }

}
