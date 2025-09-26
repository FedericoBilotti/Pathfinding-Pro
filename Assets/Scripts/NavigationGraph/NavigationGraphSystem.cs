using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Pathfinding;
using UnityEngine;

namespace NavigationGraph
{
    [DefaultExecutionOrder(-900)]
    [RequireComponent(typeof(PathRequester))]
    [RequireComponent(typeof(AgentUpdateManager))]
    public sealed class NavigationGraphSystem : MonoBehaviour
    {
        [Header("Gizmos")]
        [SerializeField] private bool _boxGrid;

        [Header("Graph")]
        [SerializeField] private NavigationGraphType _graphType;
        [SerializeField] private NeighborsPerCell _neighborsPerCell;
        [SerializeField] private Vector3Int _gridSize = new(100, 20, 100);
        [SerializeField] private float _cellSize = 0.5f;
        [SerializeField] private float _maxHeightDifference = 0.01f;
        [SerializeField, Range(0f, 10)] private float _obstacleMargin = 0.5f;
        [SerializeField, Range(0f, 10f)] private float _cliffMargin = 0.5f;

        [SerializeField] private TerrainType[] _terrainTypes;

        [Header("Check Obstacles")]
        [SerializeField, Range(0f, 90f)] private float _inclineLimit = 45f;
        [SerializeField] private LayerMask _ignoreMaskAtCreateGrid = 0;
        [SerializeField] private LayerMask _notWalkableMask;
        [SerializeField] private RaycastType _raycastCheckType;
        private INavigationGraph _graph;

        private float _radius = 1f; // This is for the capsule & sphere
        private float _height = 2f; // This is for the capsule

        public float Radius { get => _radius; set => _radius = value; }
        public float Height { get => _height; set => _height = value; }
        public RaycastType RaycastCheckType => _raycastCheckType;

        // This is for saving the path.
        [field: SerializeField] public GridDataAsset GridBaked { get; private set; }

        private void Awake()
        {
            var checkType = CheckFactory.Create(_raycastCheckType, _gridSize.y, _inclineLimit, _radius, _height, transform, _notWalkableMask, GetWalkableMask());

            _graph = GraphFactory.Create(_graphType, checkType, GetNavigationGraphConfig());
            _graph?.Initialize(GridBaked);
            ServiceLocator.Instance.RegisterService<INavigationGraph>(_graph);
        }

        private void OnDestroy()
        {
            _graph?.Destroy();
            ServiceLocator.Instance.RemoveService<INavigationGraph>();
        }

        void Update()
        {
            _graph.CreateGrid();   
        }

        private void OnValidate()
        {
            _gridSize.x = Mathf.Max(1, _gridSize.x);
            _gridSize.y = Mathf.Max(1, _gridSize.y);
            _gridSize.z = Mathf.Max(1, _gridSize.z);

            _maxHeightDifference = Mathf.Max(0.1f, _maxHeightDifference);
        }

        private LayerMask GetWalkableMask()
        {
            LayerMask walkableMask = 0;
            foreach (var region in _terrainTypes)
            {
                walkableMask.value |= region.terrainMask.value;
            }
            return walkableMask;
        }

        private NavigationGraphConfig GetNavigationGraphConfig()
        {
            return new NavigationGraphConfig
            {
                gridSize = _gridSize,
                transform = transform,
                notWalkableMask = _notWalkableMask,
                neighborsPerCell = _neighborsPerCell,
                terrainTypes = _terrainTypes,
                raycastCheckType = _raycastCheckType,
                cellSize = _cellSize,
                obstacleMargin = _obstacleMargin,
                cliffMargin = _cliffMargin,
                maxHeightDifference = _maxHeightDifference,
                inclineLimit = _inclineLimit,
                ignoreMaskAtCreateGrid = _ignoreMaskAtCreateGrid,
            };
        }

#if UNITY_EDITOR

