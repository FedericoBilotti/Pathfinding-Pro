using System;
using NavigationGraph.RaycastCheck;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace NavigationGraph.Graph
{
    internal sealed partial class SimpleGridNavigationGraph : NavigationGraph
    {
        public SimpleGridNavigationGraph(IRaycastType checkType, NavigationGraphConfig navigationGraphConfig)
        : base(checkType, navigationGraphConfig)
        {
            GraphType = NavigationGraphType.Grid2D;
        }

        protected override void LoadGridFromMemory(GridDataAsset gridBaked)
        {
            int totalGridSize = GetGridSizeLength();
            int lengthNeighbors = gridBaked.neighborsCell.neighbors.Length;
            int lengthCounts = gridBaked.neighborsCell.neighborTotalCount.Length;
            int lengthOffsets = gridBaked.neighborsCell.neighborOffsets.Length;

            grid = new NativeArray<Cell>(totalGridSize, Allocator.Persistent);
            neighbors = new NativeArray<int>(lengthNeighbors, Allocator.Persistent);
            neighborTotalCount = new NativeArray<int>(lengthCounts, Allocator.Persistent);
            neighborOffSet = new NativeArray<int>(lengthOffsets, Allocator.Persistent);

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.z; y++)
                {
                    int index = x + y * gridSize.x;
                    CellData actualCell = gridBaked.cells[index];

                    grid[index] = new Cell
                    {
                        position = actualCell.position,
                        height = actualCell.height,
                        gridIndex = actualCell.gridIndex,
                        gridX = actualCell.gridX,
                        gridZ = actualCell.gridZ,
                        cellCostPenalty = actualCell.cellCostPenalty,
                        walkableType = actualCell.walkableType
                    };
                }
            }

            for (int i = 0; i < lengthNeighbors; i++)
                neighbors[i] = gridBaked.neighborsCell.neighbors[i];

            for (int i = 0; i < lengthCounts; i++)
                neighborTotalCount[i] = gridBaked.neighborsCell.neighborTotalCount[i];

            for (int i = 0; i < lengthOffsets; i++)
                neighborOffSet[i] = gridBaked.neighborsCell.neighborOffsets[i];
        }

        public override void CreateGrid()
        {
            // --- 0. Clean ---
            OnCreateGrid?.Invoke();

            if (grid.IsCreated) grid.Dispose();
            if (neighbors.IsCreated) neighbors.Dispose();
            if (neighborOffSet.IsCreated) neighborOffSet.Dispose();
            if (neighborTotalCount.IsCreated) neighborTotalCount.Dispose();

            int totalGridSize = GetGridSizeLength();
            grid = new NativeArray<Cell>(totalGridSize, Allocator.Persistent);

            // --- 1. PREPARE RAYCASTS ---
            var commands = new NativeArray<RaycastCommand>(totalGridSize, Allocator.TempJob);
            var results = new NativeArray<RaycastHit>(totalGridSize, Allocator.TempJob);

            var prepareCmdJob = new PrepareRaycastCommandsJob
            {
                commands = commands,
                origin = transform.position,
                cellDiameter = cellDiameter,
                gridSizeX = gridSize.x,
                gridSizeY = gridSize.y,
                walkableMask = walkableMask,
                physicsScene = Physics.defaultPhysicsScene
            }.Schedule(totalGridSize, 64);

            JobHandle batchHandle = RaycastCommand.ScheduleBatch(commands, results, 64, prepareCmdJob);
            batchHandle.Complete();

            // --- 2. BUILD WALKABLE + ALTURA ---
            var normalWalkable = new NativeArray<Vector3>(totalGridSize, Allocator.TempJob);
            var layerPerCell = new NativeArray<int>(totalGridSize, Allocator.TempJob);
            var groundHeight = new NativeArray<float>(totalGridSize, Allocator.TempJob);
            var computedWalkable = new NativeArray<WalkableType>(totalGridSize, Allocator.TempJob);

            for (int i = 0; i < totalGridSize; i++)
            {
                var hit = results[i];
                if (hit.collider == null)
                {
                    groundHeight[i] = float.MinValue;
                    computedWalkable[i] = WalkableType.Air;
                }
                else
                {
                    var go = hit.collider.gameObject;
                    var layer = go.layer;
                    bool isWalkable = ((1 << layer) & walkableMask) != 0;      
                    normalWalkable[i] = hit.normal;
                    computedWalkable[i] = isWalkable ? WalkableType.Walkable : WalkableType.Obstacle;
                    layerPerCell[i] = layer;

#if UNITY_EDITOR
                    // This is only for debug and seeing the point in the grid.
                    if (!isWalkable)
                    {
                        bool walkable = Physics.Raycast(hit.point, Vector3.down, out var newHit, 999f, walkableMask);

                        if (walkable)
                        {
                            groundHeight[i] = newHit.point.y;
                            results[i] = newHit;
                            continue;
                        }
                    }
#endif

                    groundHeight[i] = hit.point.y;
                }
            }

            // --- 3. CALCULATE MARGIN (DILATE) ---
            var nativeObstacleBlocked = new NativeArray<WalkableType>(totalGridSize, Allocator.TempJob);
            var nativeCliffBlocked = new NativeArray<WalkableType>(totalGridSize, Allocator.TempJob);

            var distObstacle = new NativeArray<int>(totalGridSize, Allocator.TempJob);
            var distCliff = new NativeArray<int>(totalGridSize, Allocator.TempJob);
            var queueObstacle = new NativeQueue<int>(Allocator.TempJob);
            var queueCliff = new NativeQueue<int>(Allocator.TempJob);

            JobHandle initJob = new InitSeedsJob
            {
                gridSize = gridSize,
                normalWalkable = normalWalkable,
                inclineLimit = inclineLimit,
                computedWalkable = computedWalkable,
                groundHeight = groundHeight,
                maxHeightDifference = maxHeightDifference,

                finalObstacle = nativeObstacleBlocked,
                finalCliff = nativeCliffBlocked,

                distObstacle = distObstacle,
                distCliff = distCliff,

                queueObstacle = queueObstacle.AsParallelWriter(),
                queueCliff = queueCliff.AsParallelWriter()
            }.Schedule(totalGridSize, 64);

            int obstacleRadiusCells = Mathf.CeilToInt(obstacleMargin / cellDiameter);
            int cliffRadiusCells = Mathf.CeilToInt(cliffMargin / cellDiameter);

            JobHandle bfsJob = new BFSCombinedJob
            {
                gridSize = gridSize,
                obstacleRadius = obstacleRadiusCells,
                cliffRadius = cliffRadiusCells,

                distObstacle = distObstacle,
                distCliff = distCliff,

                nativeObstacleBlocked = nativeObstacleBlocked,
                nativeCliffBlocked = nativeCliffBlocked,

                queueObstacle = queueObstacle,
                queueCliff = queueCliff
            }.Schedule(initJob);

            // --- 4. CREATE CELLS IN GRID ---
            var createGridJob = new CreateGridJob
            {
                walkableRegionsDic = walkableRegionsDic,
                grid = grid,
                origin = transform.position,
                right = new float3(1, 0, 0),
                forward = new float3(0, 0, 1),

                cellDiameter = cellDiameter,
                gridSizeX = gridSize.x,

                results = results,
                layerPerCell = layerPerCell,
                nativeObstacleBlocked = nativeObstacleBlocked,
                nativeCliffBlocked = nativeCliffBlocked
            }.Schedule(totalGridSize, 64, bfsJob);

            // --- 5. PRECOMPUTE NEIGHBORS ---
            NativeArray<int2> offsets16 = new(16, Allocator.TempJob);
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

            var temporaryNeighborTotalCount = new NativeArray<int>(totalGridSize, Allocator.TempJob);

            var countNeighborsJob = new CountNeighborsJob
            {
                grid = grid,
                offsets16 = offsets16,
                neighborCounts = temporaryNeighborTotalCount,
                gridSizeX = gridSize.x,
                gridSizeZ = gridSize.z,
                maxHeightDifference = maxHeightDifference,
                neighborsPerCell = neighborsPerCell
            }.Schedule(createGridJob);

            countNeighborsJob.Complete();

            int totalLengthOfNeighbors = 0;
            for (int i = 0; i < temporaryNeighborTotalCount.Length; i++)
                totalLengthOfNeighbors += temporaryNeighborTotalCount[i];

            neighborTotalCount = new NativeArray<int>(totalGridSize, Allocator.Persistent);
            for (int i = 0; i < temporaryNeighborTotalCount.Length; i++)
                neighborTotalCount[i] = temporaryNeighborTotalCount[i];

            neighborOffSet = new NativeArray<int>(totalGridSize, Allocator.Persistent);
            int offset = 0;
            for (int i = 0; i < totalGridSize; i++)
            {
                neighborOffSet[i] = offset;
                offset += neighborTotalCount[i];
            }

            neighbors = new NativeArray<int>(totalLengthOfNeighbors, Allocator.Persistent);

            var neighborsJob = new PrecomputeNeighborsJob
            {
                grid = grid,
                offsets16 = offsets16,
                gridSizeX = gridSize.x,
                gridSizeZ = gridSize.z,
                neighborsPerCell = neighborsPerCell,
                allNeighbors = neighbors,
                maxHeightDifference = maxHeightDifference,
                neighborCounts = neighborTotalCount,
                neighborOffsets = neighborOffSet
            }.Schedule();

            neighborsJob.Complete();

            // --- 6. CLEANUP ---
            commands.Dispose();
            results.Dispose();

            normalWalkable.Dispose();
            computedWalkable.Dispose();
            layerPerCell.Dispose();
            groundHeight.Dispose();

            nativeObstacleBlocked.Dispose();
            nativeCliffBlocked.Dispose();
            distObstacle.Dispose();
            distCliff.Dispose();
            queueObstacle.Dispose();
            queueCliff.Dispose();
            
            offsets16.Dispose();
            temporaryNeighborTotalCount.Dispose();
        }
    }
}