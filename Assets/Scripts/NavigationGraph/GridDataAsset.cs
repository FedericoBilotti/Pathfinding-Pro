using UnityEngine;

namespace NavigationGraph
{
    public class GridDataAsset : ScriptableObject
    {
        [field: SerializeField] public Vector3Int GridSize { get; set; }
        [HideInInspector] public CellData[] cells;
        [HideInInspector] public NeighborsCell neighborsCell;
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