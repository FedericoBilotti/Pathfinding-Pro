using UnityEngine;

namespace NavigationGraph
{
    public sealed class NavigationGraphSystem : MonoBehaviour
    {
        [Header("Gizmos")]
        [SerializeField] private bool _boxGrid;

        [Header("Graph")]
        [SerializeField] private NavigationGraphType _graphType;
        [SerializeField] private Vector2Int _gridSize = new(100, 100);
        [SerializeField] private float _maxDistance = 15;
        [SerializeField] private float _cellSize = 0.5f;
        [SerializeField, Range(0f, 5f)] private float _obstacleMargin = 0.5f;
        [SerializeField, Range(0f, 5f)] private float _cliffMargin = 0.5f;

        [Header("Check Wall")]
        [SerializeField] private int _maxHits = 10;
        [SerializeField] private LayerMask _notWalkableMask;
        [SerializeField] private LayerMask _walkableMask;
        [SerializeField] private LayerMask _agentMask;

        private INavigationGraph _graph;
        private GraphFactory _graphFactory;

        private void Awake()
        {
            // Should be injected the factory
            _graphFactory = new();
            _graph = _graphFactory.Create(_graphType, _cellSize, _maxDistance, _gridSize, _notWalkableMask, transform, _walkableMask, _agentMask, _obstacleMargin, _cliffMargin, _maxHits);
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
            _graphFactory = new();
            _graph = _graphFactory.Create(_graphType, _cellSize, _maxDistance, _gridSize, _notWalkableMask, transform, _walkableMask, _agentMask, _obstacleMargin, _cliffMargin, _maxHits);
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
}