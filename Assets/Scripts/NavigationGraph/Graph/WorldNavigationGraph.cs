using System.Collections.Generic;
using System.Linq;
using NavigationGraph.RaycastCheck;
using Unity.Collections;
using UnityEngine;

namespace NavigationGraph.Graph
{
    internal sealed class WorldNavigationGraph : NavigationGraph
    {
        public WorldNavigationGraph(IRaycastType checkType, NavigationGraphConfig navigationGraphConfig)
        : base(checkType, navigationGraphConfig)
        {
            GraphType = NavigationGraphType.Grid3D;
        }

        protected override void LoadGridFromDisk(GridDataAsset gridBaked)
        {
            throw new System.NotImplementedException();
        }

        protected override void CreateGrid()
        {
            throw new System.NotImplementedException();
            
            // if (grid.IsCreated)
            // {
            //     grid.Dispose();
            // }

            // List<Cell> tempCells = new List<Cell>();

            // for (int x = 0; x < gridSize.x; x++)
            // {
            //     for (int z = 0; z < gridSize.z; z++)
            //     {
            //         Vector3 origin = transform.position
            //                          + Vector3.right * ((x + 0.5f) * cellDiameter)
            //                          + Vector3.forward * ((z + 0.5f) * cellDiameter)
            //                          + Vector3.up * gridSize.y;

            //         List<RaycastHit> hits = RaycastContinuous(origin, walkableMask);

            //         foreach (var hit in hits)
            //         {
            //             // bool isWalkable = IsCellWalkable(hit.point, 2f);
            //             bool isWalkable = true;
            //             if (!isWalkable) continue;

            //             var cell = new Cell
            //             {
            //                 position = hit.point,
            //                 gridX = x,
            //                 gridZ = z,
            //                 height = hit.point.y, // TODO: Change this.
            //                 gridIndex = tempCells.Count
            //             };

            //             tempCells.Add(cell);
            //         }
            //     }
            // }

            // grid = new NativeArray<Cell>(tempCells.Count, Allocator.Persistent);
            // for (int i = 0; i < tempCells.Count; i++)
            //     grid[i] = tempCells[i];
        }



        private List<RaycastHit> RaycastContinuous(Vector3 from, LayerMask mask)
        {
            List<RaycastHit> hits = new List<RaycastHit>();
            if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, gridSize.y, mask)) return hits;

            hits.Add(hit);
            float minDist = cellSize * 0.5f;

            const int MAX_HITS = 10;
            for (int i = 0; i < MAX_HITS; i++)
            {
                Vector3 nextOrigin = hit.point + Vector3.down * minDist;
                if (!Physics.Raycast(nextOrigin, Vector3.down, out hit, gridSize.y, mask)) break;

                if (hits.Any(h => Mathf.Abs(h.point.y - hit.point.y) < minDist)) continue;

                hits.Add(hit);
            }

            return hits;
        }

        public Cell GetClosestCell(Vector3 worldPos)
        {
            var (x, z) = GetCellsMap(worldPos);

            Cell? closest = null;
            float closestDist = float.MaxValue;

            foreach (var cell in grid)
            {
                if (cell.gridX != x || cell.gridZ != z) continue;

                float dist = Mathf.Abs(cell.position.y - worldPos.y);
                if (dist < closestDist)
                {
                    closest = cell;
                    closestDist = dist;
                }
            }

            return closest ?? default;
        }

        // private Vector3[] GetCellPositionInWorldMap(int gridX, int gridY)
        // {
        //     Vector3 cellPosition = GetCellPositionWorld(gridX, gridY);

        //     return CheckPoint(cellPosition);
        // }

        // private Vector3 GetCellPositionWorld(int gridX, int gridY)
        // {
        //     return transform.position
        //            + Vector3.right * ((gridX + 0.5f) * GetCellDiameter())
        //            + Vector3.forward * ((gridY + 0.5f) * GetCellDiameter());
        // }

        // private Vector3[] CheckPoint(Vector3 cellPosition)
        // {
        //     Vector3 from = cellPosition + Vector3.up * _maxDistance;
        //     LayerMask combined = _walkableMask | _notWalkableMask;
        //     return RaycastContinuous(from, combined).Select(h => h.point).ToArray();
        // }

    }
}