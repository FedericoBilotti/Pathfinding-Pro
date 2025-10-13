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

        private IPathRequest _pathRequest;
        private IAgentUpdater _updater;
        private ITimerFactory _timerFactory;
        private INavigationGraph _graph;
        private Timer _timer;
        private float3 _finalTargetPosition;
        private Transform _transform;

        public int CurrentWaypoint { get; private set; }
        public List<Vector3> WaypointsPath { get; private set; }
        public float Speed { get => speed; set => speed = Mathf.Max(0.01f, value); }
        public float RotationSpeed { get => rotationSpeed; set => rotationSpeed = Mathf.Max(0.01f, value); }
        public float ChangeWaypointDistance { get => changeWaypointDistance; set => changeWaypointDistance = Mathf.Max(0.1f, value); }
        public float StoppingDistance => stoppingDistance;
        public float RePath { get => rePath; set => rePath = value; }
        public float AgentRadius { get => agentRadius; set => agentRadius = Mathf.Max(0.1f, value); }
        public float AgentHeightOffset { get => agentHeightOffset; set => agentHeightOffset = Mathf.Max(0.1f, value); }
        public bool AutoBraking { get => autoBraking; set => autoBraking = value; }
        public bool AllowRePath { get => allowRePath; set => allowRePath = value; }
        public bool HasPath => WaypointsPath != null && WaypointsPath.Count > 0 && StatusPath == EPathStatus.Success;
        public EPathStatus StatusPath { get => _statusPath; set => _statusPath = value; }

        public float3 FinalTargetPosition => _finalTargetPosition;

        int IIndexed.Index { get; set; }

        public void Awake()
        {
            _transform = transform;

            _updater = new AgentUpdater();
            _timerFactory = new TimerFactory();
            _pathRequest = GetComponent<IPathRequest>();

            _graph = ServiceLocator.Instance.GetService<INavigationGraph>();
            WaypointsPath = new List<Vector3>(_graph.GetGridSizeLength() / 7);
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
        private void OnDisable()
        {
            DisableTimer();
            _updater?.UnregisterAgent(this);
        }

        private void InitializeTimer()
        {
            _timer = _timerFactory.Create<CountdownTimer>(rePath);
            _timer.onTimerStop += OnTimerStop;
        }

        private void DisableTimer()
        {
            _timer.onTimerStop -= OnTimerStop;
        }

        private void OnTimerStop()
        {
            if (!allowRePath) return;
            RequestPath(_finalTargetPosition);
        }

        public void UpdateTimer()
        {
            if (!allowRePath) return;
            _timer.Tick(Time.deltaTime);
        }

        public float3 GetCurrentTarget()
        {
            float3 agentPosition = (float3)_transform.position;
            if (WaypointsPath.Count == 0 || CurrentWaypoint >= WaypointsPath.Count)
            {
                ResetAgent();
                return agentPosition;
            }

            float3 distanceToEnd = _finalTargetPosition - agentPosition;
            if (math.lengthsq(distanceToEnd) <= stoppingDistance * stoppingDistance)
            {
                ResetAgent();
                return agentPosition;
            }

            float3 target = WaypointsPath[CurrentWaypoint];
            float3 distance = target - agentPosition;

            // Check if waypoint reached
            if (math.lengthsq(distance) < changeWaypointDistance * changeWaypointDistance)
                CurrentWaypoint++;

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
            _finalTargetPosition = targetCell.position;

            if (StatusPath == EPathStatus.Requested) return false;
            if (allowRePath && _timer.IsRunning) return false;

            Vector3 nearestWalkableCellPosition = MapAgentToGrid(_transform.position);

            Node startCell = _graph.GetNode(nearestWalkableCellPosition);
            bool isPathValid = _pathRequest.RequestPath(this, startCell, targetCell);

            StatusPath = isPathValid ? EPathStatus.Requested : EPathStatus.Failed;

            return isPathValid;
        }

        public bool RequestPath(Transform targetTransform)
        {
            var cell = _graph.GetNode(targetTransform.position);
            return RequestPath(cell);
        }

        public bool RequestPath(Vector3 targetPosition)
        {
            var cell = _graph.GetNode(targetPosition);
            return RequestPath(cell);
        }

        private Vector3 MapAgentToGrid(Vector3 nearestWalkableCellPosition)
        {
            if (!IsAgentInGrid(_graph, _transform.position))
            {
                nearestWalkableCellPosition = _graph.TryGetNearestWalkableNode(_transform.position);
                float changeCell = _graph.CellDiameter;

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
                WaypointsPath.Add(cell.position);
            }

            StatusPath = EPathStatus.Success;
            _updater?.RegisterAgent(this);

            if (!allowRePath) return;

            _timer.Reset(rePath);
            _timer.Start();
        }

        private void ResetAgent()
        {
            ClearPath();
            _updater?.UnregisterAgent(this);

            _timer.Pause();
            StatusPath = EPathStatus.Idle;
        }

        protected void ClearPath()
        {
            CurrentWaypoint = 0;
            WaypointsPath.Clear();
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
