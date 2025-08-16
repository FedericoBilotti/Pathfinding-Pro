using Unity.Collections;
using UnityEngine;

namespace NavigationGraph
{
    public interface INavigationGraph
    {
        void Initialize();
        void Destroy();
        void DrawGizmos();
        NativeArray<Cell> GetGrid();
        Cell GetRandomCell();
        Cell GetCellWithWorldPosition(Vector3 worldPosition);
        public Vector3 GetNearestWalkableCellPosition(Vector3 worldPosition);
        bool IsInGrid(Vector3 worldPosition);
        int GetGridSizeX();
        int GetGridSize();
    }
}