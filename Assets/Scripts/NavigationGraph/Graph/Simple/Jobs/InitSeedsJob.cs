using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace NavigationGraph.Graph
{
    internal sealed partial class SimpleGridNavigationGraph
    {
        #region Jobs & Burst

        [BurstCompile(Debug = true)]
        public struct InitSeedsJob : IJobParallelFor
        {
            [ReadOnly] public int3 gridSize;
            [ReadOnly] public NativeArray<float> groundHeight;
            [ReadOnly] public NativeArray<WalkableType> computedWalkable;

            public float maxHeightDifference;

            public NativeArray<WalkableType> finalObstacle;
            public NativeArray<WalkableType> finalCliff;

            public NativeArray<int> distObstacle;
            public NativeArray<int> distCliff;

            public NativeQueue<int>.ParallelWriter queueObstacle;
            public NativeQueue<int>.ParallelWriter queueCliff;

            // public void Execute(int i)
            // {
            //     finalObstacle[i] = computedWalkable[i];
            //     finalCliff[i] = computedWalkable[i];
            //     distObstacle[i] = -1;
            //     distCliff[i] = -1;

            //     int x = i % gridSize.x;
            //     int y = i / gridSize.x;

            //     if (computedWalkable[i] == WalkableType.Walkable)
            //     {
            //         bool hasObstacleNeighbor = false;
            //         bool hasCliffNeighbor = false;

            //         if (x + 1 < gridSize.x)
            //         {
            //             int n = i + 1;
            //             if (n < gridSize.x)
            //             {
            //                 hasObstacleNeighbor = computedWalkable[n] == WalkableType.Obstacle;
            //                 hasCliffNeighbor = computedWalkable[n] == WalkableType.Air;
            //             }
            //         }
            //         if (x - 1 >= 0)
            //         {
            //             int n = i - 1;
            //             if (n >= gridSize.x)
            //             {
            //                 hasObstacleNeighbor = computedWalkable[n] == WalkableType.Obstacle;
            //                 hasCliffNeighbor = computedWalkable[n] == WalkableType.Air;
            //             }
            //         }
            //         if (y + 1 < gridSize.z)
            //         {
            //             int n = i + gridSize.x;
            //             if (n < gridSize.x)
            //             {
            //                 hasObstacleNeighbor = computedWalkable[n] == WalkableType.Obstacle;
            //                 hasCliffNeighbor = computedWalkable[n] == WalkableType.Air;
            //             }
            //         }
            //         if (y - 1 >= 0)
            //         {
            //             int n = i - gridSize.x;
            //             if (n >= gridSize.x)
            //             {
            //                 hasObstacleNeighbor = computedWalkable[n] == WalkableType.Obstacle;
            //                 hasCliffNeighbor = computedWalkable[n] == WalkableType.Air;
            //             }
            //         }
            //         if (hasObstacleNeighbor)
            //         {
            //             distObstacle[i] = 0;
            //             finalObstacle[i] = WalkableType.Obstacle;
            //             queueObstacle.Enqueue(i);
            //         }

            //         if (hasCliffNeighbor)
            //         {
            //             distCliff[i] = 0;
            //             finalCliff[i] = WalkableType.Air;
            //             queueCliff.Enqueue(i);
            //         }
            //     }
            // }

            public void Execute(int i)
            {
                finalObstacle[i] = computedWalkable[i];
                finalCliff[i] = computedWalkable[i];
                distObstacle[i] = -1;
                distCliff[i] = -1;

                int x = i % gridSize.x;
                int y = i / gridSize.x;

                if (computedWalkable[i] == WalkableType.Walkable)
                {
                    bool hasObstacleNeigbhor = false;
                    bool hasCliffNeighbor = false;

                    if (x + 1 < gridSize.x)
                    {
                        int n = i + 1;
                        if (n < computedWalkable.Length)
                        {
                            hasObstacleNeigbhor |= computedWalkable[n] == WalkableType.Obstacle;
                            hasCliffNeighbor |= IsCliff(i, n);
                        }
                    }

                    if (x - 1 >= 0)
                    {
                        int n = i - 1;
                        if (n >= 0)
                        {
                            hasObstacleNeigbhor |= computedWalkable[n] == WalkableType.Obstacle;
                            hasCliffNeighbor |= IsCliff(i, n);
                        }
                    }

                    if (y + 1 < gridSize.z)
                    {
                        int n = i + gridSize.x;
                        if (n < computedWalkable.Length)
                        {
                            hasObstacleNeigbhor |= computedWalkable[n] == WalkableType.Obstacle;
                            hasCliffNeighbor |= IsCliff(i, n);
                        }
                    }

                    if (y - 1 >= 0)
                    {
                        int n = i - gridSize.x;
                        if (n >= 0)
                        {
                            hasObstacleNeigbhor |= computedWalkable[n] == WalkableType.Obstacle;
                            hasCliffNeighbor |= IsCliff(i, n);
                        }
                    }

                    if (hasObstacleNeigbhor)
                    {
                        distObstacle[i] = 0;
                        finalObstacle[i] = WalkableType.Obstacle;
                        queueObstacle.Enqueue(i);
                    }

                    if (hasCliffNeighbor)
                    {
                        distCliff[i] = 0;
                        finalCliff[i] = WalkableType.Air;
                        queueCliff.Enqueue(i);
                    }
                }
            }

            private bool IsCliff(int currentIndex, int neighborIndex)
            {
                if (computedWalkable[neighborIndex] != WalkableType.Air) return false;

                float yDistance = math.abs(groundHeight[currentIndex] - groundHeight[neighborIndex]);

                Debug.Log("Mi yDistance: " + yDistance);
                Debug.Log("Mi maxHeightDifference: " + maxHeightDifference);

                if (yDistance >= maxHeightDifference) return false;

                Debug.Log("Adding new Cliff in init");
                return true;
            }
        }
    }

    #endregion
}
