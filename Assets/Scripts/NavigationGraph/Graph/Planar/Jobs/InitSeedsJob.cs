using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System;

namespace NavigationGraph.Graph.Planar.Jobs
{
    [BurstCompile]
    internal struct InitSeedsJob : IJobParallelFor
    {
        [ReadOnly] public Vector3Int gridSize;
        [ReadOnly] public NativeArray<Vector3> normalWalkable;
        [ReadOnly] public NativeArray<WalkableType> computedWalkable;
        [ReadOnly] public NativeArray<float> groundHeight;
        [ReadOnly] public float maxHeightDifference;
        [ReadOnly] public float inclineLimit;

        public NativeArray<WalkableType> finalObstacle;
        public NativeArray<WalkableType> finalCliff;

        public NativeArray<int> distObstacle;
        public NativeArray<int> distCliff;

        public NativeQueue<int>.ParallelWriter queueObstacle;
        public NativeQueue<int>.ParallelWriter queueCliff;

        public void Execute(int i)
        {
            ResetCell(i);

            var type = computedWalkable[i];
            if (HandleBaseType(i, type)) return;

            int x = i % gridSize.x;
            int y = i / gridSize.x;

            bool isObstacleNeighbor = false;
            bool isCliffNeighbor = false;

            CheckNeighbor(i, x + 1 < gridSize.x, i + 1, ref isObstacleNeighbor, ref isCliffNeighbor);
            CheckNeighbor(i, x - 1 >= 0, i - 1, ref isObstacleNeighbor, ref isCliffNeighbor);
            CheckNeighbor(i, y + 1 < gridSize.z, i + gridSize.x, ref isObstacleNeighbor, ref isCliffNeighbor);
            CheckNeighbor(i, y - 1 >= 0, i - gridSize.x, ref isObstacleNeighbor, ref isCliffNeighbor);

            EnqueueIfTrue(i, isCliffNeighbor, ref finalCliff, ref distCliff, WalkableType.Air, queueCliff);
            EnqueueIfTrue(i, isObstacleNeighbor, ref finalObstacle, ref distObstacle, WalkableType.Obstacle, queueObstacle);
        }

        private void ResetCell(int i)
        {
            distObstacle[i] = -1;
            distCliff[i] = -1;
            finalObstacle[i] = computedWalkable[i];
            finalCliff[i] = computedWalkable[i];
        }

        private bool HandleBaseType(int i, WalkableType type)
        {
            switch (type)
            {
                case WalkableType.Obstacle:
                    SetSeed(i, WalkableType.Obstacle, ref finalObstacle, ref distObstacle, queueObstacle);
                    return true;

                case WalkableType.Air:
                    SetSeed(i, WalkableType.Air, ref finalCliff, ref distCliff, queueCliff);
                    return true;

                default:
                    return false;
            }
        }

        private void CheckNeighbor(int current, bool valid, int neighbor, ref bool isObstacle, ref bool isCliff)
        {
            if (!valid || neighbor < 0 || neighbor >= computedWalkable.Length)
                return;

            var nType = computedWalkable[neighbor];
            isObstacle |= nType == WalkableType.Obstacle;
            isCliff |= IsCliff(current, neighbor);
        }

        private bool IsCliff(int current, int neighbor)
        {
            if (normalWalkable[current].y <= math.cos(inclineLimit * Mathf.Deg2Rad))
                return true;

            float yDist = math.abs(groundHeight[current] - groundHeight[neighbor]);
            return yDist >= maxHeightDifference;
        }

        private static void SetSeed(int i, WalkableType type, ref NativeArray<WalkableType> final, ref NativeArray<int> dist, NativeQueue<int>.ParallelWriter queue)
        {
            dist[i] = 0;
            final[i] = type;
            queue.Enqueue(i);
        }

        private static void EnqueueIfTrue(int i, bool condition, ref NativeArray<WalkableType> final, ref NativeArray<int> dist, WalkableType type, NativeQueue<int>.ParallelWriter queue)
        {
            if (!condition) return;
            SetSeed(i, type, ref final, ref dist, queue);
        }
    }
}