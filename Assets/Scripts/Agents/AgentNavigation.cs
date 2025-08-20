using System;
using System.Collections;
using System.Collections.Generic;
using NavigationGraph;
using Pathfinding;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
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

        [Header("Pathfinding")]
        [SerializeField, Tooltip("Time that the agent it's going to ask a new path when reaching a target")] protected float rePath = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool _showPath = true;

        private Timer _timer;
        private IPathfinding _pathfinding;
        private Coroutine _moveAgentCoroutine;
        protected INavigationGraph graph;
        protected List<Vector3> waypointsPath;
        protected Transform ownTransform;
        protected int currentWaypoint;


        protected float3 lastTargetPosition = new(0, 0, 0);
        private Transform _actualTargetTransform;

        public PathStatus StatusPath { get; protected set; } = PathStatus.Idle;
        public bool HasPath => waypointsPath != null && waypointsPath.Count > 0 && StatusPath == PathStatus.Success;
        public float Speed { get => speed; set => speed = Mathf.Max(0.01f, value); }
        public float RotationSpeed { get => rotationSpeed; set => rotationSpeed = Mathf.Max(0.01f, value); }
        public float ChangeWaypointDistance { get => changeWaypointDistance; set => changeWaypointDistance = Mathf.Max(0.1f, value); }

        private void Awake()
        {
            ownTransform = transform;

            _pathfinding = ServiceLocator.Instance.GetService<IPathfinding>();
            graph = ServiceLocator.Instance.GetService<INavigationGraph>();

            InitializeTimer();
            Initialize();
        }

        private void InitializeTimer()
        {
            _timer = new CountdownTimer(rePath);
            _timer.onTimerStop += () => RequestPath(_actualTargetTransform);

            waypointsPath = new List<Vector3>(10);
        }

        protected virtual void Initialize() { }

        private void OnValidate()
        {
            speed = Mathf.Max(0.01f, speed);
            rotationSpeed = Mathf.Max(0.01f, rotationSpeed);
            changeWaypointDistance = Mathf.Max(0.1f, changeWaypointDistance);
            stoppingDistance = Mathf.Max(0f, stoppingDistance);
            rePath = Mathf.Max(0f, rePath);
        }

        protected virtual IEnumerator MoveAgent()
        {
            _timer.Start();
            while (currentWaypoint < waypointsPath.Count)
            {
                _timer.Tick(Time.deltaTime);

                Vector3 distanceToTarget = waypointsPath[currentWaypoint] - ownTransform.position;

                Move(distanceToTarget);
                Rotate(distanceToTarget);
                CheckWaypoints(distanceToTarget);
                yield return null;
            }

            ClearPath();
            _timer.Pause();
            _timer.Reset(rePath);
            StatusPath = PathStatus.Idle;
        }

        protected abstract void Move(Vector3 targetDistance);
        protected abstract void Rotate(Vector3 targetDistance);
        protected abstract bool IsBraking(Vector3 targetDistance);

        protected bool StopMovement(Vector3 targetDistance)
        {
            bool distance = targetDistance.sqrMagnitude < stoppingDistance * stoppingDistance;

            if (distance)
            {
                ClearPath();
                StatusPath = PathStatus.Idle;
                return true;
            }

            return false;
        }

        protected float GetMarginBraking()
        {
            if (stoppingDistance <= 1) return 2;

            float margin = stoppingDistance * 2;
            return margin;
        }

        public virtual bool RequestPath(Transform targetTransform)
        {
            if (StatusPath == PathStatus.Requested) return false;
            if (_timer.IsRunning) return false;

            Vector3 agentPosition = ownTransform.position;
            if (!IsAgentInGrid(graph, ownTransform.position))
            {
                agentPosition = graph.GetNearestWalkableCellPosition(ownTransform.position);
                // Change this cause' the agent maybe isn't on the same height -> This is because a 3D Grid, in a 2D Grid it's okay.
                agentPosition.y = ownTransform.position.y;

                // Change this and obtain the result with the cellSize and cellDiameter, 
                // the min distance to change must be two cells away.
                const float margin = 2f;
                
                // Map the agent if the distance is to far.
                Vector3 distance = agentPosition - ownTransform.position;
                if (distance.sqrMagnitude >= margin * margin)
                {
                    ownTransform.position = agentPosition;
                }
            }

            _actualTargetTransform = targetTransform;
            float3 target = targetTransform.position;
            Cell endCell = graph.GetCellWithWorldPosition(target);
            if (all(lastTargetPosition == endCell.position)) return false;

            StatusPath = PathStatus.Requested;

            Cell startCell = graph.GetCellWithWorldPosition(agentPosition);
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