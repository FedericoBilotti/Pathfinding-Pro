using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace NavigationGraph.Graph
{
    internal sealed partial class SimpleGridNavigationGraph
    {
        #region Jobs & Burst

        // It could be separated in three diferent jobs, so I dont't have to ask all the time the NeigbhorsPerCell size.
        [BurstCompile]
        public struct CountNeighborsJob : IJob
        {
            [ReadOnly] public NativeArray<Cell> grid;
            [ReadOnly] public NativeArray<int2> offsets16;
            [WriteOnly] public NativeArray<int> neighborCounts;
            public int gridSizeX;
            public int gridSizeZ;
            public float maxHeightDifference;
            public NeighborsPerCell neighborsPerCell;

            public void Execute()
            {
                for (int i = 0; i < grid.Length; i++)
                {
                    int count = 0;

                    if (neighborsPerCell != NeighborsPerCell.Sixteen)
                    {
                        int range = 1;
                        for (int offsetX = -range; offsetX <= range; offsetX++)
                        {
                            for (int offsetZ = -range; offsetZ <= range; offsetZ++)
                            {
                                if (offsetX == 0 && offsetZ == 0) continue;

                                if (neighborsPerCell == NeighborsPerCell.Four &&
                                    math.abs(offsetX) + math.abs(offsetZ) != 1) continue;

                                if (neighborsPerCell == NeighborsPerCell.Eight &&
                                    math.max(math.abs(offsetX), math.abs(offsetZ)) > 1) continue;

                                int gridX = grid[i].gridX + offsetX;
                                int gridZ = grid[i].gridZ + offsetZ;

                                if (gridX >= 0 && gridX < gridSizeX &&
                                    gridZ >= 0 && gridZ < gridSizeZ)
                                {
                                    var yDistance = grid[i].position.y - grid[gridX + gridZ * gridSizeX].position.y;
                                    if (yDistance < maxHeightDifference)
                                        count++;
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var offset in offsets16)
                        {
                            int gridX = grid[i].gridX + offset.x;
                            int gridZ = grid[i].gridZ + offset.y;

                            if (gridX >= 0 && gridX < gridSizeX &&
                                gridZ >= 0 && gridZ < gridSizeZ)
                            {
                                var yDistance = grid[i].position.y - grid[gridX + gridZ * grid.Length].position.y;
                                if (yDistance < maxHeightDifference)
                                    count++;
                            }
                        }
                    }

                    neighborCounts[i] = count;
                }
            }
        }
    }

    #endregion
}
