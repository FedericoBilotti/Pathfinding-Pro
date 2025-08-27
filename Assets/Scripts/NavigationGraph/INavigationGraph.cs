using Unity.Collections;
using UnityEngine;

namespace NavigationGraph
{
    public interface INavigationGraph
    {
        void Initialize();
        void Destroy();
        void DrawGizmos();
        int GetGridSizeX();
        int GetGridSize();
        NativeArray<Cell> GetGrid();
        NativeArray<FixedList64Bytes<int>> GetNeighbors();
        Cell GetRandomCell(); // Eliminate this in the future.
        Cell GetCellWithWorldPosition(Vector3 worldPosition);
        Vector3 GetNearestWalkableCellPosition(Vector3 worldPosition);
        bool IsInGrid(Vector3 worldPosition);
    }
}