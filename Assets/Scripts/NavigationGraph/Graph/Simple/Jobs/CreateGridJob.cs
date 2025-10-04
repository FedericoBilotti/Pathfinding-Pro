using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NavigationGraph.Graph
{
    [BurstCompile]
    internal struct CreateGridJob : IJobParallelFor
    {
        [ReadOnly] public NativeHashMap<int, int> walkableRegionsDic;
        [WriteOnly] public NativeArray<Node> grid;
        public float3 origin;
        public float3 right;
        public float3 forward;

        public float cellDiameter;
        public int gridSizeX;

        [ReadOnly] public NativeArray<RaycastHit> results;
        [ReadOnly] public NativeArray<int> layerPerCell;
        [ReadOnly] public NativeArray<WalkableType> nativeObstacleBlocked;
        [ReadOnly] public NativeArray<WalkableType> nativeCliffBlocked;

        public void Execute(int i)
        {
            const float kHitEpsilon = 1e-4f;
            int x = i % gridSizeX;
            int y = i / gridSizeX;

            float3 defaultPos = origin
                + right * ((x + 0.5f) * cellDiameter)
                + forward * ((y + 0.5f) * cellDiameter);

            bool hit = results[i].distance > kHitEpsilon;

            walkableRegionsDic.TryGetValue(layerPerCell[i], out int penalty);

            var walkableTypeDebug = GetWalkableType(i);

            grid[i] = new Node
            {
                position = hit ? results[i].point : defaultPos,
                normal = hit ? results[i].normal : math.up(),
                gridIndex = i,
                gridX = x,
                gridZ = y,
                walkableType = walkableTypeDebug,
                cellCostPenalty = penalty
            };
        }

        private WalkableType GetWalkableType(int index)
        {
            WalkableType cliff = nativeCliffBlocked[index];
            WalkableType blocked = nativeObstacleBlocked[index];

            return (cliff, blocked) switch
            {
                var (c, b) when c == WalkableType.Air
                    => WalkableType.Air,

                var (c, b) when b == WalkableType.Obstacle
                    => WalkableType.Obstacle,

                _ => WalkableType.Walkable
                // Roof is left
            };
        }
    }
}
