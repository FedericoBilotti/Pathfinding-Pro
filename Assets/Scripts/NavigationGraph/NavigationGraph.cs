using System.Collections.Generic;
using NavigationGraph.RaycastCheck;
using Unity.Collections;
using UnityEngine;

namespace NavigationGraph
{
    internal abstract class NavigationGraph : INavigationGraph
    {
        protected readonly LayerMask notWalkableMask;
        protected readonly LayerMask walkableMask;
        protected readonly float maxDistance;
        protected readonly IRaycastType checkType;

        protected float cellSize;
        protected float cellDiameter;
        protected Vector2Int gridSize;
        protected NativeArray<Cell> grid;

        protected readonly Transform transform;

        protected float obstacleMargin;
        protected float cliffMargin;

        protected NativeArray<FixedList32Bytes<int>> cellNeighbors;

        public NavigationGraphType GraphType { get; protected set; }

        protected NavigationGraph(IRaycastType checkType, float cellSize, float maxDistance, Vector2Int gridSize, LayerMask notWalkableMask, Transform transform, LayerMask walkableMask, float obstacleMargin, float cliffMargin)
        {
            this.checkType = checkType;

            this.cellSize = cellSize;
            this.gridSize = gridSize;

            this.maxDistance = maxDistance;
            this.walkableMask = walkableMask;
            this.notWalkableMask = notWalkableMask;

            this.transform = transform;


            this.obstacleMargin = obstacleMargin;
            this.cliffMargin = cliffMargin;

        }

        protected abstract void CreateGrid();

        public NativeArray<Cell> GetGrid() => grid;
        public Cell GetRandomCell() => grid[Random.Range(0, grid.Length)];
        public int GetGridSize() => gridSize.x * gridSize.y;
        public int GetGridSizeX() => gridSize.x;
        public NativeArray<FixedList32Bytes<int>> GetNeighbors() => cellNeighbors;

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

            if (x < 0 || x >= gridSize.x || y < 0 || y >= gridSize.y) return false;

            int gridIndex = x + y * gridSize.x;
            return grid[gridIndex].isWalkable;
        }

        public Vector3 GetNearestWalkableCellPosition(Vector3 worldPosition)
        {
            var (startX, startY) = GetCellsMap(worldPosition);

            var visited = new bool[grid.Length];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startY));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int x = current.x;
                int y = current.y;

                if (x < 0 || x >= gridSize.x || y < 0 || y >= gridSize.y) continue;

                int index = x + y * gridSize.x;
                if (visited[index]) continue;

                visited[index] = true;

                if (grid[index].isWalkable)
                {
                    return transform.position + new Vector3(x * cellDiameter + cellSize, 0f, y * cellDiameter + cellSize);
                }

                queue.Enqueue(new Vector2Int(x + 1, y));
                queue.Enqueue(new Vector2Int(x - 1, y));
                queue.Enqueue(new Vector2Int(x, y + 1));
                queue.Enqueue(new Vector2Int(x, y - 1));
            }

            return transform.position;
        }

        // Pass this to Jobs
        protected WalkableType IsCellWalkable(Vector3 cellPosition, float radius)
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
            return Physics.Raycast(cellPosition + Vector3.up * maxDistance,
                    Vector3.down, out RaycastHit raycastHit, maxDistance, walkableMask)
                    ? raycastHit.point
                    : cellPosition;
        }

        protected (int x, int y) GetCellsMap(Vector3 worldPosition)
        {
            Vector3 gridPos = worldPosition - transform.position;

            int x = Mathf.Clamp(Mathf.FloorToInt(gridPos.x / cellDiameter), 0, gridSize.x - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(gridPos.z / cellDiameter), 0, gridSize.y - 1);

            return (x, y);
        }

        #region Unity Methods

        public virtual void Initialize()
        {
            cellSize = Mathf.Max(0.05f, cellSize);
            cellDiameter = cellSize * 2;

            CreateGrid();
        }

        public void Destroy()
        {
            if (grid.IsCreated) grid.Dispose();
            if (cellNeighbors.IsCreated) cellNeighbors.Dispose();
        }

        #endregion

        #region Gizmos

        public virtual void DrawGizmos()
        {
            if (!grid.IsCreated || grid.Length == 0) return;

            Vector3 sizeCell = new Vector3(0.99f, 0.05f, 0.99f) * cellDiameter;

            for (int i = 0; i < grid.Length; i++)
            {
                Vector3 drawPos = grid[i].position;

                if (grid[i].isWalkable)
                {
                    Gizmos.color = new Color(0, 1, 0.2f, 0.5f);
                    Gizmos.DrawCube(drawPos, sizeCell);
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(drawPos, sizeCell);
                }
                else
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(drawPos, new Vector3(0.2f, 0.2f, 0.2f));
                }
            }
        }

        #endregion
    }
}