using Unity.Collections;
using UnityEngine;

namespace NavigationGraph
{
    public interface INavigationGraph
    {
        void Initialize();
        void Destroy();
        bool? DrawGizmos();
        int GetGridSize();
        float GetCellSize();
        float GetCellDiameter();
        int GetXSize();
        NativeArray<Cell> GetGrid();
        NativeArray<int> GetNeighbors();
        NativeArray<int> GetNeighborCounts();
        NativeArray<int> GetNeighborsOffSet();
        Cell GetRandomCell(); // Eliminate this in the future.
        Cell GetCellWithWorldPosition(Vector3 worldPosition);
        Vector3 GetNearestWalkableCellPosition(Vector3 worldPosition);
        bool IsInGrid(Vector3 worldPosition);
    }
}