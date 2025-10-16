using NavigationGraph.Graph.Planar.Jobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace NavigationGraph.Graph.Planar
{
    public sealed class PlanarGrid : GraphNavigation
    {
        private int _visitId = 0;
        private readonly int[] _visited;
        private readonly Queue<Vector2Int> _queue;
        private readonly ILoadGraph _gridLoader;

        public PlanarGrid(NavigationGraphConfig navigationGraphConfig,
            ILoadGraphFactory graphFactory)
        : base(navigationGraphConfig)
        {
            GraphType = NavigationGraphType.Grid2D;

            var totalGridSize = GridSize.x * GridSize.z;
            _visited = new int[totalGridSize];
            _queue = new Queue<Vector2Int>(GridSize.x * GridSize.z);
                        
            _gridLoader = graphFactory.CreateLoadGraph(GraphLoadType.Memory, this);
        }

        public override void LoadGrid(IGraphDataAsset graphDataAsset)
        {
            _gridLoader.LoadGraph(graphDataAsset);
        }

        public override void CreateGrid()
        {
            // --- 0. Clean ---
            OnCreateGrid?.Invoke();

            if (Graph.IsCreated) Graph.Dispose();
            if (Neighbors.IsCreated) Neighbors.Dispose();
            if (NeighborOffsets.IsCreated) NeighborOffsets.Dispose();
            if (NeighborTotalCount.IsCreated) NeighborTotalCount.Dispose();

            int totalGridSize = GetGridSizeLength();
            graph = new NativeArray<Node>(totalGridSize, Allocator.Persistent);

            // --- 1. PREPARE RAYCASTS ---
            var commands = new NativeArray<RaycastCommand>(totalGridSize, Allocator.TempJob);
            var results = new NativeArray<RaycastHit>(totalGridSize, Allocator.TempJob);

            var prepareCmdJob = new PrepareRaycastCommandsJob
            {
                commands = commands,
                origin = transform.position,
                cellDiameter = CellDiameter,
                ignoreMasks = ignoreMasksAtCreateGrid,
                gridSizeX = GridSize.x,
                gridSizeY = GridSize.y,
                walkableMask = WalkableMask,
                physicsScene = Physics.defaultPhysicsScene
            }.Schedule(totalGridSize, 64);

            JobHandle batchHandle = RaycastCommand.ScheduleBatch(commands, results, 64, prepareCmdJob);
            batchHandle.Complete();

            // --- 2. BUILD WALKABLE + HEIGHT ---
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
                    bool isWalkable = ((1 << layer) & WalkableMask) != 0;
                    normalWalkable[i] = hit.normal;
                    computedWalkable[i] = isWalkable ? WalkableType.Walkable : WalkableType.Obstacle;
                    layerPerCell[i] = layer;

#if UNITY_EDITOR
                    // This is only for debug and seeing the point in the grid.
                    if (!isWalkable)
                    {
                        bool walkable = Physics.Raycast(hit.point, Vector3.down, out var newHit, 999f, WalkableMask);

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
                gridSize = GridSize,
                normalWalkable = normalWalkable,
                inclineLimit = inclineLimit,
                computedWalkable = computedWalkable,
                groundHeight = groundHeight,
                maxHeightDifference = MAX_HEIGHT_DISTANCE,

                finalObstacle = nativeObstacleBlocked,
                finalCliff = nativeCliffBlocked,

                distObstacle = distObstacle,
                distCliff = distCliff,

                queueObstacle = queueObstacle.AsParallelWriter(),
                queueCliff = queueCliff.AsParallelWriter()
            }.Schedule(totalGridSize, 64);

            int obstacleRadiusCells = Mathf.CeilToInt(obstacleMargin / CellDiameter);
            int cliffRadiusCells = Mathf.CeilToInt(cliffMargin / CellDiameter);

            JobHandle bfsJob = new BFSCombinedJob
            {
                gridSize = GridSize,
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
                grid = Graph,
                origin = transform.position,
                right = new float3(1, 0, 0),
                forward = new float3(0, 0, 1),

                cellDiameter = CellDiameter,
                gridSizeX = GridSize.x,

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
                grid = Graph,
                offsets16 = offsets16,
                neighborCounts = temporaryNeighborTotalCount,
                gridSizeX = GridSize.x,
                gridSizeZ = GridSize.z,
                maxHeightDifference = MAX_HEIGHT_DISTANCE,
                neighborsPerCell = neighborsPerCell
            }.Schedule(createGridJob);

            countNeighborsJob.Complete();

            int totalLengthOfNeighbors = 0;
            for (int i = 0; i < temporaryNeighborTotalCount.Length; i++)
                totalLengthOfNeighbors += temporaryNeighborTotalCount[i];

            neighborTotalCount = new NativeArray<int>(totalGridSize, Allocator.Persistent);
            for (int i = 0; i < temporaryNeighborTotalCount.Length; i++)
                neighborTotalCount[i] = temporaryNeighborTotalCount[i];

            neighborOffsets = new NativeArray<int>(totalGridSize, Allocator.Persistent);
            int offset = 0;
            for (int i = 0; i < totalGridSize; i++)
            {
                neighborOffsets[i] = offset;
                offset += NeighborTotalCount[i];
            }

            neighbors = new NativeArray<int>(totalLengthOfNeighbors, Allocator.Persistent);

            var neighborsJob = new PrecomputeNeighborsJob
            {
                grid = Graph,
                offsets16 = offsets16,
                gridSizeX = GridSize.x,
                gridSizeZ = GridSize.z,
                normalWalkable = normalWalkable,
                groundHeight = groundHeight,
                //inclineLimit = inclineLimit,
                neighborsPerCell = neighborsPerCell,
                allNeighbors = Neighbors,
                maxHeightDifference = MAX_HEIGHT_DISTANCE,
                neighborCounts = NeighborTotalCount,
                neighborOffsets = NeighborOffsets
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

        public override Vector3 TryGetNearestWalkableNode(Vector3 worldPosition)
        {
            var (startX, startY) = GetNodesMap(worldPosition);

            _visitId++;

            _queue.Clear();
            _queue.Enqueue(new Vector2Int(startX, startY));

            while (_queue.Count > 0)
            {
                var current = _queue.Dequeue();
                int x = current.x;
                int y = current.y;

                if (x < 0 || x >= GridSize.x || y < 0 || y >= GridSize.z)
                    continue;

                int index = x + y * GridSize.x;

                if (_visited[index] == _visitId)
                    continue;

                _visited[index] = _visitId;

                if (Graph[index].walkableType == WalkableType.Walkable)
                {
                    return GetCellPositionInWorldMap(x, y);
                }

                _queue.Enqueue(new Vector2Int(x + 1, y));
                _queue.Enqueue(new Vector2Int(x - 1, y));
                _queue.Enqueue(new Vector2Int(x, y + 1));
                _queue.Enqueue(new Vector2Int(x, y - 1));
            }

            return transform.position;
        }

        public override Node GetNode(Vector3 worldPosition)
        {
            var (x, y) = GetNodesMap(worldPosition);

            return Graph[x + y * GridSize.x];
        }

        private (int x, int y) GetNodesMap(Vector3 worldPosition)
        {
            Vector3 gridPos = worldPosition - transform.position;

            int x = Mathf.Clamp(Mathf.FloorToInt(gridPos.x / CellDiameter), 0, GridSize.x - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(gridPos.z / CellDiameter), 0, GridSize.z - 1);

            return (x, y);
        }

        private Vector3 GetCellPositionInWorldMap(int gridX, int gridY)
        {
            Vector3 cellPosition = GetCellPositionInGrid(gridX, gridY);

            return CheckPoint(cellPosition);
        }

        private Vector3 GetCellPositionInGrid(int gridX, int gridY)
        {
            return transform.position
                   + Vector3.right * ((gridX + 0.5f) * CellDiameter)
                   + Vector3.forward * ((gridY + 0.5f) * CellDiameter);
        }

        private Vector3 CheckPoint(Vector3 cellPosition)
        {
            return Physics.Raycast(cellPosition + Vector3.up * GridSize.y,
                    Vector3.down, out RaycastHit raycastHit, GridSize.y, WalkableMask)
                    ? raycastHit.point
                    : cellPosition;
        }

        public override bool? DrawGizmos()
        {
            if (!Graph.IsCreated || Graph.Length == 0) return false;

            Vector3 sizeCell = new Vector3(0.99f, 0.05f, 0.99f) * CellDiameter;

            var walkableColor = new Color(0, 1, 0.5f, 0.5f);
            var nonWalkableSize = new Vector3(0.2f, 0.2f, 0.2f);

            for (int i = 0; i < Graph.Length; i++)
            {
                var node = Graph[i];
                if (node.walkableType == WalkableType.Air) continue;

                Vector3 drawPos = node.position;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, node.normal);
                Matrix4x4 oldMatrix = Gizmos.matrix;

                Gizmos.matrix = Matrix4x4.TRS(drawPos + Vector3.up * 0.025f, rotation, Vector3.one);

                if (node.walkableType == WalkableType.Walkable)
                {
                    Gizmos.color = walkableColor;
                    Gizmos.DrawWireCube(Vector3.zero, sizeCell);
                }
                else
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(Vector3.zero, nonWalkableSize);
                }

                Gizmos.matrix = oldMatrix;
            }

            return true;
        }
    }
}