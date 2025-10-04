using System.Collections.Generic;
using NavigationGraph;
using Pathfinding;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
using Utilities.Collections;

namespace Agents
{
    [DefaultExecutionOrder(-600)]
    [RequireComponent(typeof(PathRequester))]
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

        [Header("Agent Settings")]
        [SerializeField, Tooltip("Height offset over the cell")] protected float agentHeightOffset = 1f;
        [SerializeField, Tooltip("Radius of the agent")] protected float agentRadius = 0.5f;

        private IPathfinding _pathfinding;

        protected int currentWaypoint;
        protected List<Vector3> waypointsPath;
        protected float3 finalTargetPosition;
        protected Transform ownTransform;
        protected INavigationGraph graph;
        protected Timer timer;

        public PathStatus StatusPath { get; protected set; } = PathStatus.Idle;
        public bool HasPath => waypointsPath != null && waypointsPath.Count > 0 && StatusPath == PathStatus.Success;
        public bool AutoBraking { get => autoBraking; set => autoBraking = value; }
        public float Speed { get => speed; set => speed = Mathf.Max(0.01f, value); }
        public float RotationSpeed { get => rotationSpeed; set => rotationSpeed = Mathf.Max(0.01f, value); }
        public float ChangeWaypointDistance { get => changeWaypointDistance; set => changeWaypointDistance = Mathf.Max(0.1f, value); }
        public float AgentHeightOffset { get => agentHeightOffset; set => agentHeightOffset = Mathf.Max(0.1f, value); }
        public float AgentRadius { get => agentRadius; set => agentRadius = Mathf.Max(0.1f, value); }
        public float StoppingDistance => stoppingDistance;
        public float3 FinalTargetPosition => finalTargetPosition;

        // For custom inspector
        public List<Vector3> WaypointsPath => waypointsPath;
        public int CurrentWaypoint => currentWaypoint;

        int IIndexed.Index { get; set; }

        private void Awake()
        {
            ownTransform = transform;

            InitializeTimer();
            Initialize();
        }

        private void Start()
        {
            _pathfinding = GetComponent<PathRequester>();
            graph = ServiceLocator.Instance.GetService<INavigationGraph>();
            waypointsPath = new List<Vector3>(graph.GetGridSizeLength() / 7);

            var agentUpdateManager = AgentUpdateManager.Instance;
            if (agentUpdateManager)
                agentUpdateManager.RegisterAgent(this);
        }

        private void OnEnable()
        {
            var agentUpdateManager = AgentUpdateManager.Instance;
            if (agentUpdateManager)
                agentUpdateManager.RegisterAgent(this);
        }

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
            // timer.onTimerStop += OnStopTimer;
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
                return agentPosition;
            }

            float3 distanceToEnd = finalTargetPosition - agentPosition;

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
            if (allowRePath)
            {
                if (timer.IsRunning) return false;
            }
            else
            {
                bool3 samePosition = finalTargetPosition == targetCell.position;
                if (math.all(samePosition)) return false;
            }

            Vector3 nearestWalkableCellPosition = MapAgentToGrid(ownTransform.position);

            // Changed the transform for the cell
            Cell startCell = graph.GetCellWithWorldPosition(nearestWalkableCellPosition);
            Cell endCell = graph.GetCellWithWorldPosition(targetCell.position);
            bool isPathValid = _pathfinding.RequestPath(this, startCell, endCell);

            StatusPath = PathStatus.Requested;

            if (isPathValid)
            {
                finalTargetPosition = endCell.position;
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

        private Vector3 MapAgentToGrid(Vector3 nearestWalkableCellPosition)
        {
            if (!IsAgentInGrid(graph, ownTransform.position))
            {
                nearestWalkableCellPosition = graph.GetNearestWalkableCellPosition(ownTransform.position);
                float changeCell = graph.GetCellDiameter() * 2f;

                Vector3 distance = nearestWalkableCellPosition - ownTransform.position;
                if (distance.sqrMagnitude >= changeCell * changeCell)
                {
                    ownTransform.position = nearestWalkableCellPosition;
                }
            }

            return nearestWalkableCellPosition;
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

        public enum PathStatus : byte
        {
            Idle,
            Failed,
            Requested,
            Success
        }
    }
}