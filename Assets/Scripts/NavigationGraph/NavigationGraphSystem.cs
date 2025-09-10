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
        [SerializeField, Range(0f, 50f)] private float _obstacleMargin = 0.5f;
        [SerializeField, Range(0f, 50f)] private float _cliffMargin = 0.5f;

        [SerializeField] private TerrainType[] _terrainTypes;

        [Header("Check Obstacles")]
        [SerializeField, Range(0f, 90f)] private float _inclineLimit = 45f;
        [SerializeField] private LayerMask _notWalkableMask;
        [SerializeField] private RaycastType _raycastCheckType;
        private INavigationGraph _graph;

        private float _radius = 1f;
        private float _height = 2f;

        public float Radius { get => _radius; set => _radius = value; }
        public float Height { get => _height; set => _height = value; }
        public RaycastType RaycastCheckType => _raycastCheckType;

        // This is for saving the path.
        public GridDataAsset CurrentGrid { get; private set; }

        private void Awake()
        {
            var checkType = CheckFactory.Create(_raycastCheckType, _gridSize.y, _inclineLimit, _radius, _height, transform, _notWalkableMask, GetWalkableMask());

            _graph = GraphFactory.Create(_graphType, checkType, GetNavigationGraphConfig());
            _graph?.Initialize();
            ServiceLocator.Instance.RegisterService<INavigationGraph>(_graph);
        }

        private void OnDestroy() => _graph?.Destroy();

#if UNITY_EDITOR

        public GridDataAsset BakeGridAsset(string assetPath)
        {
            // Generar la grilla como siempre
            Clear();
            Scan();

            // Crear instancia del ScriptableObject
            GridDataAsset asset = ScriptableObject.CreateInstance<GridDataAsset>();
            asset.gridSize = _gridSize;
            asset.cells = new CellData[_gridSize.x * _gridSize.z];

            var grid = _graph.GetGrid();

            // Missing to add neighbors.
            for (int i = 0; i < _gridSize.x; i++)
            {
                for (int j = 0; j < _gridSize.z; j++)
                {
                    int index = i * _gridSize.z + j;
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

            string folder = System.IO.Path.GetDirectoryName(assetPath);
            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);

            // Save asset
            UnityEditor.AssetDatabase.CreateAsset(asset, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"Grid baked and saved as asset at: {assetPath}");

            return asset;
        }

        public void SetBakeGrid(GridDataAsset grid) => CurrentGrid = grid;

        public void SaveGrid(string path)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using FileStream file = File.Create(path);
            bf.Serialize(file, CurrentGrid);
        }

        public void LoadFromBakedAsset()
        {
            if (CurrentGrid == null)
            {
                Debug.LogError("No baked grid assigned!");
                return;
            }

            foreach (var cell in CurrentGrid.cells)
            {
                Debug.Log("Construct the grid");
                // Rebuild
            }
        }

        public void DeleteGrid(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError("Grid file not found: " + path);
                return;
            }

            Debug.Log("Deleting the grid file");
        }

        /// <summary>
        /// Scans the environment and updates the graph. This is for Edit Only.
        /// </summary>
        public void Scan()
        {
            var checkType = CheckFactory.Create(_raycastCheckType, _gridSize.y, _inclineLimit, _radius, _height, transform, _notWalkableMask, GetWalkableMask());

            _graph = GraphFactory.Create(_graphType, checkType, GetNavigationGraphConfig());
            _graph?.Initialize();
        }

        /// <summary>
        /// Destroy the graph. This is for Edit Only.
        /// </summary>
        public void Clear() => _graph?.Destroy();

#endif

        private void OnValidate()
        {
            _gridSize.x = Mathf.Max(1, _gridSize.x);
            _gridSize.y = Mathf.Max(1, _gridSize.y);
            _gridSize.z = Mathf.Max(1, _gridSize.z);
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
                maxHeightDifference = _maxHeightDifference
            };
        }

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
    }
}