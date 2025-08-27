using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace NavigationGraph
{
    public sealed class NavigationGraphSystem : MonoBehaviour
    {
        [Header("Gizmos")]
        [SerializeField] private bool _boxGrid;

        [Header("Graph")]
        [SerializeField] private NavigationGraphType _graphType;
        [SerializeField] private NeighborsPerCell _neighborsPerCell;
        [SerializeField] private Vector2Int _gridSize = new(100, 100);
        [SerializeField] private float _maxDistance = 15;
        [SerializeField] private float _cellSize = 0.5f;
        [SerializeField, Range(0f, 50f)] private float _obstacleMargin = 0.5f;
        [SerializeField, Range(0f, 50f)] private float _cliffMargin = 0.5f;

        [SerializeField] private TerrainType[] _terrainTypes;

        [Header("Check Obstacles")]
        [SerializeField] private int _maxHits = 10;
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
            var checkType = CheckFactory.Create(_raycastCheckType, _maxDistance, _radius, _height, _notWalkableMask);
            _graph = GraphFactory.Create(_graphType, checkType, _terrainTypes, _cellSize, _maxDistance, _gridSize, _notWalkableMask, transform, _obstacleMargin, _cliffMargin);
            _graph?.Initialize();
            ServiceLocator.Instance.RegisterService<INavigationGraph>(_graph);
        }

        private void OnDestroy() => _graph?.Destroy();

        [System.Serializable]
        public struct TerrainType
        {
            public LayerMask terrainMask;
            public int terrainPenalty;
        }

#if UNITY_EDITOR

        /// <summary>
        /// Scans the environment and updates the graph. This is for Edit Only.
        /// </summary>
        public void Scan()
        {
            var checkType = CheckFactory.Create(_raycastCheckType, _maxDistance, _radius, _height, _notWalkableMask);
            _graph = GraphFactory.Create(_graphType, checkType, _terrainTypes, _cellSize, _maxDistance, _gridSize, _notWalkableMask, transform, _obstacleMargin, _cliffMargin);
            _graph?.Initialize();
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
            DrawCubeForGrid();
            _graph?.DrawGizmos();
        }

        private void DrawCubeForGrid()
        {
            if (!_boxGrid) return;

            var cellDiameter = _cellSize * 2;

            float width = _gridSize.x * cellDiameter;
            float depth = _gridSize.y * cellDiameter;
            float height = _maxDistance;

            Vector3 gridCenter = transform.position + Vector3.right * (width * 0.5f) + Vector3.forward * (depth * 0.5f) + Vector3.up * (height * 0.5f);

            Vector3 boxSize = new Vector3(width, height, depth);

            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(gridCenter, boxSize);
        }

        #endregion
    }

    public enum NeighborsPerCell
    {
        Four = 0,
        Eight = 1,
        Sixteen = 2
    }
}