        public GridDataAsset BakeGridAsset(string assetPath)
        {
            Clear();
            Scan();

            var grid = _graph.GetGrid();
            var neighbors = _graph.GetNeighbors();
            var neighborTotalCounts = _graph.GetNeighborTotalCount();
            var neighborOffsets = _graph.GetNeighborOffsets();

            GridDataAsset asset = ScriptableObject.CreateInstance<GridDataAsset>();
            asset.gridSize = _gridSize;
            asset.cells = new CellData[_gridSize.x * _gridSize.z];
            asset.neighborsCell.neighbors = new int[neighbors.Length];
            asset.neighborsCell.neighborTotalCount = new int[neighborTotalCounts.Length];
            asset.neighborsCell.neighborOffsets = new int[neighborOffsets.Length];

            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.z; y++)
                {
                    int index = x + y * _gridSize.x;
                    Cell actualCell = grid[index];

                    asset.cells[index] = new CellData
                    {
                        position = actualCell.position,
                        gridX = actualCell.gridX,
                        gridZ = actualCell.gridZ,
                        gridIndex = actualCell.gridIndex,
                        cellCostPenalty = actualCell.cellCostPenalty,
                        height = actualCell.height,
                        walkableType = actualCell.walkableType
                    };
                }
            }

            for (int i = 0; i < neighbors.Length; i++)
                asset.neighborsCell.neighbors[i] = neighbors[i];

            for (int i = 0; i < neighborTotalCounts.Length; i++)
                asset.neighborsCell.neighborTotalCount[i] = neighborTotalCounts[i];

            for (int i = 0; i < neighborOffsets.Length; i++)
                asset.neighborsCell.neighborOffsets[i] = neighborOffsets[i];

            string folder = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // Save asset
            UnityEditor.AssetDatabase.CreateAsset(asset, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"Grid baked and saved as asset at: {assetPath}");

            return asset;
        }

        public void SetBakeGrid(GridDataAsset grid) => GridBaked = grid;

        public void SaveGrid(string path)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using FileStream file = File.Create(path);
            bf.Serialize(file, GridBaked);
        }

        public void DeleteGrid(string path)
        {
            if (GridBaked == null)
            {
                Debug.LogWarning("No baked grid assigned to delete.");
                return;
            }

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Could not find asset path for grid.");
                return;
            }

            bool success = UnityEditor.AssetDatabase.DeleteAsset(path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            if (success)
            {
                Debug.Log($"Deleted baked grid at {path}");
                GridBaked = null;
                return;
            }
            else
            {
                Debug.LogError($"Failed to delete baked grid at {path}");
                return;
            }
        }

        /// <summary>
        /// Scans the environment and updates the graph. This is for Edit Only.
        /// </summary>
        public void Scan()
        {
            var checkType = CheckFactory.Create(_raycastCheckType, _gridSize.y, _inclineLimit, _radius, _height, transform, _notWalkableMask, GetWalkableMask());

            _graph = GraphFactory.Create(_graphType, checkType, GetNavigationGraphConfig());
            _graph?.Initialize(GridBaked);
        }

        /// <summary>
        /// Destroy the graph. This is for Edit Only.
        /// </summary>
        public void Clear() => _graph?.Destroy();

#endif

        #region Gizmos

        // Each gizmo is going to be with his own grid.

        private void OnDrawGizmos()
        {
            bool? drawed = _graph?.DrawGizmos();
            DrawCubeForGrid(drawed);
        }

        private void DrawCubeForGrid(bool? drawed)
        {
            if (!_boxGrid) return;

            var cellDiameter = _cellSize * 2;

            float width = _gridSize.x * cellDiameter;
            float depth = _gridSize.z * cellDiameter;
            float height = _gridSize.y;

            Vector3 gridCenter = transform.position + Vector3.right * (width * 0.5f) + Vector3.forward * (depth * 0.5f) + Vector3.up * (height * 0.5f);
            Vector3 boxSize = new Vector3(width, height, depth);

            const float R = 0;
            const float G = 0;
            const float B = 0.8f;
            float opacity = drawed == null || drawed == false ? 0.2f : 0.1f;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(gridCenter, boxSize);
            Gizmos.color = new Color(R, G, B, opacity);
            Gizmos.DrawCube(gridCenter, boxSize);
        }

        #endregion
    }

    [System.Serializable]
    public struct TerrainType
    {
        public LayerMask terrainMask;
        public int terrainPenalty;
    }

    public enum NeighborsPerCell : byte
    {
        Four = 4,
        Eight = 8,
        Sixteen = 16
    }

    public struct NavigationGraphConfig
    {
        public Vector3Int gridSize;
        public Transform transform;
        public LayerMask notWalkableMask;
        public RaycastType raycastCheckType;
        public NeighborsPerCell neighborsPerCell;
        public TerrainType[] terrainTypes;
        public float cellSize;
        public float obstacleMargin;
        public float cliffMargin;
        public float maxHeightDifference;
        public float inclineLimit;
        public LayerMask ignoreMaskAtCreateGrid;
    }
}