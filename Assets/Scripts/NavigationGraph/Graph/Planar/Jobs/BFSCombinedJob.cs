using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
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
            while (queueObstacle.Count > 0 || queueCliff.Count > 0)
            {
                int iterO = queueObstacle.Count;
                for (int k = 0; k < iterO; k++)
                {
                    int current = queueObstacle.Dequeue();
                    int cx = current % gridSize.x;
                    int cy = current / gridSize.x;
                    int cd = distObstacle[current];
                    if (cd >= obstacleRadius) continue;

                    EnqueueNeighborObstacle(cx + 1, cy, cd);
                    EnqueueNeighborObstacle(cx - 1, cy, cd);
                    EnqueueNeighborObstacle(cx, cy + 1, cd);
                    EnqueueNeighborObstacle(cx, cy - 1, cd);
                }

                int iterC = queueCliff.Count;
                for (int k = 0; k < iterC; k++)
                {
                    int current = queueCliff.Dequeue();
                    int cx = current % gridSize.x;
                    int cy = current / gridSize.x;
                    int cd = distCliff[current];
                    if (cd >= cliffRadius) continue;

                    EnqueueNeighborCliff(cx + 1, cy, cd);
                    EnqueueNeighborCliff(cx - 1, cy, cd);
                    EnqueueNeighborCliff(cx, cy + 1, cd);
                    EnqueueNeighborCliff(cx, cy - 1, cd);
                }
            }
        }

        private void EnqueueNeighborObstacle(int x, int y, int currentDist)
        {
            if (x < 0 || y < 0 || x >= gridSize.x || y >= gridSize.z) return;

            int idx = x + y * gridSize.x;

            if (idx < 0 || idx >= distObstacle.Length) return;
            if (distObstacle[idx] != -1) return;

            distObstacle[idx] = currentDist + 1;
            nativeObstacleBlocked[idx] = WalkableType.Obstacle;
            queueObstacle.Enqueue(idx);
        }

        private void EnqueueNeighborCliff(int x, int y, int currentDist)
        {
            if (x < 0 || y < 0 || x >= gridSize.x || y >= gridSize.z) return;

            int idx = x + y * gridSize.x;

            if (idx < 0 || idx >= distCliff.Length) return;
            if (distCliff[idx] != -1) return;

            distCliff[idx] = currentDist + 1;
            nativeCliffBlocked[idx] = WalkableType.Air;
            queueCliff.Enqueue(idx);
        }
    }
}
