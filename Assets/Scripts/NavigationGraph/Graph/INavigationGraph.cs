using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace NavigationGraph
{
    // Refac this
    public interface INavigationGraph
    {
        // Initialize
        void Initialize(GridDataAsset gridBaked);
        void Destroy();
        void CreateGrid();
        bool? DrawGizmos();

        // Graph
        NativeArray<Node> Graph { get; }
        NativeArray<int> Neighbors { get; }
        NativeArray<int> NeighborTotalCount { get; }
        NativeArray<int> NeighborOffsets { get; }
        Vector3Int GridSize { get; }
        Vector3 Origin { get; }
        float CellSize { get; }
        float CellDiameter { get; }
        LayerMask WalkableMask { get; }

        int GetGridSizeLength();
        Vector3 TryGetNearestWalkableNode(Vector3 worldPosition);
        Node GetRandomCell(); // Eliminate this in the future.
        Node GetNode(Vector3 worldPosition);
        bool IsInGrid(Vector3 worldPosition);

        void CombineDependencies(JobHandle jobHandle);

        Action OnCreateGrid { get; set; }
    }
}