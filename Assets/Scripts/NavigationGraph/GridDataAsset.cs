using UnityEngine;

namespace NavigationGraph
{
    public class GridDataAsset : ScriptableObject
    {
        [field: SerializeField] public Vector3Int GridSize { get; set; }
        [HideInInspector] public CellData[] cells;
        [HideInInspector] public NeighborsCell neighborsCell;

        public void DrawGizmos(float cellDiameter)
        {
            Vector3 sizeCell = new Vector3(0.99f, 0.05f, 0.99f) * cellDiameter;

            var walkableColor = new Color(0, 1, 0.5f, 0.5f);
            var nonWalkableSize = new Vector3(0.2f, 0.2f, 0.2f);

            for (int i = 0; i < cells.Length; i++)
            {
                Vector3 drawPos = cells[i].position;

                if (cells[i].walkableType == WalkableType.Air) continue;

                if (cells[i].walkableType == WalkableType.Walkable)
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
        }
    }

    [System.Serializable]
    public struct NeighborsCell
    {
        public int[] neighbors;
        public int[] neighborTotalCount;
        public int[] neighborOffsets;
    }

    [System.Serializable]
    public struct CellData
    {
        public Vector3 position;
        public float height;
        public int gridX;
        public int gridZ;
        public int gridIndex;
        public int cellCostPenalty;
        public WalkableType walkableType;
    }
}