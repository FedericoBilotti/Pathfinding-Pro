using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Agents
{
    [BurstCompile]
    public struct GroundRaycastSystem : IJobParallelFor
    {
        [WriteOnly] public NativeArray<RaycastCommand> commands;
        [ReadOnly] public NativeList<float3> originAgentPositions;
        [ReadOnly] public int layerMask;
        [ReadOnly] public PhysicsScene physicsScene;

        public float3 upDirection;
        public float rayDistance;

        public void Execute(int i)
        {
            var queryParams = new QueryParameters { layerMask = layerMask };

            var pos = originAgentPositions[i] + upDirection * 0.5f;

            commands[i] = new RaycastCommand(physicsScene, pos, Vector3.down, queryParams, rayDistance);
        }
    }
}