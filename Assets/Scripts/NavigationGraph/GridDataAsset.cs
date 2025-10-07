using Unity.Mathematics;
using UnityEngine;

namespace NavigationGraph
{
    public class GridDataAsset : ScriptableObject
    {
        [field: SerializeField] public Vector3Int GridSize { get; set; }
        [HideInInspector] public NodeData[] cells;
        [HideInInspector] public NeighborsCell neighborsCell;

        public void DrawGizmos(float cellDiameter)
        {
            Vector3 sizeCell = new Vector3(0.99f, 0.05f, 0.99f) * cellDiameter;

            var walkableColor = new Color(0, 1, 0.5f, 0.5f);
            var nonWalkableSize = new Vector3(0.2f, 0.2f, 0.2f);

            for (int i = 0; i < cells.Length; i++)
            {
                var node = cells[i];
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
    public struct NodeData
    {
        public Vector3 position;
        public float3 normal;
        public float height;
        public int gridX;
        public int gridZ;
        public int gridIndex;
        public int cellCostPenalty;
        public WalkableType walkableType;
    }
}