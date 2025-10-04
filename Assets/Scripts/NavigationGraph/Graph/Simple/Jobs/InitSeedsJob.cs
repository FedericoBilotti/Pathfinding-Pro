using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System;

namespace NavigationGraph.Graph
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
            distObstacle[i] = -1;
            distCliff[i] = -1;
            finalObstacle[i] = computedWalkable[i];
            finalCliff[i] = computedWalkable[i];

            if (computedWalkable[i] == WalkableType.Obstacle)
            {
                distObstacle[i] = 0;
                finalObstacle[i] = WalkableType.Obstacle;
                queueObstacle.Enqueue(i);
            }
            else if (computedWalkable[i] == WalkableType.Air)
            {
                distCliff[i] = 0;
                finalCliff[i] = WalkableType.Air;
                queueCliff.Enqueue(i);
            }
            else // if it's walkable
            {
                int x = i % gridSize.x;
                int y = i / gridSize.x;

                bool isCliffNeighbor = false;
                bool isObstacleNeighbor = false;

                if (x + 1 < gridSize.x)
                {
                    int ni = i + 1;
                    if (ni >= 0 && ni < computedWalkable.Length)
                    {
                        isObstacleNeighbor |= computedWalkable[ni] == WalkableType.Obstacle;
                        isCliffNeighbor |= IsCliff(i, ni);
                    }
                }

                if (x - 1 >= 0)
                {
                    int ni = i - 1;
                    if (ni >= 0 && ni < computedWalkable.Length)
                    {
                        isObstacleNeighbor |= computedWalkable[ni] == WalkableType.Obstacle;
                        isCliffNeighbor |= IsCliff(i, ni);
                    }
                }

                if (y + 1 < gridSize.z)
                {
                    int ni = i + gridSize.x;
                    if (ni >= 0 && ni < computedWalkable.Length)
                    {
                        isObstacleNeighbor |= computedWalkable[ni] == WalkableType.Obstacle;
                        isCliffNeighbor |= IsCliff(i, ni);
                    }
                }

                if (y - 1 >= 0)
                {
                    int ni = i - gridSize.x;
                    if (ni >= 0 && ni < computedWalkable.Length)
                    {
                        isObstacleNeighbor |= computedWalkable[ni] == WalkableType.Obstacle;
                        isCliffNeighbor |= IsCliff(i, ni);
                    }
                }

                if (isCliffNeighbor)
                {
                    distCliff[i] = 0;
                    finalCliff[i] = WalkableType.Air;
                    queueCliff.Enqueue(i);
                }

                if (isObstacleNeighbor)
                {
                    distObstacle[i] = 0;
                    finalObstacle[i] = WalkableType.Obstacle;
                    queueObstacle.Enqueue(i);
                }
            }
        }

        private bool IsCliff(int currentIndex, int neighborIndex)
        {
            if (normalWalkable[currentIndex].y <= Mathf.Cos(inclineLimit * Mathf.Deg2Rad))
                return true;

            float yDistance = math.abs(groundHeight[currentIndex] - groundHeight[neighborIndex]);
            return yDistance >= maxHeightDifference;
        }
    }
}
