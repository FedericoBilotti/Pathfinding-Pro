using System.Collections.Generic;
using System.Linq;
using NavigationGraph.Graph;
using UnityEngine;

namespace NavigationGraph
{
    public sealed class NavigationGraphSystem : MonoBehaviour
    {
        [Header("Gizmos")]
        [SerializeField] private bool _boxGrid;
        [SerializeField] private bool _scanDistance;
        [SerializeField] private bool _previewOfCells;
        [SerializeField] private bool _debugOfWalkableCells;
        [SerializeField] private Vector2 _cellSizeGizmos;

        [Header("Graph")]
        [SerializeField] private NavigationGraphType _graphType;
        [SerializeField] private Vector2Int _gridSize = new(100, 100);
        [SerializeField] private float _maxDistance = 15;
        [SerializeField] private float _cellSize = 0.5f;

        [Header("Check Wall")]
        [SerializeField] private int _maxHits = 10;
        [SerializeField] private LayerMask _notWalkableMask;
        [SerializeField] private LayerMask _walkableMask;
        [SerializeField] private LayerMask _agentMask;

        private NavigationGraph _graph;

        private void Awake()
        {
            _graph = _graphType == NavigationGraphType.Grid2D
                    ? new SimpleGridNavigationGraph(_cellSize, _maxDistance, _gridSize, _notWalkableMask, transform, _walkableMask, _agentMask)
                    : new WorldNavigationGraph(_cellSize, _maxDistance, _gridSize, _notWalkableMask, transform, _walkableMask, _agentMask);
            _graph?.Initialize();

            ServiceLocator.Instance.RegisterService<INavigationGraph>(_graph);
        }

        private void OnValidate()
        {
            _cellSizeGizmos.x = Mathf.Max(0.01f, _cellSizeGizmos.x);
            _cellSizeGizmos.x = Mathf.Min(0.99f, _cellSizeGizmos.x);
            _cellSizeGizmos.y = Mathf.Max(0.01f, _cellSizeGizmos.y);
            _cellSizeGizmos.y = Mathf.Min(0.99f, _cellSizeGizmos.y);
        }

        private void OnDestroy() => _graph?.Destroy();

        public enum NavigationGraphType
        {
            Grid2D,
            Grid3D,
        }

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