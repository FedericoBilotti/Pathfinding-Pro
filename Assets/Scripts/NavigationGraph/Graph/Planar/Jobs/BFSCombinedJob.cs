using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NavigationGraph.Graph.Planar.Jobs
{
    [BurstCompile]
    internal struct BFSCombinedJob : IJob
    {
        [ReadOnly] public Vector3Int gridSize;
        [ReadOnly] public int obstacleRadius;
        [ReadOnly] public int cliffRadius;

        public NativeArray<int> distObstacle;
        public NativeArray<int> distCliff;

        public NativeArray<WalkableType> nativeObstacleBlocked;
        public NativeArray<WalkableType> nativeCliffBlocked;

        public NativeQueue<int> queueObstacle;
        public NativeQueue<int> queueCliff;

        public void Execute()
        {
            BFSPropagate(queueObstacle, distObstacle, nativeObstacleBlocked, obstacleRadius, WalkableType.Obstacle);
            BFSPropagate(queueCliff, distCliff, nativeCliffBlocked, cliffRadius, WalkableType.Air);
        }

        private void BFSPropagate(NativeQueue<int> queue, NativeArray<int> dist, NativeArray<WalkableType> finalArray, int radius, WalkableType markType)
        {
            while (queue.Count > 0)
            {
                int iter = queue.Count;
                for (int k = 0; k < iter; k++)
                {
                    int current = queue.Dequeue();
                    int cx = current % gridSize.x;
                    int cy = current / gridSize.x;
                    int cd = dist[current];

                    if (cd >= radius) continue;

                    foreach (var neighbor in GetNeighbors(cx, cy))
                    {
                        int ni = neighbor.x + neighbor.y * gridSize.x;
                        if (ni < 0 || ni >= dist.Length) continue;
                        if (dist[ni] != -1) continue;

                        dist[ni] = cd + 1;
                        finalArray[ni] = markType;
                        queue.Enqueue(ni);
                    }
                }
            }
        }

        private static NativeArray<int2> GetNeighbors(int x, int y)
        {
            var arr = new NativeArray<int2>(4, Allocator.Temp);
            arr[0] = new int2(x + 1, y);
            arr[1] = new int2(x - 1, y);
            arr[2] = new int2(x, y + 1);
            arr[3] = new int2(x, y - 1);
            return arr;
        }
    }
}
