using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace NavigationGraph.Graph
{
    [BurstCompile]
    public struct PrecomputeNeighborsJob : IJob
    {
        [ReadOnly] public NativeArray<Cell> grid;
        [ReadOnly] public NativeArray<int2> offsets16;
        [ReadOnly] public NativeArray<int> neighborCounts;
        [ReadOnly] public NativeArray<int> neighborOffsets;
        [WriteOnly] public NativeArray<int> allNeighbors;
        public int gridSizeX;
        public int gridSizeZ;
        public float maxHeightDifference;
        public NeighborsPerCell neighborsPerCell;

        public void Execute()
        {
            for (int i = 0; i < grid.Length; i++)
            {
                int count = 0;
                int baseIndex = neighborOffsets[i];
                Cell currentCell = grid[i];
                int maxNeighbors = neighborCounts[i];

                if (neighborsPerCell != NeighborsPerCell.Sixteen)
                {
                    int range = 1;
                    for (int offsetX = -range; offsetX <= range; offsetX++)
                    {
                        for (int offsetZ = -range; offsetZ <= range; offsetZ++)
                        {
                            if (count >= maxNeighbors) break;
                            if (offsetX == 0 && offsetZ == 0) continue;

                            if (neighborsPerCell == NeighborsPerCell.Four &&
                                math.abs(offsetX) + math.abs(offsetZ) != 1) continue;

                            if (neighborsPerCell == NeighborsPerCell.Eight &&
                                math.max(math.abs(offsetX), math.abs(offsetZ)) > 1) continue;

                            int gridX = currentCell.gridX + offsetX;
                            int gridZ = currentCell.gridZ + offsetZ;

                            if (gridX >= 0 && gridX < gridSizeX &&
                                gridZ >= 0 && gridZ < gridSizeZ)
                            {
                                int neighborIndex = gridX + gridZ * gridSizeX;
                                var yDistance = math.abs(currentCell.position.y - grid[neighborIndex].position.y);
                                if (yDistance < maxHeightDifference)
                                {
                                    allNeighbors[baseIndex + count] = neighborIndex;
                                    count++;
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (var offset in offsets16)
                    {
                        if (count >= maxNeighbors) break;

                        int gridX = currentCell.gridX + offset.x;
                        int gridZ = currentCell.gridZ + offset.y;

                        if (gridX >= 0 && gridX < gridSizeX &&
                            gridZ >= 0 && gridZ < gridSizeZ)
                        {
                            int neighborIndex = gridX + gridZ * gridSizeX;
                            var yDistance = math.abs(currentCell.position.y - grid[neighborIndex].position.y);
                            if (yDistance < maxHeightDifference)
                            {
                                allNeighbors[baseIndex + count] = neighborIndex;
                                count++;
                            }
                        }
                    }
                }
            }
        }
    }
}
