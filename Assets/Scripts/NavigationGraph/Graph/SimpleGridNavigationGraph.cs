using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace NavigationGraph.Graph
{
    internal sealed class SimpleGridNavigationGraph : NavigationGraph
    {
        public SimpleGridNavigationGraph(float cellSize, float maxDistance, Vector2Int gridSize,
                LayerMask notWalkableMask, Transform transform, LayerMask walkableMask, LayerMask agentMask, float obstacleMargin = 0.2f)
            : base(cellSize, maxDistance, gridSize, notWalkableMask, transform, walkableMask, agentMask, obstacleMargin)
        {
            GraphType = NavigationGraphType.Grid2D;
        }

        protected override void CreateGrid()
        {
            if (grid.IsCreated) grid.Dispose();

            int total = gridSize.x * gridSize.y;
            grid = new NativeArray<Cell>(total, Allocator.Persistent);

            // Check cells that are blocked by expanded bounds
            bool[] blockedByBounds = CollectBlockedByExpandedBounds();

            // For the remaining cells, compute if they are walkable
            bool[] computedWalkable = ComputeComputedWalkable(blockedByBounds);

            // Dilate the mask for non-walkable cells according to obstacle margin converted to cells
            int rCells = Mathf.CeilToInt(obstacleMargin / cellDiameter);
            if (rCells == 0 && obstacleMargin > 0f) rCells = 1; // At least one
            bool[] finalBlocked = DilateBlockedMask(computedWalkable, rCells);

            for (int i = 0; i < total; i++)
            {
                int x = i % gridSize.x;
                int y = i / gridSize.x;

                Vector3 cellPosition = GetCellPositionInWorldMap(x, y);

                bool isWalkable = !finalBlocked[i];
                WalkableType walkableType = IsCellWalkable(cellPosition, cellSize);

                // Keep it for safety check.
                if (!isWalkable && walkableType == WalkableType.Air)
                    continue;

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

        private bool[] CollectBlockedByExpandedBounds()
        {
            int total = gridSize.x * gridSize.y;
            var blocked = new bool[total];

            Vector3 gridWorldSize = new(gridSize.x * cellDiameter, maxDistance * 2f, gridSize.y * cellDiameter);
            Vector3 areaCenter = transform.position + new Vector3(gridWorldSize.x * 0.5f, 0f, gridWorldSize.z * 0.5f);
            Vector3 areaExtents = new(gridWorldSize.x * 0.5f, maxDistance, gridWorldSize.z * 0.5f);

            Collider[] hits = Physics.OverlapBox(areaCenter, areaExtents, transform.rotation, notWalkableMask.value);

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
                        if (idx >= 0 && idx < total) blocked[idx] = true;
                    }
                }
            }

            return blocked;
        }

        // Returns array[total] with true if the cell is walkable according to physical checks (and false if blocked by bounds).
        private bool[] ComputeComputedWalkable(bool[] blockedByBounds)
        {
            int total = GetGridSize();
            var computedWalkable = new bool[total];

            for (int i = 0; i < total; i++)
            {
                if (blockedByBounds[i])
                {
                    computedWalkable[i] = false;
                    continue;
                }

                int x = i % gridSize.x;
                int y = i / gridSize.x;

                Vector3 cellPosition = GetCellPositionInWorldMap(x, y);
                WalkableType walkableType = IsCellWalkable(cellPosition, cellSize);
                computedWalkable[i] = walkableType == WalkableType.Walkable;
            }

            return computedWalkable;
        }

        // Dilate (BFS multi-source) with all non-walkable cells rCells steps, and return finalBlocked[] (true = blocked)
        private bool[] DilateBlockedMask(bool[] computedWalkable, int rCells)
        {
            int total = GetGridSize();
            var finalBlocked = new bool[total];

            if (rCells <= 0)
            {
                // If it has 0 steps, invert computedWalkable
                for (int i = 0; i < total; i++) finalBlocked[i] = !computedWalkable[i];
                return finalBlocked;
            }

            var queue = new Queue<int>(total / 2);
            var distances = new int[total];
            for (int i = 0; i < total; i++) distances[i] = -1;

            // Enqueue all the non-walkable cells as sources (dist = 0)
            for (int i = 0; i < total; i++)
            {
                if (!computedWalkable[i])
                {
                    distances[i] = 0;
                    queue.Enqueue(i);
                    finalBlocked[i] = true;
                }
            }

            // Check connections (N-E-S-W)
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                int currentX = current % gridSize.x;
                int currentY = current / gridSize.x;
                int currentDistance = distances[current];

                if (currentDistance >= rCells) continue;

                TryEnqueueNeighbor(currentX + 1, currentY, currentDistance, distances, queue, finalBlocked);
                TryEnqueueNeighbor(currentX - 1, currentY, currentDistance, distances, queue, finalBlocked);
                TryEnqueueNeighbor(currentX, currentY + 1, currentDistance, distances, queue, finalBlocked);
                TryEnqueueNeighbor(currentX, currentY - 1, currentDistance, distances, queue, finalBlocked);
            }

            return finalBlocked;
        }

        private void TryEnqueueNeighbor(int neighborX, int neighborY, int currentDistance, int[] distances, Queue<int> queue, bool[] finalBlocked)
        {
            if (neighborX < 0 || neighborY < 0 || neighborX >= gridSize.x || neighborY >= gridSize.y) return;
            int neighborIndex = neighborX + neighborY * gridSize.x;
            if (distances[neighborIndex] != -1) return;
            distances[neighborIndex] = currentDistance + 1;
            finalBlocked[neighborIndex] = true;
            queue.Enqueue(neighborIndex);
        }
    }
}
