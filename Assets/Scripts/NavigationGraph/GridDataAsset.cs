using UnityEngine;

namespace NavigationGraph
{
    public class GridDataAsset : ScriptableObject
    {
        public Vector3Int gridSize;
        public CellData[] cells;
    }
    
    // Missing to add neighbors
    [System.Serializable]
    public struct CellData
    {        
        public Vector3 position;
        public int gridX;
        public int gridZ;
        public int gridIndex;
        public int cellCostPenalty;
        public float height;
        public WalkableType walkableType;
    }
}