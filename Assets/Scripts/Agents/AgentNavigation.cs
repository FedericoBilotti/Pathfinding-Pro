using System.Collections;
using System.Collections.Generic;
using NavigationGraph;
using Pathfinding;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Agents
{
    public abstract class AgentNavigation : MonoBehaviour, IAgent
    {
        [Header("Steering")]
        [SerializeField] protected float speed = 5;
        [SerializeField] protected float rotationSpeed = 10;
        [SerializeField] protected float changeWaypointDistance = 0.5f;
        [SerializeField, Tooltip("Stop from this distance from the target position")] protected float stoppingDistance = 1f;
        [SerializeField, Tooltip("The agent will slowing down in time to reach the target")] protected bool autoBraking = true;

        [Header("Debug")]
        [SerializeField] private bool _showPath = true;

        protected static float brakingMargin = 2f;

        private IPathfinding _pathfinding;
        private Coroutine _moveAgentCoroutine;
        protected INavigationGraph graph;
        protected List<Vector3> waypointsPath;
        protected Transform ownTransform;

        protected int currentWaypoint;

        protected float3 lastTargetPosition = new(0, 0, 0);

        public PathStatus StatusPath { get; protected set; } = PathStatus.Idle;
        public bool HasPath => waypointsPath != null && waypointsPath.Count > 0 && StatusPath == PathStatus.Success;
        public float Speed { get => speed; set => speed = Mathf.Max(0.01f, value); }
        public float RotationSpeed { get => rotationSpeed; set => rotationSpeed = Mathf.Max(0.01f, value); }
        public float ChangeWaypointDistance { get => changeWaypointDistance; set => changeWaypointDistance = Mathf.Max(0.1f, value); }

        private void Awake()
        {
            waypointsPath = new List<Vector3>(10);
            ownTransform = transform;

            _pathfinding = ServiceLocator.Instance.GetService<IPathfinding>();
            graph = ServiceLocator.Instance.GetService<INavigationGraph>();

            Initialize();
        }

        protected virtual void Initialize() { }

        private void OnValidate()
        {
            speed = Mathf.Max(0.01f, speed);
            rotationSpeed = Mathf.Max(0.01f, rotationSpeed);
            changeWaypointDistance = Mathf.Max(0.1f, changeWaypointDistance);
            stoppingDistance = Mathf.Max(0f, stoppingDistance);
        }

        protected virtual IEnumerator MoveAgent()
        {
            while (currentWaypoint < waypointsPath.Count)
            {
                Vector3 distanceToTarget = waypointsPath[currentWaypoint] - ownTransform.position;

                Move(distanceToTarget);
                Rotate(distanceToTarget);
                CheckWaypoints(distanceToTarget);
                yield return null;
            }

            ClearPath();
            StatusPath = PathStatus.Idle;
        }

        protected abstract void Move(Vector3 targetDistance);
        protected abstract void Rotate(Vector3 targetDistance);

        protected abstract bool IsBraking();

        public virtual bool RequestPath(Vector3 startPosition, Vector3 endPosition)
        {
            if (StatusPath == PathStatus.Requested) return false;
            if (!IsAgentInGrid(graph, ownTransform.position))
            {
                StatusPath = PathStatus.Failed;
                return false;
            }

            float3 target = endPosition;
            Cell endCell = graph.GetCellWithWorldPosition(target);
            if (all(lastTargetPosition == endCell.position)) return false;

            StatusPath = PathStatus.Requested;

            Cell startCell = graph.GetCellWithWorldPosition(startPosition);
            bool isPathValid = _pathfinding.RequestPath(this, startCell, endCell);

            if (isPathValid)
            {
                lastTargetPosition = endCell.position;
                return true;
            }

            StatusPath = PathStatus.Failed;
            return false;
        }

        public virtual void SetPath(NativeList<Cell> path)
        {
            if (!path.IsCreated || path.Length == 0)
            {
                StatusPath = PathStatus.Failed;
                return;
            }

            waypointsPath.Clear();
            currentWaypoint = 0;

            foreach (var cell in path)
            {
                waypointsPath.Add(cell.position);
            }

            StatusPath = PathStatus.Success;
            if (_moveAgentCoroutine != null) StopCoroutine(_moveAgentCoroutine);
            _moveAgentCoroutine = StartCoroutine(MoveAgent());

        }

        protected void CheckWaypoints(Vector3 distance)
        {
            if (distance.sqrMagnitude > changeWaypointDistance * changeWaypointDistance) return;
            currentWaypoint++;

            if (currentWaypoint < waypointsPath.Count) return;

            ClearPath();
            StatusPath = PathStatus.Idle;
        }

        protected void ClearPath()
        {
            currentWaypoint = 0;
            waypointsPath.Clear();
        }

        protected float GetMarginBraking()
        {
            float margin = brakingMargin * stoppingDistance;
            var clamp = Mathf.Clamp(margin, 0.1f, 4f);
            return clamp;
        }


        protected void MapToGrid()
        {
            // If the agent is not on the grid, move it to the closest grid position
            if (IsAgentInGrid(graph, transform.position)) return;

            Vector3 nearestCellPosition = MapToNearestCellPosition(graph, transform.position);
            transform.position = nearestCellPosition;
        }

        private static Vector3 MapToNearestCellPosition(INavigationGraph graph, Vector3 agentPosition)
        {
            Vector3 nearestPosition = graph.GetNearestWalkableCellPosition(agentPosition);
            return nearestPosition;
        }

        private static bool IsAgentInGrid(INavigationGraph graph, Vector3 position) => graph.IsInGrid(position);

        public enum PathStatus
        {
            Idle,
            Failed,
            Requested,
            Success
        }

        private void OnDrawGizmos()
        {
            if (!_showPath) return;
            if (waypointsPath == null || waypointsPath.Count == 0) return;

            Gizmos.color = Color.black;

            for (int i = currentWaypoint; i < waypointsPath.Count; i++)
            {
                Gizmos.DrawLine(i == currentWaypoint ? transform.position : waypointsPath[i - 1], waypointsPath[i]);
                Gizmos.DrawCube(waypointsPath[i], Vector3.one * 0.35f);
            }
        }
    }
}