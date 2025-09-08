using NavigationGraph;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Utilities;

namespace Pathfinding.PathImplementation
{
    [BurstCompile]
    internal struct AStarJob : IJob
    {
        [ReadOnly] public NativeArray<Cell> grid;
        [ReadOnly] public NativeArray<int> allNeighbors;
        [ReadOnly] public NativeArray<int> neighborCounts;
        [ReadOnly] public int neighborsPerCell;

        public NativeHashSet<int> closedList;
        public NativePriorityQueue<PathCellData> openList;
        public NativeHashMap<int, PathCellData> visitedNodes;

        public int gridSizeX;

        public int startIndex;
        public int endIndex;

        public int patience;

        public void Execute()
        {
            var startData = new PathCellData { cellIndex = startIndex, gCost = 0, hCost = GetDistance(startIndex, endIndex), cameFrom = -1, HeapIndex = int.MaxValue };
            openList.Enqueue(startData);
            visitedNodes.Add(startIndex, startData);

            while (openList.Length > 0 && patience >= 0)
            {
                PathCellData currentData = openList.Dequeue();
                int currentIndex = currentData.cellIndex;
                closedList.Add(currentIndex);

                if (currentIndex == endIndex) return;

                NativeSlice<int> neighbors = GetNeighbors(currentIndex);

                WalkableType unwalkableTypes = WalkableType.Obstacle | WalkableType.Roof | WalkableType.Air;

                foreach (int neighborIndex in neighbors)
                {
                    if (neighborIndex < 0 || neighborIndex >= grid.Length) break;

                    if ((grid[neighborIndex].walkableType & unwalkableTypes) != WalkableType.Walkable || closedList.Contains(neighborIndex))
                        continue;

                    int costToNeighbor = currentData.gCost + GetDistance(currentIndex, neighborIndex) + grid[neighborIndex].cellCostPenalty;

                    if (visitedNodes.TryGetValue(neighborIndex, out PathCellData neighborData))
                    {
                        if (costToNeighbor >= neighborData.gCost) continue;
                    }

                    var newNeighborData = new PathCellData
                    {
                        cellIndex = neighborIndex,
                        cameFrom = currentIndex,
                        gCost = costToNeighbor,
                        hCost = GetDistance(neighborIndex, endIndex),
                        HeapIndex = int.MaxValue
                    };
                    visitedNodes[neighborIndex] = newNeighborData;
                    openList.Enqueue(newNeighborData);
                }
            }
        }

        private NativeSlice<int> GetNeighbors(int currentIndex)
        {
            int maxNeighbors = neighborsPerCell;
            int start = currentIndex * maxNeighbors;
            int count = neighborCounts[currentIndex];

            return allNeighbors.Slice(start, count);
        }

        private int GetDistance(int indexCellA, int indexCellB)
        {
            int xDistance = math.abs(grid[indexCellA].gridX - grid[indexCellB].gridX);
            int zDistance = math.abs(grid[indexCellA].gridZ - grid[indexCellB].gridZ);

            if (xDistance > zDistance) return 14 * zDistance + 10 * (xDistance - zDistance);

            return 14 * xDistance + 10 * (zDistance - xDistance);
        }
    }

    [BurstCompile]
    internal struct AddPath : IJob
    {
        [ReadOnly] public NativeArray<Cell> grid;
        [ReadOnly] public NativeHashMap<int, PathCellData> visitedNodes;
        public NativeList<Cell> finalPath;

        [ReadOnly] public int endIndex;

        public void Execute()
        {
            AddFinalPath(endIndex);
        }

        private void AddFinalPath(int lastIndex)
        {
            int currentIndex = lastIndex;

            while (currentIndex != -1)
            {
                finalPath.Add(grid[currentIndex]);
                currentIndex = visitedNodes[currentIndex].cameFrom;
            }

            if (finalPath.Length > 0)
                finalPath.RemoveAt(finalPath.Length - 1);
        }
    }

    [BurstCompile]
    internal struct ReversePath : IJob
    {
        public NativeList<Cell> finalPath;

        public void Execute()
        {
            int length = finalPath.Length;
            for (int i = 0; i < length / 2; i++)
            {
                (finalPath[i], finalPath[length - i - 1]) = (finalPath[length - i - 1], finalPath[i]);
            }
        }
    }
}