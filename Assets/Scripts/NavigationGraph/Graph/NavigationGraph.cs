using System.Collections.Generic;
using NavigationGraph.RaycastCheck;
using Unity.Collections;
using UnityEngine;

namespace NavigationGraph
{
    internal abstract class NavigationGraph : INavigationGraph
    {
        private readonly bool[] _visited;
        private readonly Queue<Vector2Int> _queue;

        protected readonly IRaycastType checkType;
        protected readonly LayerMask notWalkableMask;
        protected TerrainType[] terrainTypes;
        protected Vector3Int gridSize;
        protected LayerMask walkableMask;

        protected readonly Transform transform;

        protected NativeArray<Cell> grid;
        protected NativeArray<int> neighbors;
        protected NativeArray<int> neighborTotalCount;
        protected NativeArray<int> neighborOffSet;
        protected NativeHashMap<int, int> walkableRegionsDic;

        protected NeighborsPerCell neighborsPerCell;

        protected float cellSize;
        protected float cellDiameter;
        protected float maxHeightDifference;

        protected float obstacleMargin;
        protected float cliffMargin;

        public NavigationGraphType GraphType { get; protected set; }

        protected NavigationGraph(IRaycastType checkType, NavigationGraphConfig navigationGraphConfig)
        {
            this.checkType = checkType;
            terrainTypes = navigationGraphConfig.terrainTypes;
            cellSize = navigationGraphConfig.cellSize;
            gridSize = navigationGraphConfig.gridSize;
            notWalkableMask = navigationGraphConfig.notWalkableMask;
            transform = navigationGraphConfig.transform;
            obstacleMargin = navigationGraphConfig.obstacleMargin;
            cliffMargin = navigationGraphConfig.cliffMargin;
            neighborsPerCell = navigationGraphConfig.neighborsPerCell;
            maxHeightDifference = navigationGraphConfig.maxHeightDifference;

            _visited = new bool[gridSize.x * gridSize.z];
            _queue = new Queue<Vector2Int>(gridSize.x * gridSize.z);
        }

        protected abstract void CreateGrid();

        public NativeArray<Cell> GetGrid() => grid;
        public Cell GetRandomCell() => grid[Random.Range(0, grid.Length)];
        public float GetCellSize() => cellSize;
        public float GetCellDiameter() => cellDiameter;
        public int GetGridSize() => gridSize.x * gridSize.z;
        public int GetXSize() => gridSize.x;

        public NativeArray<int> GetNeighbors() => neighbors;
        public NativeArray<int> GetNeighborCounts() => neighborTotalCount;
        public NativeArray<int> GetNeighborsOffSet() => neighborOffSet;

        public virtual Cell GetCellWithWorldPosition(Vector3 worldPosition)
        {
            var (x, y) = GetCellsMap(worldPosition);

            return grid[x + y * gridSize.x];
        }

        public virtual bool IsInGrid(Vector3 worldPosition)
        {
            Vector3 gridPos = worldPosition - transform.position;

            int x = Mathf.FloorToInt(gridPos.x / cellDiameter);
            int y = Mathf.FloorToInt(gridPos.z / cellDiameter);

            if (x < 0 || x >= gridSize.x || y < 0 || y >= gridSize.z) return false;

            int gridIndex = x + y * gridSize.x;
            return grid[gridIndex].walkableType == WalkableType.Walkable;
        }

        // Extract to other class
        public Vector3 GetNearestWalkableCellPosition(Vector3 worldPosition)
        {
            var (startX, startY) = GetCellsMap(worldPosition);

            System.Array.Clear(_visited, 0, _visited.Length);
            _queue.Clear();
            _queue.Enqueue(new Vector2Int(startX, startY));

            while (_queue.Count > 0)
            {
                var current = _queue.Dequeue();
                int x = current.x;
                int y = current.y;

                if (x < 0 || x >= gridSize.x || y < 0 || y >= gridSize.z) continue;

                int index = x + y * gridSize.x;
                if (_visited[index]) continue;

                _visited[index] = true;

                if (grid[index].walkableType == WalkableType.Walkable)
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

        // Pass this to Jobs
        protected WalkableType IsCellWalkable(Vector3 cellPosition)
        {
            return checkType.IsCellWalkable(cellPosition);
        }

        protected Vector3 GetCellPositionInWorldMap(int gridX, int gridY)
        {
            Vector3 cellPosition = GetCellPositionInGrid(gridX, gridY);

            return CheckPoint(cellPosition);
        }

        protected Vector3 GetCellPositionInGrid(int gridX, int gridY)
        {
            return transform.position
                   + Vector3.right * ((gridX + 0.5f) * cellDiameter)
                   + Vector3.forward * ((gridY + 0.5f) * cellDiameter);
        }

        /// <summary>
        /// Checks if hits something, so returns the position.
        /// </summary>
        /// <param name="cellPosition"></param>
        /// <returns></returns>
        private Vector3 CheckPoint(Vector3 cellPosition)
        {
            return Physics.Raycast(cellPosition + Vector3.up * gridSize.y,
                    Vector3.down, out RaycastHit raycastHit, gridSize.y, walkableMask)
                    ? raycastHit.point
                    : cellPosition;
        }

        protected (int x, int y) GetCellsMap(Vector3 worldPosition)
        {
            Vector3 gridPos = worldPosition - transform.position;

            int x = Mathf.Clamp(Mathf.FloorToInt(gridPos.x / cellDiameter), 0, gridSize.x - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(gridPos.z / cellDiameter), 0, gridSize.z - 1);

            return (x, y);
        }

        #region Unity Methods

        public virtual void Initialize()
        {
            cellSize = Mathf.Max(0.05f, cellSize);
            cellDiameter = cellSize * 2;

            walkableRegionsDic = new NativeHashMap<int, int>(terrainTypes.Length, Allocator.Persistent);

            foreach (var region in terrainTypes)
            {
                walkableMask.value |= region.terrainMask.value;
                walkableRegionsDic.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
            }

            CreateGrid();
        }

        public void Destroy()
        {
            if (grid.IsCreated) grid.Dispose();
            if (neighbors.IsCreated) neighbors.Dispose();
            if (neighborOffSet.IsCreated) neighborOffSet.Dispose();
            if (neighborTotalCount.IsCreated) neighborTotalCount.Dispose();
            if (walkableRegionsDic.IsCreated) walkableRegionsDic.Dispose();
        }

        #endregion

        #region Gizmos

        public virtual bool? DrawGizmos()
        {
            if (!grid.IsCreated || grid.Length == 0) return false;

            Vector3 sizeCell = new Vector3(0.99f, 0.05f, 0.99f) * cellDiameter;

            var walkableColor = new Color(0, 1, 0.5f, 0.5f);
            var nonWalkableSize = new Vector3(0.2f, 0.2f, 0.2f);

            for (int i = 0; i < grid.Length; i++)
            {
                Vector3 drawPos = grid[i].position;

                if (grid[i].walkableType == WalkableType.Air) continue;

                if (grid[i].walkableType == WalkableType.Walkable)
                {
                    Gizmos.color = walkableColor;
                    Gizmos.DrawWireCube(drawPos, sizeCell);
                }
                else
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(drawPos, nonWalkableSize);
                }
            }

            return true;
        }


        #endregion
    }
}