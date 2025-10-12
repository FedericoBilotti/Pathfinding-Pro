using NavigationGraph;
using Pathfinding;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
using Utilities.Collections;

namespace Agents
{
    [DefaultExecutionOrder(-600)]
    [RequireComponent(typeof(PathRequester))]
    [SelectionBase]
    public class AgentNavigation : MonoBehaviour, IAgent, IIndexed
    {
        [Header("Steering")]
        [SerializeField] protected float speed = 5f;
        [SerializeField] protected float rotationSpeed = 3f;
        [SerializeField] protected float changeWaypointDistance = 1.5f;
        [SerializeField, Tooltip("Stop from this distance from the target position")] protected float stoppingDistance = 2f;
        [SerializeField, Tooltip("The agent will slowing down in time to reach the target")] protected bool autoBraking = true;

        [SerializeField, HideInInspector, Tooltip("Allow rePath for the agent")] protected bool allowRePath = true;
        [SerializeField, HideInInspector, Tooltip("Time that the agent it's going to ask a new path when reaching a target")] protected float rePath = 0.5f;

        [Header("Agent Settings")]
        [SerializeField, Tooltip("Height offset over the cell")] protected float agentHeightOffset = 1f;
        [SerializeField, Tooltip("Radius of the agent")] protected float agentRadius = 0.5f;

        [SerializeField] private EPathStatus _statusPath = EPathStatus.Idle;

        private IPathRequest _pathfinding;
        private IPathfinder _pathfinder;
        private IGraphProvider _graphProvider;
        private IAgentUpdater _updater;
        private ITimerFactory _timerFactory;
        protected INavigationGraph graph;
        protected Timer timer;

        protected int currentWaypoint;
        protected List<Vector3> waypointsPath;
        protected float3 finalTargetPosition;
        protected Transform _transform;

        public float Speed { get => speed; set => speed = Mathf.Max(0.01f, value); }
        public float RotationSpeed { get => rotationSpeed; set => rotationSpeed = Mathf.Max(0.01f, value); }
        public float ChangeWaypointDistance { get => changeWaypointDistance; set => changeWaypointDistance = Mathf.Max(0.1f, value); }
        public float StoppingDistance => stoppingDistance;
        public bool AutoBraking { get => autoBraking; set => autoBraking = value; }
        public bool AllowRePath { get => allowRePath; set => allowRePath = value; }
        public float RePath { get => rePath; set => rePath = value; }
        public float AgentRadius { get => agentRadius; set => agentRadius = Mathf.Max(0.1f, value); }
        public float AgentHeightOffset { get => agentHeightOffset; set => agentHeightOffset = Mathf.Max(0.1f, value); }
        public bool HasPath => waypointsPath != null && waypointsPath.Count > 0 && StatusPath == EPathStatus.Success;
        public EPathStatus StatusPath { get => _statusPath; set => _statusPath = value; }

        public float3 FinalTargetPosition => finalTargetPosition;

        // For custom inspector
        public List<Vector3> WaypointsPath => waypointsPath;
        public int CurrentWaypoint => currentWaypoint;

        int IIndexed.Index { get; set; }

        private void Awake()
        {
            _transform = transform;
            _pathfinding = GetComponent<IPathRequest>();
            graph = ServiceLocator.Instance.GetService<INavigationGraph>();
            waypointsPath = new List<Vector3>(graph.GetGridSizeLength() / 7);
        }

        public void Initialize(IPathfinder pathfinder, IGraphProvider graphProvider, IAgentUpdater updater, ITimerFactory timerFactory)
        {
            _pathfinder = pathfinder;
            _graphProvider = graphProvider;
            _updater = updater;
            _timerFactory = timerFactory;

            graph = _graphProvider.GetGraph();
            _transform = transform;
            waypointsPath = new List<Vector3>(graph.GetGridSizeLength() / 7);

            timer = _timerFactory.Create<CountdownTimer>(rePath,
                                                         onStop: OnTimerStop);
        }

