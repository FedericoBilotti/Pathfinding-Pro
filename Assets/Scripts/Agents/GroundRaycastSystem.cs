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
            float3 from = originAgentPositions[i] + upDirection;
            float3 direction = Vector3.down;
            QueryParameters queryParams = new QueryParameters
            {
                layerMask = layerMask
            };

            commands[i] = new RaycastCommand(physicsScene, from, direction, queryParams, rayDistance);
        }
    }
}