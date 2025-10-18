using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NavigationGraph.Graph.Planar.Jobs
{
    // Refac this entire class
    [BurstCompile]
    internal struct PrecomputeNeighborsJob : IJob
    {
        [ReadOnly] public NativeArray<Node> grid;
        [ReadOnly] public NativeArray<int2> offsets16;
        [ReadOnly] public NativeArray<int> neighborCounts;
        [ReadOnly] public NativeArray<int> neighborOffsets;
        [WriteOnly] public NativeArray<int> allNeighbors;
        
        public int gridSizeX; 
        public int gridSizeZ;
        public NeighborsPerCell neighborsPerCell;

        public void Execute()
        {
            for (int i = 0; i < grid.Length; i++)
            {
                int count = 0;
                int baseIndex = neighborOffsets[i];
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

                            int gridX = grid[i].gridX + offsetX;
                            int gridZ = grid[i].gridZ + offsetZ;

                            if (gridX >= 0 && gridX < gridSizeX &&
                                gridZ >= 0 && gridZ < gridSizeZ)
                            {
                                int neighborIndex = gridX + gridZ * gridSizeX;
                                if (CanBeNeighbor(neighborIndex))
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

                        int gridX = grid[i].gridX + offset.x;
                        int gridZ = grid[i].gridZ + offset.y;

                        if (gridX >= 0 && gridX < gridSizeX &&
                            gridZ >= 0 && gridZ < gridSizeZ)
                        {
                            int neighborIndex = gridX + gridZ * gridSizeX;                            
                            if (CanBeNeighbor(neighborIndex))
                            {
                                allNeighbors[baseIndex + count] = neighborIndex;
                                count++;
                            }
                        }
                    }
                }
            }
        }

        private bool CanBeNeighbor(int neighborIndex) => grid[neighborIndex].walkableType == WalkableType.Walkable;
    }
}
