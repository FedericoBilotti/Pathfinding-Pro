using System.Collections.Generic;
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
            WalkableType[] computedWalkable = ComputeRemainingWalkableCells(blockedByBounds);

            // Dilate the mask for non-walkable cells according to obstacle margin converted to cells
            int obstacleRadiusCells = Mathf.CeilToInt(obstacleMargin / cellDiameter);
            if (obstacleRadiusCells == 0 && obstacleMargin > 0f) obstacleRadiusCells = 1; // At least one
            WalkableType[] finalBlocked = DilateBlockedMask(computedWalkable, obstacleRadiusCells, WalkableType.Obstacle);

            // Dilate the mask for non-walkable cells according to cliff margin converted to cells
            int airRadiusCells = Mathf.CeilToInt(cliffMargin / cellDiameter);
            if (airRadiusCells == 0 && cliffMargin > 0f) airRadiusCells = 1; // At least one
            WalkableType[] cliffBlocked = DilateBlockedMask(computedWalkable, airRadiusCells, WalkableType.Air);

            var nativeFinalBlocked = new NativeArray<int>(total, Allocator.TempJob);
            var nativeCliffBlocked = new NativeArray<int>(total, Allocator.TempJob);

            for (int i = 0; i < total; i++)
            {
                nativeFinalBlocked[i] = (int)finalBlocked[i];
                nativeCliffBlocked[i] = (int)cliffBlocked[i];
            }

            // Raycast batch arrays
            NativeArray<RaycastCommand> commands = new(total, Allocator.TempJob);
            NativeArray<RaycastHit> results = new(total, Allocator.TempJob);

            var prepareJob = new PrepareRaycastCommandsJob
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

            JobHandle prepareHandle = prepareJob.Schedule(total, 32);

            JobHandle batchHandle = RaycastCommand.ScheduleBatch(commands, results, 32, prepareHandle);

            var createGridJob = new CreateGridJob
            {
                grid = grid,
                origin = transform.position,
                cellDiameter = cellDiameter,
                total = total,
                gridSizeX = gridSize.x,
                gridSizeY = gridSize.y,
                results = results,
                finalBlocked = nativeFinalBlocked,
                cliffBlocked = nativeCliffBlocked
            };

            JobHandle createHandle = createGridJob.Schedule(batchHandle);

            var neighborsPerCell = new NativeArray<FixedList32Bytes<int>>(grid.Length, Allocator.TempJob);

            var neighborsJob = new PrecomputeNeighborsJob
            {
                grid = grid,
                gridSizeX = gridSize.x,
                gridSizeZ = gridSize.y,
                neighborsPerCell = neighborsPerCell
            }.Schedule(grid.Length, 32, createHandle);

            neighborsJob.Complete();

            neighborsPerCell.Dispose();
            commands.Dispose();
            results.Dispose();
            nativeFinalBlocked.Dispose();
            nativeCliffBlocked.Dispose();
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
        private WalkableType[] ComputeRemainingWalkableCells(bool[] blockedByBounds)
        {
            int total = GetGridSize();
            var computedWalkable = new WalkableType[total];

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
                WalkableType walkableType = IsCellWalkable(cellPosition, cellSize);
                computedWalkable[i] = walkableType;
            }

            return computedWalkable;
        }

        // Dilate (BFS) with all non-walkable-air cells airRadiusCells steps, and return finalBlocked[] (WalkableType)
        private WalkableType[] DilateBlockedMask(WalkableType[] computedWalkable, int airRadiusCells, WalkableType walkableType)
        {
            int total = GetGridSize();
            var finalBlocked = new WalkableType[total];

            for (int i = 0; i < total; i++)
                finalBlocked[i] = computedWalkable[i];

            if (airRadiusCells <= 0)
                return finalBlocked;

            var queue = new Queue<int>(total / 2);
            var distances = new int[total];
            for (int i = 0; i < total; i++)
                distances[i] = -1;

            for (int i = 0; i < total; i++)
            {
                if (computedWalkable[i] != WalkableType.Walkable) continue;

                int x = i % gridSize.x;
                int y = i / gridSize.x;

                bool hasAirNeighbor = false;

                if (x + 1 < gridSize.x && computedWalkable[x + 1 + y * gridSize.x] == walkableType)
                    hasAirNeighbor = true;

                if (x - 1 >= 0 && computedWalkable[x - 1 + y * gridSize.x] == walkableType)
                    hasAirNeighbor = true;

                if (y + 1 < gridSize.y && computedWalkable[x + (y + 1) * gridSize.x] == walkableType)
                    hasAirNeighbor = true;

                if (y - 1 >= 0 && computedWalkable[x + (y - 1) * gridSize.x] == walkableType)
                    hasAirNeighbor = true;

                if (hasAirNeighbor)
                {
                    distances[i] = 0;
                    queue.Enqueue(i);
                    finalBlocked[i] = walkableType;
                }
            }

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                int currentX = current % gridSize.x;
                int currentY = current / gridSize.x;
                int currentDistance = distances[current];

                if (currentDistance >= airRadiusCells) continue;

                TryEnqueueNeighbor(currentX + 1, currentY, currentDistance, distances, queue, finalBlocked, walkableType);
                TryEnqueueNeighbor(currentX - 1, currentY, currentDistance, distances, queue, finalBlocked, walkableType);
                TryEnqueueNeighbor(currentX, currentY + 1, currentDistance, distances, queue, finalBlocked, walkableType);
                TryEnqueueNeighbor(currentX, currentY - 1, currentDistance, distances, queue, finalBlocked, walkableType);
            }

            return finalBlocked;
        }

        private void TryEnqueueNeighbor(int neighborX, int neighborY, int currentDistance, int[] distances, Queue<int> queue, WalkableType[] finalBlocked, WalkableType walkableType)
        {
            if (neighborX < 0 || neighborY < 0 || neighborX >= gridSize.x || neighborY >= gridSize.y) return;

            int neighborIndex = neighborX + neighborY * gridSize.x;
            if (distances[neighborIndex] != -1) return;

            distances[neighborIndex] = currentDistance + 1;
            finalBlocked[neighborIndex] = walkableType;
            queue.Enqueue(neighborIndex);
        }

        #region Jobs & Burst

        [BurstCompile]
        public struct PrepareRaycastCommandsJob : IJobParallelFor
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
