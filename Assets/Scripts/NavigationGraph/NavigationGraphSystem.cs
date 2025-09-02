using UnityEngine;

namespace NavigationGraph
{
    public sealed class NavigationGraphSystem : MonoBehaviour
    {
        [Header("Gizmos")]
        [SerializeField] private bool _boxGrid;

        [Header("Graph")]
        [SerializeField] private NavigationGraphType _graphType;
        [SerializeField] private NeighborsPerCell _neighborsPerCell;
        [SerializeField] private Vector3Int _gridSize = new(100, 20, 100);
        [SerializeField] private float _cellSize = 0.5f;
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

        private void Awake()
        {
            var checkType = CheckFactory.Create(_raycastCheckType, _gridSize.y, _inclineLimit, _radius, _height, transform, _notWalkableMask, GetWalkableMask());

            _graph = GraphFactory.Create(_graphType, checkType, GetNavigationGraphConfig());
            _graph?.Initialize();
            ServiceLocator.Instance.RegisterService<INavigationGraph>(_graph);
        }

        private void OnDestroy() => _graph?.Destroy();

#if UNITY_EDITOR

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
            };
        }

        #region Gizmos

        // Each gizmo is going to be with his own grid.

        private void OnDrawGizmos()
        {
            bool? drawed = _graph?.DrawGizmos();

            if (drawed == null || drawed == false)
                DrawCubeForGrid();
        }

        private void DrawCubeForGrid()
        {
            if (!_boxGrid) return;

            var cellDiameter = _cellSize * 2;

            float width = _gridSize.x * cellDiameter;
            float depth = _gridSize.z * cellDiameter;
            float height = _gridSize.y;

            Vector3 gridCenter = transform.position + Vector3.right * (width * 0.5f) + Vector3.forward * (depth * 0.5f) + Vector3.up * (height * 0.5f);

            Vector3 boxSize = new Vector3(width, height, depth);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(gridCenter, boxSize);
            Gizmos.color = new Color(0, 0, 0.8f, 0.1f);
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

    public enum NeighborsPerCell
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
    }
}
