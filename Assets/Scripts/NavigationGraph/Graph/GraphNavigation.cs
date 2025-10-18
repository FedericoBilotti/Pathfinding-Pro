using System;
using System.Collections.Generic;
using NavigationGraph.Graph.Planar;
using NavigationGraph.RaycastCheck;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace NavigationGraph
{
    public abstract class GraphNavigation : INavigationGraph
    {
        private JobHandle _allJobsDependencies;

        //protected readonly IRaycastType checkType;
        protected readonly LayerMask notWalkableMask;
        protected TerrainType[] terrainTypes;
        protected LayerMask ignoreMasksAtCreateGrid;

        protected readonly Transform transform;

        protected NeighborsPerCell neighborsPerCell;

        private NativeArray<Node> _graph;
        private NativeArray<int> _neighbors;
        private NativeArray<int> _neighborTotalCount;
        private NativeArray<int> _neighborOffsets;

        public ref NativeArray<Node> Graph => ref _graph;
        public ref NativeArray<int> Neighbors => ref _neighbors;
        public ref NativeArray<int> NeighborTotalCount => ref _neighborTotalCount;
        public ref NativeArray<int> NeighborOffsets => ref _neighborOffsets;
        public Vector3Int GridSize { get; }
        public Vector3 Origin => transform.position;
        public float CellSize { get; private set; }
        public float CellDiameter { get; private set; }
        public LayerMask WalkableMask { get; private set; }

        protected float inclineLimit;

        protected float obstacleMargin;
        protected float cliffMargin;

        protected const float MAX_HEIGHT_DISTANCE = 0.5f;

        protected static NativeHashMap<int, int> walkableRegionsDic;

        public NavigationGraphType GraphType { get; protected set; }

        public Action OnCreateGrid { get; set; }

        protected GraphNavigation(NavigationGraphConfig navigationGraphConfig)
        {
            // this.checkType = checkType;
            terrainTypes = navigationGraphConfig.terrainTypes;
            CellSize = navigationGraphConfig.cellSize;
            GridSize = navigationGraphConfig.gridSize;
            notWalkableMask = navigationGraphConfig.notWalkableMask;
            transform = navigationGraphConfig.transform;
            obstacleMargin = navigationGraphConfig.obstacleMargin;
            cliffMargin = navigationGraphConfig.cliffMargin;
            neighborsPerCell = navigationGraphConfig.neighborsPerCell;
            inclineLimit = navigationGraphConfig.inclineLimit;
            ignoreMasksAtCreateGrid = navigationGraphConfig.ignoreMaskAtCreateGrid;
        }

        public abstract void LoadGrid(IGraphDataAsset gridBaked);
        public abstract void CreateGrid();
        public abstract Vector3 TryGetNearestWalkableNode(Vector3 worldPosition);
        public abstract Node GetNode(Vector3 worldPosition);
        public abstract bool? DrawGizmos();

        public int GetGridSizeLength() => GridSize.x * GridSize.z;
        public Node GetRandomCell() => Graph[UnityEngine.Random.Range(0, Graph.Length)];

        public virtual bool IsInGrid(Vector3 worldPosition)
        {
            Vector3 gridPos = worldPosition - transform.position;

            int x = Mathf.FloorToInt(gridPos.x / CellDiameter);
            int y = Mathf.FloorToInt(gridPos.z / CellDiameter);

            if (x < 0 || x >= GridSize.x || y < 0 || y >= GridSize.z) return false;

            int gridIndex = x + y * GridSize.x;
            return Graph[gridIndex].walkableType == WalkableType.Walkable;
        }

        public virtual void Initialize(GridDataAsset gridBaked)
        {
            CellSize = Mathf.Max(0.05f, CellSize);
            CellDiameter = CellSize * 2;
            InitializeWalkableRegionCost();

            if (gridBaked)
                LoadGrid(gridBaked);
            else
                CreateGrid();
        }

        private void InitializeWalkableRegionCost()
        {
            walkableRegionsDic = new NativeHashMap<int, int>(terrainTypes.Length, Allocator.Persistent);

            foreach (var region in terrainTypes)
            {
                // WalkableMask.value |= region.terrainMask.value;
                WalkableMask |= region.terrainMask.value;
                walkableRegionsDic.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
            }
        }

        public void CombineDependencies(JobHandle jobHandle)
        {
            _allJobsDependencies = JobHandle.CombineDependencies(_allJobsDependencies, jobHandle);
        }

        public void Destroy()
        {
            if (Graph.IsCreated) Graph.Dispose(_allJobsDependencies);
            if (Neighbors.IsCreated) Neighbors.Dispose(_allJobsDependencies);
            if (NeighborOffsets.IsCreated) NeighborOffsets.Dispose(_allJobsDependencies);
            if (NeighborTotalCount.IsCreated) NeighborTotalCount.Dispose(_allJobsDependencies);
            if (walkableRegionsDic.IsCreated) walkableRegionsDic.Dispose(_allJobsDependencies);
        }
    }
}