using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace NavigationGraph
{
    // Refac this
    public interface INavigationGraph
    {
        void Initialize(GridDataAsset gridBaked);
        void Destroy();
        void CreateGrid();
        bool? DrawGizmos();
        int GetGridSizeLength();
        float GetCellSize();
        float GetCellDiameter();
        int GetXSize();
        int GetZSize();
        Vector3 GetOrigin();
        NativeArray<Cell> GetGrid();
        NativeArray<int> GetNeighbors();
        NativeArray<int> GetNeighborTotalCount();
        NativeArray<int> GetNeighborOffsets();
        LayerMask GetWalkableMask();
        Cell GetRandomCell(); // Eliminate this in the future.
        Cell GetCellWithWorldPosition(Vector3 worldPosition);
        Vector3 GetNearestWalkableCellPosition(Vector3 worldPosition);
        bool IsInGrid(Vector3 worldPosition);

        void CombineDependencies(JobHandle jobHandle);

        Action OnCreateGrid { get; set; }
    }
}