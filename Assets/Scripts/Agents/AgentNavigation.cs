using System.Collections.Generic;
using NavigationGraph;
using Pathfinding;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

namespace Agents
{
    public abstract class AgentNavigation : MonoBehaviour, IAgent, IIndexed
    {
        [Header("Steering")]
        [SerializeField] protected float speed = 5;
        [SerializeField] protected float rotationSpeed = 10;
        [SerializeField] protected float changeWaypointDistance = 1.5f;
        [SerializeField, Tooltip("Stop from this distance from the target position")] protected float stoppingDistance = 1f;
        [SerializeField, Tooltip("The agent will slowing down in time to reach the target")] protected bool autoBraking = true;

        [SerializeField, HideInInspector, Tooltip("Allow rePath for the agent")] protected bool allowRePath = true;
        [SerializeField, HideInInspector, Tooltip("Time that the agent it's going to ask a new path when reaching a target")] protected float rePath = 0.5f;

        private IPathfinding _pathfinding;

        protected int currentWaypoint;
        protected INavigationGraph graph;
        protected List<Vector3> waypointsPath;
        protected Transform ownTransform;
        protected Timer timer;

        protected float3 lastTargetPosition = new(0, 0, 0);
        private Cell _agentTargetLastCell;

        public PathStatus StatusPath { get; protected set; } = PathStatus.Idle;
        public bool HasPath => waypointsPath != null && waypointsPath.Count > 0 && StatusPath == PathStatus.Success;
        public bool AutoBraking { get => autoBraking; set => autoBraking = value; }
        public float Speed { get => speed; set => speed = Mathf.Max(0.01f, value); }
        public float RotationSpeed { get => rotationSpeed; set => rotationSpeed = Mathf.Max(0.01f, value); }
        public float ChangeWaypointDistance { get => changeWaypointDistance; set => changeWaypointDistance = Mathf.Max(0.1f, value); }
        public float StoppingDistance => stoppingDistance;
        public float3 LastTargetPosition => lastTargetPosition;

        // For custom inspector
        public List<Vector3> WaypointsPath => waypointsPath;
        public int CurrentWaypoint => currentWaypoint;

        int IIndexed.Index { get; set; }

        private void Awake()
        {
            ownTransform = transform;

            _pathfinding = ServiceLocator.Instance.GetService<IPathfinding>();
            graph = ServiceLocator.Instance.GetService<INavigationGraph>();

            // In the worst case scenario, the agent will have a path of length grid size / 4
            // So for that I divide by 7 to have some extra space.
            waypointsPath = new List<Vector3>(graph.GetGridSize() / 7);

            InitializeTimer();
            Initialize();
        }

        private void OnEnable() => AgentUpdateManager.Instance.RegisterAgent(this);
        private void OnDisable() => AgentUpdateManager.Instance.UnregisterAgent(this);

        private void OnValidate()
        {
            speed = Mathf.Max(0.01f, speed);
            rotationSpeed = Mathf.Max(0.01f, rotationSpeed);
            changeWaypointDistance = Mathf.Max(0.1f, changeWaypointDistance);
            stoppingDistance = Mathf.Max(0f, stoppingDistance);
            rePath = Mathf.Max(0f, rePath);
        }

        public void UpdateTimer()
        {
            if (allowRePath)
                timer.Tick(Time.deltaTime);
        }

        private void InitializeTimer()
        {
            timer = new CountdownTimer(rePath);
            timer.onTimerStop += () =>
            {
                if (allowRePath)
                {
                    timer.Reset(rePath);
                    RequestPath(_agentTargetLastCell);
                }
            };
        }

        protected virtual void Initialize() { }

        protected abstract void Move(Vector3 targetDistance);
        protected abstract void Rotate(Vector3 targetDistance);
        protected abstract bool IsBraking(Vector3 targetDistance, Vector3 direction);

        public float3 GetCurrentTarget()
        {
            float3 agentPosition = (float3)ownTransform.position;
            if (waypointsPath.Count == 0 || currentWaypoint >= waypointsPath.Count)
            {
                ResetAgent();
                return agentPosition;
            }

            float3 distanceToEnd = lastTargetPosition - agentPosition;

            if (math.lengthsq(distanceToEnd) < stoppingDistance * stoppingDistance)
            {
                ResetAgent();
                return agentPosition;
            }

            float3 target = waypointsPath[currentWaypoint];
            float3 distance = target - agentPosition;

            // Check if waypoint reached
            if (math.lengthsq(distance) < changeWaypointDistance * changeWaypointDistance)
                currentWaypoint++;

            return target;
        }

        protected float GetMarginBraking()
        {
            if (stoppingDistance <= 1) return 2;

            float margin = stoppingDistance * 2;
            return margin;
        }

        public bool RequestPath(Cell targetCell)
        {
            if (StatusPath == PathStatus.Requested) return false;
            if (timer.IsRunning) return false;

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

            // Changed the transform for the cell
            _agentTargetLastCell = graph.GetCellWithWorldPosition(targetCell.position);
            Cell endCell = graph.GetCellWithWorldPosition(targetCell.position);
            if (math.all(lastTargetPosition == endCell.position)) return false;

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

        public bool RequestPath(Transform targetTransform)
        {
            var cell = graph.GetCellWithWorldPosition(targetTransform.position);
            return RequestPath(cell);
        }

        public bool RequestPath(Vector3 targetPosition)
        {
            var cell = graph.GetCellWithWorldPosition(targetPosition);
            return RequestPath(cell);
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

            if (allowRePath)
            {
                timer.Reset(rePath);
                timer.Start();
            }
        }

        private void ResetAgent()
        {
            ClearPath();
            timer.Pause();
            StatusPath = PathStatus.Idle;
        }


        protected void ClearPath()
        {
            currentWaypoint = 0;
            waypointsPath.Clear();
        }

        private static bool IsAgentInGrid(INavigationGraph graph, Vector3 position) => graph.IsInGrid(position);


        public enum PathStatus // : byte
        {
            Idle,
            Failed,
            Requested,
            Success
        }
    }
}