using System.Collections;
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
        public SimpleGridNavigationGraph(float cellSize, float maxDistance, Vector2Int gridSize,
                LayerMask notWalkableMask, Transform transform, LayerMask walkableMask, LayerMask agentMask, float obstacleMargin, float cliffMargin)
            : base(cellSize, maxDistance, gridSize, notWalkableMask, transform, walkableMask, agentMask, obstacleMargin, cliffMargin)
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
            NativeArray<int> computedWalkable = ComputeRemainingWalkableCells(blockedByBounds);

            int obstacleRadiusCells = Mathf.CeilToInt(obstacleMargin / cellDiameter);
            int airRadiusCells = Mathf.CeilToInt(cliffMargin / cellDiameter);
            if (obstacleRadiusCells == 0) obstacleRadiusCells = 1;
            if (airRadiusCells == 0) airRadiusCells = 1;

            var nativeObstacleBlocked = new NativeArray<int>(total, Allocator.TempJob);
            var nativeCliffBlocked = new NativeArray<int>(total, Allocator.TempJob);

            var distObstacle = new NativeArray<int>(total, Allocator.TempJob);
            var distCliff = new NativeArray<int>(total, Allocator.TempJob);

            var queueObstacle = new NativeQueue<int>(Allocator.TempJob);
            var queueCliff = new NativeQueue<int>(Allocator.TempJob);

            // Dilate the mask for non-walkable cells.
            var dilateMaskJob = CombinedDilateMasks(computedWalkable, new int2(gridSize.x, gridSize.y), obstacleRadiusCells, airRadiusCells, nativeObstacleBlocked, nativeCliffBlocked, distObstacle, distCliff, queueObstacle, queueCliff);

            // Raycast batch arrays
            NativeArray<RaycastCommand> commands = new(total, Allocator.TempJob);
            NativeArray<RaycastHit> results = new(total, Allocator.TempJob);

            var prepareJob = new CheckPointsJob
            {
                commands = commands,
                origin = transform.position,
                cellDiameter = cellDiameter,
                gridSizeX = gridSize.x,
                gridSizeY = gridSize.y,
                maxDistance = maxDistance,
                walkableMask = walkableMask,
                physicsScene = Physics.defaultPhysicsScene
            };

            JobHandle prepareHandle = prepareJob.Schedule(total, 32, dilateMaskJob);

            JobHandle batchHandle = RaycastCommand.ScheduleBatch(commands, results, 32, prepareHandle);

            JobHandle createGridJob = new CreateGridJob
            {
                grid = grid,
                origin = transform.position,
                cellDiameter = cellDiameter,
                total = total,
                gridSizeX = gridSize.x,
                gridSizeY = gridSize.y,
                results = results,
                finalBlocked = nativeObstacleBlocked,
                cliffBlocked = nativeCliffBlocked
            }.Schedule(batchHandle);

            var neighborsPerCell = new NativeArray<FixedList32Bytes<int>>(grid.Length, Allocator.TempJob);

            var neighborsJob = new PrecomputeNeighborsJob
            {
                grid = grid,
                gridSizeX = gridSize.x,
                gridSizeZ = gridSize.y,
                neighborsPerCell = neighborsPerCell
            }.Schedule(grid.Length, 32, createGridJob);

            neighborsJob.Complete();

            distObstacle.Dispose();
            distCliff.Dispose();
            queueObstacle.Dispose();
            queueCliff.Dispose();
            neighborsPerCell.Dispose();
            commands.Dispose();
            results.Dispose();
            nativeObstacleBlocked.Dispose();
            nativeCliffBlocked.Dispose();
            computedWalkable.Dispose();
        }

        private bool[] CollectBlockedByExpandedBounds()
        {
            int total = gridSize.x * gridSize.y;
            var blocked = new bool[total];

            Vector3 gridWorldSize = new(gridSize.x * cellDiameter, maxDistance, gridSize.y * cellDiameter);
            Vector3 areaCenter = transform.position + new Vector3(gridWorldSize.x * 0.5f, gridWorldSize.y / 2, gridWorldSize.z * 0.5f);
            Vector3 halfAreaExtents = new(gridWorldSize.x * 0.5f, maxDistance / 2, gridWorldSize.z * 0.5f);

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
        private NativeArray<int> ComputeRemainingWalkableCells(bool[] blockedByBounds)
        {
            int total = GetGridSize();
            var computedWalkable = new NativeArray<int>(total, Allocator.TempJob);

            for (int i = 0; i < total; i++)
            {
                if (blockedByBounds[i])
                {
                    computedWalkable[i] = (int)WalkableType.Obstacle;
                    continue;
                }

                int x = i % gridSize.x;
                int y = i / gridSize.x;

                Vector3 cellPosition = GetCellPositionInWorldMap(x, y);
                WalkableType walkableType = IsCellWalkable(cellPosition, cellSize);
                computedWalkable[i] = (int)walkableType;
            }

            return computedWalkable;
        }

        // Dilate (BFS) with all non-walkable cells radius steps
        private JobHandle CombinedDilateMasks(NativeArray<int> computedWalkable, int2 gridSize, int obstacleRadiusCells, int cliffRadiusCells, NativeArray<int> finalObstacle, NativeArray<int> finalCliff, NativeArray<int> distObstacle, NativeArray<int> distCliff, NativeQueue<int> queueObstacle, NativeQueue<int> queueCliff)
        {
            int total = gridSize.x * gridSize.y;

            var initJob = new InitSeedsJob
            {
                computedWalkable = computedWalkable,
                gridSize = gridSize,

                Walkable = 0,
                Obstacle = 1,
                Air = 2,

                finalObstacle = finalObstacle,
                finalCliff = finalCliff,
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

                Obstacle = 1,
                Air = 2,

                distObstacle = distObstacle,
                distCliff = distCliff,
                finalObstacle = finalObstacle,
                finalCliff = finalCliff,
                queueObstacle = queueObstacle,
                queueCliff = queueCliff
            }.Schedule(initJob);

            return bfsJob;
        }

        #region Jobs & Burst

        [BurstCompile]
        public struct InitSeedsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> computedWalkable;
            [ReadOnly] public int2 gridSize;

            [ReadOnly] public int Walkable;
            [ReadOnly] public int Obstacle;
            [ReadOnly] public int Air;

            public NativeArray<int> finalObstacle;
            public NativeArray<int> finalCliff;

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
                        hasObstacleNeighbor |= computedWalkable[n] == Obstacle;
                        hasCliffNeighbor |= computedWalkable[n] == Air;
                    }
                    if (x - 1 >= 0)
                    {
                        int n = i - 1;
                        hasObstacleNeighbor |= computedWalkable[n] == Obstacle;
                        hasCliffNeighbor |= computedWalkable[n] == Air;
                    }
                    if (y + 1 < gridSize.y)
                    {
                        int n = i + gridSize.x;
                        hasObstacleNeighbor |= computedWalkable[n] == Obstacle;
                        hasCliffNeighbor |= computedWalkable[n] == Air;
                    }
                    if (y - 1 >= 0)
                    {
                        int n = i - gridSize.x;
                        hasObstacleNeighbor |= computedWalkable[n] == Obstacle;
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
            [ReadOnly] public int2 gridSize;
            [ReadOnly] public int obstacleRadius;
            [ReadOnly] public int cliffRadius;

            [ReadOnly] public int Obstacle;
            [ReadOnly] public int Air;

            public NativeArray<int> distObstacle;
            public NativeArray<int> distCliff;

            public NativeArray<int> finalObstacle;
            public NativeArray<int> finalCliff;

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

                    // Paso de CLIFFS
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
                if (x < 0 || y < 0 || x >= gridSize.x || y >= gridSize.y) return;
                int idx = x + y * gridSize.x;
                if (distObstacle[idx] != -1) return;

                distObstacle[idx] = currentDist + 1;
                finalObstacle[idx] = Obstacle;
                queueObstacle.Enqueue(idx);
            }

            private void EnqueueNeighborCliff(int x, int y, int currentDist)
            {
                if (x < 0 || y < 0 || x >= gridSize.x || y >= gridSize.y) return;
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
            public NativeArray<RaycastCommand> commands;
            public Vector3 origin;
            public float cellDiameter;
            public int gridSizeX;
            public int gridSizeY;
            public float maxDistance;
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

                commands[i] = new RaycastCommand(physicsScene, cellPosition + Vector3.up * maxDistance, Vector3.down, queryParams, maxDistance);
            }
        }

        [BurstCompile]
        private struct CreateGridJob : IJob
        {
            public NativeArray<Cell> grid;
            public Vector3 origin;
            public float cellDiameter;
            public int total;
            public int gridSizeX;
            public int gridSizeY;

            [ReadOnly] public NativeArray<RaycastHit> results;
            [ReadOnly] public NativeArray<int> finalBlocked;
            [ReadOnly] public NativeArray<int> cliffBlocked;

            public void Execute()
            {
                const float kHitEpsilon = 1e-4f;

                for (int i = 0; i < total; i++)
                {
                    int x = i % gridSizeX;
                    int y = i / gridSizeX;

                    Vector3 defaultPos = origin
                        + Vector3.right * ((x + 0.5f) * cellDiameter)
                        + Vector3.forward * ((y + 0.5f) * cellDiameter);

                    bool hit = results[i].distance > kHitEpsilon;
                    Vector3 cellPosition = hit ? results[i].point : defaultPos;

                    bool isWalkable = (finalBlocked[i] == (int)WalkableType.Walkable)
                                   && (cliffBlocked[i] == (int)WalkableType.Walkable);

                    grid[i] = new Cell
                    {
                        position = cellPosition,
                        gridIndex = i,
                        gridX = x,
                        gridZ = y,
                        isWalkable = isWalkable,
                    };
                }
            }
        }

        #endregion

        #region Gizmos

        public override void DrawGizmos()
        {
            base.DrawGizmos();

            Vector3 gridWorldSize = new(gridSize.x * cellDiameter, maxDistance, gridSize.y * cellDiameter);
            Vector3 areaCenter = transform.position + new Vector3(gridWorldSize.x * 0.5f, gridWorldSize.y / 2, gridWorldSize.z * 0.5f);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(areaCenter, gridWorldSize);
        }

        #endregion
    }
}
