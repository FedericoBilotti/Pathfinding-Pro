using System;
using System.Collections.Generic;
using NavigationGraph.RaycastCheck;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace NavigationGraph
{
    internal abstract class NavigationGraph : INavigationGraph
    {
        private JobHandle _allJobsDependencies;

        //protected readonly IRaycastType checkType;
        protected readonly LayerMask notWalkableMask;
        protected TerrainType[] terrainTypes;
        protected Vector3Int gridSize;
        protected LayerMask walkableMask;
        protected LayerMask ignoreMasksAtCreateGrid;

        protected readonly Transform transform;

        protected NativeArray<Node> graph;
        protected NativeArray<int> neighbors;
        protected NativeArray<int> neighborTotalCount;
        protected NativeArray<int> neighborOffSet;
        
        protected NeighborsPerCell neighborsPerCell;

        protected float cellSize;
        protected float cellDiameter;
        protected float inclineLimit;

        protected float obstacleMargin;
        protected float cliffMargin;

        protected const float MAX_HEIGHT_DISTANCE = 0.5f;

        protected static NativeHashMap<int, int> walkableRegionsDic;

        public NavigationGraphType GraphType { get; protected set; }

        public Action OnCreateGrid { get; set; }

        protected NavigationGraph(NavigationGraphConfig navigationGraphConfig)
        {
            // this.checkType = checkType;
            terrainTypes = navigationGraphConfig.terrainTypes;
            cellSize = navigationGraphConfig.cellSize;
            gridSize = navigationGraphConfig.gridSize;
            notWalkableMask = navigationGraphConfig.notWalkableMask;
            transform = navigationGraphConfig.transform;
            obstacleMargin = navigationGraphConfig.obstacleMargin;
            cliffMargin = navigationGraphConfig.cliffMargin;
            neighborsPerCell = navigationGraphConfig.neighborsPerCell;
            inclineLimit = navigationGraphConfig.inclineLimit;
            ignoreMasksAtCreateGrid = navigationGraphConfig.ignoreMaskAtCreateGrid;
        }

        public abstract void LoadGridFromMemory(GridDataAsset gridBaked);
        public abstract void CreateGrid();
        public abstract Vector3 TryGetNearestWalkableNode(Vector3 worldPosition);
        public abstract Node GetNode(Vector3 worldPosition);

        public abstract bool? DrawGizmos();

        // Change this
        public NativeArray<Node> GetGraph() => graph;
        public Node GetRandomCell() => graph[UnityEngine.Random.Range(0, graph.Length)];
        public float GetCellSize() => cellSize;
        public float GetCellDiameter() => cellDiameter;
        public int GetGridSizeLength() => gridSize.x * gridSize.z;
        public int GetXSize() => gridSize.x;
        public int GetZSize() => gridSize.z;
        public Vector3 GetOrigin() => transform.position;
        public NativeArray<int> GetNeighbors() => neighbors;
        public NativeArray<int> GetNeighborTotalCount() => neighborTotalCount;
        public NativeArray<int> GetNeighborOffsets() => neighborOffSet;
        public LayerMask GetWalkableMask() => walkableMask;

        public virtual bool IsInGrid(Vector3 worldPosition)
        {
            Vector3 gridPos = worldPosition - transform.position;

            int x = Mathf.FloorToInt(gridPos.x / cellDiameter);
            int y = Mathf.FloorToInt(gridPos.z / cellDiameter);

            if (x < 0 || x >= gridSize.x || y < 0 || y >= gridSize.z) return false;

            int gridIndex = x + y * gridSize.x;
            return graph[gridIndex].walkableType == WalkableType.Walkable;
        }

        public virtual void Initialize(GridDataAsset gridBaked)
        {
            cellSize = Mathf.Max(0.05f, cellSize);
            cellDiameter = cellSize * 2;
            InitializeWalkableRegionCost();

            if (gridBaked)
                LoadGridFromMemory(gridBaked);
            else
                CreateGrid();
        }

        private void InitializeWalkableRegionCost()
        {
            walkableRegionsDic = new NativeHashMap<int, int>(terrainTypes.Length, Allocator.Persistent);

            foreach (var region in terrainTypes)
            {
                walkableMask.value |= region.terrainMask.value;
                walkableRegionsDic.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
            }
        }

        public void CombineDependencies(JobHandle jobHandle)
        {
            _allJobsDependencies = JobHandle.CombineDependencies(_allJobsDependencies, jobHandle);
        }

        public void Destroy()
        {
            if (graph.IsCreated) graph.Dispose(_allJobsDependencies);
            if (neighbors.IsCreated) neighbors.Dispose(_allJobsDependencies);
            if (neighborOffSet.IsCreated) neighborOffSet.Dispose(_allJobsDependencies);
            if (neighborTotalCount.IsCreated) neighborTotalCount.Dispose(_allJobsDependencies);
            if (walkableRegionsDic.IsCreated) walkableRegionsDic.Dispose(_allJobsDependencies);
        }
    }
}