        private void OnValidate()
        {
            speed = Mathf.Max(0.01f, speed);
            rotationSpeed = Mathf.Max(0.01f, rotationSpeed);
            changeWaypointDistance = Mathf.Max(0.1f, changeWaypointDistance);
            stoppingDistance = Mathf.Clamp(stoppingDistance, changeWaypointDistance + 0.1f, stoppingDistance);
            rePath = Mathf.Max(0f, rePath);
        }

        private void OnEnable() => InitializeTimer();
        private void OnDisable() => DisableTimer();

        private void InitializeTimer()
        {
            timer = _timerFactory.Create<CountdownTimer>(rePath,
                                                         onStop: OnTimerStop);
            timer.onTimerStop += OnTimerStop;
        }

        private void DisableTimer()
        {
            timer.onTimerStop -= OnTimerStop;
        }

        private void OnTimerStop()
        {
            if (!allowRePath) return;
            RequestPath(finalTargetPosition);
        }

        public void UpdateTimer()
        {
            if (!allowRePath) return;
            timer.Tick(Time.deltaTime);
        }

        public float3 GetCurrentTarget()
        {
            float3 agentPosition = (float3)_transform.position;
            if (waypointsPath.Count == 0 || currentWaypoint >= waypointsPath.Count)
            {
                ResetAgent();
                return agentPosition;
            }

            float3 distanceToEnd = finalTargetPosition - agentPosition;
            if (math.lengthsq(distanceToEnd) <= stoppingDistance * stoppingDistance)
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

        public bool RequestPath(Node targetCell)
        {
            finalTargetPosition = targetCell.position;

            if (StatusPath == EPathStatus.Requested) return false;
            if (allowRePath && timer.IsRunning) return false;

            Vector3 nearestWalkableCellPosition = MapAgentToGrid(_transform.position);

            Node startCell = graph.GetNode(nearestWalkableCellPosition);
            bool isPathValid = _pathfinding.RequestPath(this, startCell, targetCell);

            StatusPath = isPathValid ? EPathStatus.Requested : EPathStatus.Failed;

            return isPathValid;
        }

        public bool RequestPath(Transform targetTransform)
        {
            var cell = graph.GetNode(targetTransform.position);
            return RequestPath(cell);
        }

        public bool RequestPath(Vector3 targetPosition)
        {
            var cell = graph.GetNode(targetPosition);
            return RequestPath(cell);
        }

        private Vector3 MapAgentToGrid(Vector3 nearestWalkableCellPosition)
        {
            if (!IsAgentInGrid(graph, _transform.position))
            {
                nearestWalkableCellPosition = graph.TryGetNearestWalkableNode(_transform.position);
                float changeCell = graph.CellDiameter;

                Vector3 distance = nearestWalkableCellPosition - _transform.position;
                if (distance.sqrMagnitude >= changeCell * changeCell)
                {
                    _transform.position = nearestWalkableCellPosition;
                }
            }

            return nearestWalkableCellPosition;
        }

        public virtual void SetPath(in NativeList<Node> path)
        {
            ClearPath();

            foreach (var cell in path)
            {
                waypointsPath.Add(cell.position);
            }

            StatusPath = EPathStatus.Success;
            RegisterAgentToUpdateManager();

            if (!allowRePath) return;

            timer.Reset(rePath);
            timer.Start();
        }

        private void RegisterAgentToUpdateManager()
        {
            var agentUpdateManager = AgentUpdateManager.Instance;
            if (agentUpdateManager)
                agentUpdateManager.RegisterAgent(this);
        }

        private void UnregisterAgentToUpdateManager()
        {
            var agentUpdateManager = AgentUpdateManager.Instance;
            if (agentUpdateManager)
                agentUpdateManager.UnregisterAgent(this);
        }

        private void ResetAgent()
        {
            ClearPath();
            UnregisterAgentToUpdateManager();

            timer.Pause();
            StatusPath = EPathStatus.Idle;
        }

        protected void ClearPath()
        {
            currentWaypoint = 0;
            waypointsPath.Clear();
        }

        private static bool IsAgentInGrid(INavigationGraph graph, Vector3 position) => graph.IsInGrid(position);
    }
}

public enum EPathStatus : byte
{
    Idle,
    Failed,
    Requested,
    Success
}
