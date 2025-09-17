using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace NavigationGraph.Graph
{
    internal sealed partial class SimpleGridNavigationGraph
    {
        #region Jobs & Burst

        [BurstCompile]
        private struct CreateGridJob : IJobParallelFor
        {
            [ReadOnly] public NativeHashMap<int, int> walkableRegionsDic;
            [WriteOnly] public NativeArray<Cell> grid;
            public Vector3 origin;
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

                Vector3 defaultPos = origin
                    + Vector3.right * ((x + 0.5f) * cellDiameter)
                    + Vector3.forward * ((y + 0.5f) * cellDiameter);

                bool hit = results[i].distance > kHitEpsilon;
                Vector3 cellPosition = hit ? results[i].point : defaultPos;

                walkableRegionsDic.TryGetValue(layerPerCell[i], out int penalty);

                grid[i] = new Cell
                {
                    position = cellPosition,
                    gridIndex = i,
                    gridX = x,
                    gridZ = y,
                    walkableType = GetWalkableType(i),
                    cellCostPenalty = penalty
                };
            }

            private WalkableType GetWalkableType(int index)
            {
                WalkableType cliff = nativeCliffBlocked[index];
                WalkableType finalB = nativeObstacleBlocked[index];

                return (cliff, finalB) switch
                {
                    var (c, f) when c == WalkableType.Air || f == WalkableType.Air
                        => WalkableType.Air,

                    var (c, f) when c == WalkableType.Walkable || f == WalkableType.Walkable
                        => WalkableType.Walkable,

                    var (c, f) when c == WalkableType.Obstacle || f == WalkableType.Obstacle
                        => WalkableType.Obstacle,

                    _ => WalkableType.Roof
                };
            }
        }
    }

    #endregion
}
