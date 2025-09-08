using NavigationGraph.RaycastCheck;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace NavigationGraph.Graph
{
    internal sealed class SimpleGridNavigationGraph : NavigationGraph
    {
        public SimpleGridNavigationGraph(IRaycastType checkType, NavigationGraphConfig navigationGraphConfig)
        : base(checkType, navigationGraphConfig)
        {
            GraphType = NavigationGraphType.Grid2D;
        }

        protected override void CreateGrid()
        {
            if (grid.IsCreated) grid.Dispose();

            int total = GetGridSize();
            grid = new NativeArray<Cell>(total, Allocator.Persistent);

            // Check cells that are blocked by expanded bounds
            bool[] blockedByBounds = CollectBlockedByExpandedBounds();

            // For the remaining cells, compute if they are walkable
            NativeArray<WalkableType> computedWalkable = ComputeRemainingWalkableCells(blockedByBounds);

            int obstacleRadiusCells = Mathf.CeilToInt(obstacleMargin / cellDiameter);
            int cliffRadiusCells = Mathf.CeilToInt(cliffMargin / cellDiameter);

            var nativeObstacleBlocked = new NativeArray<WalkableType>(total, Allocator.TempJob);
            var nativeCliffBlocked = new NativeArray<WalkableType>(total, Allocator.TempJob);

            var distObstacle = new NativeArray<int>(total, Allocator.TempJob);
            var distCliff = new NativeArray<int>(total, Allocator.TempJob);

            var queueObstacle = new NativeQueue<int>(Allocator.TempJob);
            var queueCliff = new NativeQueue<int>(Allocator.TempJob);

            // Dilate the mask for non-walkable cells.
            JobHandle dilateMaskJob = CombinedDilateMasks(computedWalkable, new int3(gridSize.x, gridSize.y, gridSize.z),
                                                          obstacleRadiusCells, cliffRadiusCells, nativeObstacleBlocked,
                                                          nativeCliffBlocked, distObstacle, distCliff, queueObstacle, queueCliff);

            // Raycast batch arrays
            var commands = new NativeArray<RaycastCommand>(total, Allocator.TempJob);
            var results = new NativeArray<RaycastHit>(total, Allocator.TempJob);

            var checkPointJob = new CheckPointsJob
            {
                commands = commands,
                origin = transform.position,
                cellDiameter = cellDiameter,
                gridSizeX = gridSize.x,
                gridSizeY = gridSize.y,
                walkableMask = walkableMask,
                physicsScene = Physics.defaultPhysicsScene
            }.Schedule(total, 64, dilateMaskJob);

            JobHandle batchHandle = RaycastCommand.ScheduleBatch(commands, results, 64, checkPointJob);

            // TODO: Delete this 'Complete', and pass to multithread the asignment of penalty.
            batchHandle.Complete();

            NativeArray<int> layerPerCell = new NativeArray<int>(total, Allocator.TempJob);

            for (int i = 0; i < total; i++)
            {
                var col = results[i].collider;
                if (col == null) continue;
                var go = col.gameObject;
                if (go == null) continue;

                layerPerCell[i] = go.layer;
            }

            JobHandle createGridJob = new CreateGridJob
            {
                walkableRegionsDic = walkableRegionsDic,
                grid = grid,
                origin = transform.position,
                cellDiameter = cellDiameter,
                gridSizeX = gridSize.x,
                results = results,
                layerPerCell = layerPerCell,
                finalBlocked = nativeObstacleBlocked,
                cliffBlocked = nativeCliffBlocked
            }.Schedule(total, 64, batchHandle);

            allNeighbors = new NativeArray<int>(total * GetNeighborsPerCellCount(), Allocator.Persistent);
            neighborCounts = new NativeArray<int>(total, Allocator.Persistent);

            NativeArray<int2> offsets16 = new NativeArray<int2>(16, Allocator.TempJob);
            offsets16[0] = new int2(-2, 0);
            offsets16[1] = new int2(-2, -1);
            offsets16[2] = new int2(-2, 1);
            offsets16[3] = new int2(-1, -2);
            offsets16[4] = new int2(-1, 2);
            offsets16[5] = new int2(0, -2);
            offsets16[6] = new int2(0, 2);
            offsets16[7] = new int2(1, -2);
            offsets16[8] = new int2(1, 2);
            offsets16[9] = new int2(2, -1);
            offsets16[10] = new int2(2, 0);
            offsets16[11] = new int2(2, 1);
            offsets16[12] = new int2(-1, -1);
            offsets16[13] = new int2(-1, 1);
            offsets16[14] = new int2(1, -1);
            offsets16[15] = new int2(1, 1);

            var neighborsJob = new PrecomputeNeighborsJob
            {
                grid = grid,
                offsets16 = offsets16,
                gridSizeX = gridSize.x,
                gridSizeZ = gridSize.z,
                neighborsPerCell = neighborsPerCell,
                allNeighbors = allNeighbors,
                neighborCounts = neighborCounts
            }.Schedule(createGridJob);

            neighborsJob.Complete();

            distObstacle.Dispose();
            distCliff.Dispose();
            queueObstacle.Dispose();
            queueCliff.Dispose();
            commands.Dispose();
            results.Dispose();
            nativeObstacleBlocked.Dispose();
            nativeCliffBlocked.Dispose();
            computedWalkable.Dispose();
            layerPerCell.Dispose();
            offsets16.Dispose();
        }

        private bool[] CollectBlockedByExpandedBounds()
        {
            int total = gridSize.x * gridSize.z;
            var blocked = new bool[total];

            Vector3 gridWorldSize = new(gridSize.x * cellDiameter, gridSize.y, gridSize.z * cellDiameter);
            Vector3 areaCenter = transform.position + new Vector3(gridWorldSize.x * 0.5f, gridWorldSize.y / 2, gridWorldSize.z * 0.5f);
            Vector3 halfAreaExtents = new(gridWorldSize.x * 0.5f, gridSize.y / 2, gridWorldSize.z * 0.5f);

            Collider[] hits = Physics.OverlapBox(areaCenter, halfAreaExtents, transform.rotation, notWalkableMask.value);

            // For each collider: expand the bounds and paint the rectangle of the indices affected
            foreach (var c in hits)
            {
                Bounds b = c.bounds;

                b.Expand(obstacleMargin * 2f);

                // If it's negative, use the collider bounds
                if (b.size.x <= 0f || b.size.y <= 0f || b.size.z <= 0f)
                {
                    b = c.bounds;
                }

                var (minX, minY) = GetCellsMap(b.min);
                var (maxX, maxY) = GetCellsMap(b.max);

                if (minX > maxX) (maxX, minX) = (minX, maxX);
                if (minY > maxY) (maxY, minY) = (minY, maxY);

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        int idx = x + y * gridSize.x;
                        if (idx >= 0 && idx < total)
                            blocked[idx] = true;
                    }
                }
            }

            return blocked;
        }

        // Returns a new array[gridSize] with true if the cell is walkable or not.
        private NativeArray<WalkableType> ComputeRemainingWalkableCells(bool[] blockedByBounds)
        {
            int total = GetGridSize();
            var computedWalkable = new NativeArray<WalkableType>(total, Allocator.TempJob);

            for (int i = 0; i < total; i++)
            {
                if (blockedByBounds[i])
                {
                    computedWalkable[i] = WalkableType.Obstacle;
                    continue;
                }

                int x = i % gridSize.x;
                int y = i / gridSize.x;

                Vector3 cellPosition = GetCellPositionInWorldMap(x, y);
                WalkableType walkableType = IsCellWalkable(cellPosition);
                computedWalkable[i] = walkableType;
            }

            return computedWalkable;
        }

        // Dilate (BFS) with all non-walkable cells radius steps
        private JobHandle CombinedDilateMasks(NativeArray<WalkableType> computedWalkable, int3 gridSize, int obstacleRadiusCells, int cliffRadiusCells, NativeArray<WalkableType> nativeObstacleBlocked, NativeArray<WalkableType> nativeCliffBlocked, NativeArray<int> distObstacle, NativeArray<int> distCliff, NativeQueue<int> queueObstacle, NativeQueue<int> queueCliff)
        {
            int total = gridSize.x * gridSize.z;

            var initJob = new InitSeedsJob
            {
                computedWalkable = computedWalkable,
                gridSize = gridSize,

                Walkable = WalkableType.Walkable,
                Obstacle = WalkableType.Obstacle,
                Air = WalkableType.Air,

                finalObstacle = nativeObstacleBlocked,
                finalCliff = nativeCliffBlocked,
                distObstacle = distObstacle,
                distCliff = distCliff,
                queueObstacle = queueObstacle.AsParallelWriter(),
                queueCliff = queueCliff.AsParallelWriter()
            }.Schedule(total, 64);

            var bfsJob = new BFSCombinedJob
            {
                gridSize = gridSize,
                obstacleRadius = obstacleRadiusCells,
                cliffRadius = cliffRadiusCells,

                Obstacle = WalkableType.Obstacle,
                Air = WalkableType.Air,

                distObstacle = distObstacle,
                distCliff = distCliff,
                finalObstacle = nativeObstacleBlocked,
                finalCliff = nativeCliffBlocked,
                queueObstacle = queueObstacle,
                queueCliff = queueCliff
            }.Schedule(initJob);

            return bfsJob;
        }

        #region Jobs & Burst

        [BurstCompile]
        public struct InitSeedsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<WalkableType> computedWalkable;
            [ReadOnly] public int3 gridSize;

            [ReadOnly] public WalkableType Walkable;
            [ReadOnly] public WalkableType Obstacle;
            [ReadOnly] public WalkableType Air;

            public NativeArray<WalkableType> finalObstacle;
            public NativeArray<WalkableType> finalCliff;

            public NativeArray<int> distObstacle;
            public NativeArray<int> distCliff;

            public NativeQueue<int>.ParallelWriter queueObstacle;
            public NativeQueue<int>.ParallelWriter queueCliff;

            public void Execute(int i)
            {
                finalObstacle[i] = computedWalkable[i];
                finalCliff[i] = computedWalkable[i];

                distObstacle[i] = -1;
                distCliff[i] = -1;

                int x = i % gridSize.x;
                int y = i / gridSize.x;

                if (computedWalkable[i] == Walkable)
                {
                    bool hasObstacleNeighbor = false;
                    bool hasCliffNeighbor = false;

                    if (x + 1 < gridSize.x)
                    {
                        int n = i + 1;
                        // hasObstacleNeighbor |= computedWalkable[n] == Obstacle;
                        hasCliffNeighbor |= computedWalkable[n] == Air;
                    }
                    if (x - 1 >= 0)
                    {
                        int n = i - 1;
                        // hasObstacleNeighbor |= computedWalkable[n] == Obstacle;
                        hasCliffNeighbor |= computedWalkable[n] == Air;
                    }
                    if (y + 1 < gridSize.z)
                    {
                        int n = i + gridSize.x;
                        // hasObstacleNeighbor |= computedWalkable[n] == Obstacle;
                        hasCliffNeighbor |= computedWalkable[n] == Air;
                    }
                    if (y - 1 >= 0)
                    {
                        int n = i - gridSize.x;
                        // hasObstacleNeighbor |= computedWalkable[n] == Obstacle;
                        hasCliffNeighbor |= computedWalkable[n] == Air;
                    }

                    if (hasObstacleNeighbor)
                    {
                        distObstacle[i] = 0;
                        finalObstacle[i] = Obstacle;
                        queueObstacle.Enqueue(i);
                    }
                    if (hasCliffNeighbor)
                    {
                        distCliff[i] = 0;
                        finalCliff[i] = Air;
                        queueCliff.Enqueue(i);
                    }
                }
            }
        }

        [BurstCompile]
        public struct BFSCombinedJob : IJob
        {
            [ReadOnly] public int3 gridSize;
            [ReadOnly] public int obstacleRadius;
            [ReadOnly] public int cliffRadius;

            [ReadOnly] public WalkableType Obstacle;
            [ReadOnly] public WalkableType Air;

            public NativeArray<int> distObstacle;
            public NativeArray<int> distCliff;

            public NativeArray<WalkableType> finalObstacle;
            public NativeArray<WalkableType> finalCliff;

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
                if (distObstacle[idx] != -1) return;

                distObstacle[idx] = currentDist + 1;
                finalObstacle[idx] = Obstacle;
                queueObstacle.Enqueue(idx);
            }

            private void EnqueueNeighborCliff(int x, int y, int currentDist)
            {
                if (x < 0 || y < 0 || x >= gridSize.x || y >= gridSize.z) return;
                int idx = x + y * gridSize.x;
                if (distCliff[idx] != -1) return;

                distCliff[idx] = currentDist + 1;
                finalCliff[idx] = Air;
                queueCliff.Enqueue(idx);
            }
        }

        [BurstCompile]
        private struct CheckPointsJob : IJobParallelFor
        {
            [WriteOnly] public NativeArray<RaycastCommand> commands;
            public Vector3 origin;
            public float cellDiameter;
            public int gridSizeX;
            public int gridSizeY;
            public int walkableMask;
            public PhysicsScene physicsScene;

            public void Execute(int i)
            {
                int x = i % gridSizeX;
                int y = i / gridSizeX;

                Vector3 cellPosition = origin
                    + Vector3.right * ((x + 0.5f) * cellDiameter)
                    + Vector3.forward * ((y + 0.5f) * cellDiameter);

                var queryParams = new QueryParameters { layerMask = walkableMask };

                commands[i] = new RaycastCommand(physicsScene, cellPosition + Vector3.up * gridSizeY, Vector3.down, queryParams, gridSizeY);
            }
        }

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
            [ReadOnly] public NativeArray<WalkableType> finalBlocked;
            [ReadOnly] public NativeArray<WalkableType> cliffBlocked;

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
                WalkableType cliff = cliffBlocked[index];
                WalkableType finalB = finalBlocked[index];

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

        [BurstCompile]
        public struct PrecomputeNeighborsJob : IJob
        {
            [ReadOnly] public NativeArray<Cell> grid;
            [ReadOnly] public NativeArray<int2> offsets16;
            [WriteOnly] public NativeArray<int> allNeighbors;
            [WriteOnly] public NativeArray<int> neighborCounts;

            public int gridSizeX;
            public int gridSizeZ;
            public NeighborsPerCell neighborsPerCell;

            public void Execute()
            {
                int numCells = grid.Length;
                for (int index = 0; index < numCells; index++)
                {
                    Cell cell = grid[index];
                    int maxNeighbors = neighborsPerCell switch
                    {
                        NeighborsPerCell.Four => 4,
                        NeighborsPerCell.Eight => 8,
                        NeighborsPerCell.Sixteen => 16,
                        _ => 4
                    };

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

                                int gridX = cell.gridX + offsetX;
                                int gridZ = cell.gridZ + offsetZ;

                                if (gridX >= 0 && gridX < gridSizeX &&
                                    gridZ >= 0 && gridZ < gridSizeZ)
                                {
                                    allNeighbors[index * maxNeighbors + count] = gridZ * gridSizeX + gridX;
                                    count++;
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var offset in offsets16)
                        {
                            int gridX = cell.gridX + offset.x;
                            int gridZ = cell.gridZ + offset.y;

                            if (gridX >= 0 && gridX < gridSizeX &&
                                gridZ >= 0 && gridZ < gridSizeZ)
                            {
                                allNeighbors[index * maxNeighbors + count] = gridZ * gridSizeX + gridX;
                                count++;
                            }
                        }
                    }

                    neighborCounts[index] = count;
                }
            }
        }

        #endregion
    }
}
