using Unity.Collections;
using UnityEngine;

namespace NavigationGraph
{
    public interface INavigationGraph
    {
        void Initialize(GridDataAsset gridBaked);
        void Destroy();
        bool? DrawGizmos();
        int GetGridSizeLength();
        float GetCellSize();
        float GetCellDiameter();
        int GetXSize();
        NativeArray<Cell> GetGrid();
        NativeArray<int> GetNeighbors();
        NativeArray<int> GetNeighborTotalCount();
        NativeArray<int> GetNeighborOffsets();
        Cell GetRandomCell(); // Eliminate this in the future.
        Cell GetCellWithWorldPosition(Vector3 worldPosition);
        Vector3 GetNearestWalkableCellPosition(Vector3 worldPosition);
        bool IsInGrid(Vector3 worldPosition);
    }
}