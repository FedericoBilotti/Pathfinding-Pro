using Agents;
using Agents.Strategies;
using NavigationGraph;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Utilities;

[DefaultExecutionOrder(-950)]
public class AgentUpdateManager : Singleton<AgentUpdateManager>
{
    [SerializeField, Tooltip("Decides how the agents are going to move in the grid, in less performance")]
    private EAccurateMovement _accurateMovement = EAccurateMovement.High;

    private SwapBackListIndexed<AgentNavigation> _agents;
    private TransformAccessArray _transforms;

    private NativeList<float3> _finalTargets;
    private NativeList<float3> _targetPositions;
    private NativeList<float> _speeds;
    private NativeList<float> _rotationSpeeds;
    private NativeList<float> _stoppingDistances;
    private NativeList<float> _changeWaypointDistances;
    private NativeList<bool> _autoBraking;

    private NativeList<float3> _agentPositions;
    private NativeArray<RaycastCommand> _commands;
    private NativeArray<RaycastHit> _results;
    private JobHandle _agentUpdateJob;

    private const int InitialCapacity = 10;

    protected override void InitializeSingleton()
    {
        _agents = new SwapBackListIndexed<AgentNavigation>(InitialCapacity);
        _transforms = new TransformAccessArray(InitialCapacity);
        CreateArrays(InitialCapacity);
    }

    public void RegisterAgent(AgentNavigation agent)
    {
        if (_agents == null || !_transforms.isCreated) return;
        if (!agent || _agents.Contains(agent)) return;

        _transforms.Add(agent.transform);
        _agents.Add(agent);
    }

    public void UnregisterAgent(AgentNavigation agent)
    {
        if (_agents == null || !_transforms.isCreated) return;
        if (!agent || !_agents.Contains(agent)) return;

#if UNITY_EDITOR
        if (agent is not IIndexed indexedAgent) throw new System.Exception("Agent Navigation doesn't implement IIndexed interface");
#endif

        _transforms.RemoveAtSwapBack(indexedAgent.Index);
        _agents.RemoveAtSwapBack(agent);
    }

    private void Update()
    {
        if (_agents.Count == 0 || _transforms.length == 0) return;
        if (!_agentUpdateJob.IsCompleted) return;

        _agentUpdateJob.Complete();
        ClearArrays();

        for (int i = 0; i < _agents.Count; i++)
        {
            var agent = _agents[i];
            agent.UpdateTimer();

            _agentPositions.Add(agent.transform.position);
            _finalTargets.Add(math.all(agent.FinalTargetPosition == float3.zero) ? float3.zero : agent.FinalTargetPosition);
            _targetPositions.Add(agent.GetCurrentTarget());
            _speeds.Add(agent.Speed);
            _rotationSpeeds.Add(agent.RotationSpeed);
            _stoppingDistances.Add(agent.StoppingDistance);
            _changeWaypointDistances.Add(agent.ChangeWaypointDistance);
            _autoBraking.Add(agent.AutoBraking);
        }

        var navigationGraph = ServiceLocator.Instance.GetService<INavigationGraph>();

        if (EAccurateMovement.High == _accurateMovement)
        {
            HighUpdate();
        }
        else
        {
            LowUpdate(navigationGraph);
        }

        navigationGraph.CombineDependencies(_agentUpdateJob);
    }

    private void HighUpdate()
    {
        ResizeArraysIfNeeded(_transforms.length);
        var navigationGraph = ServiceLocator.Instance.GetService<INavigationGraph>();

        var prepareRaycastCommands = new GroundRaycastSystem()
        {
            commands = _commands,
            originAgentPositions = _agentPositions,
            layerMask = navigationGraph.GetWalkableMask(),
            physicsScene = Physics.defaultPhysicsScene,

            upDirection = Vector3.up,
            rayDistance = 2f
        };

        int batch = 32;
        JobHandle prepareCmdJob = prepareRaycastCommands.ScheduleByRef(_commands.Length, batch);
        JobHandle batchHandle = RaycastCommand.ScheduleBatch(_commands, _results, batch, prepareCmdJob);

        _agentUpdateJob = new AgentHighUpdateJob
        {
            finalTargets = _finalTargets,
            targetPositions = _targetPositions,

            movementSpeeds = _speeds,
            rotationSpeeds = _rotationSpeeds,
            stoppingDistances = _stoppingDistances,

            changeWaypointDistances = _changeWaypointDistances,
            autoBraking = _autoBraking,

            deltaTime = Time.deltaTime,

            results = _results,
            grid = navigationGraph.GetGrid(),
            gridX = navigationGraph.GetXSize(),
            gridZ = navigationGraph.GetZSize(),
            gridOrigin = navigationGraph.GetOrigin(),
            cellSize = navigationGraph.GetCellSize(),
            cellDiameter = navigationGraph.GetCellDiameter(),

            // agentRadius = 0.5f,
            // agentHeightOffset = 1f,
        }.Schedule(_transforms, batchHandle);
    }

    private void ResizeArraysIfNeeded(int newCapacity)
    {
        if (newCapacity == _results.Length) return;
        
        if (_commands.IsCreated) _commands.Dispose();
        if (_results.IsCreated) _results.Dispose();

        _commands = new NativeArray<RaycastCommand>(newCapacity, Allocator.Persistent);
        _results = new NativeArray<RaycastHit>(newCapacity, Allocator.Persistent);
    }

    private void LowUpdate(INavigationGraph navigationGraph)
    {
        _agentUpdateJob = new AgentLowUpdateJob
        {
            finalTargets = _finalTargets,
            targetPositions = _targetPositions,

            movementSpeeds = _speeds,
            rotationSpeeds = _rotationSpeeds,
            stoppingDistances = _stoppingDistances,

            changeWaypointDistances = _changeWaypointDistances,
            autoBraking = _autoBraking,

            deltaTime = Time.deltaTime,

            grid = navigationGraph.GetGrid(),
            gridX = navigationGraph.GetXSize(),
            gridZ = navigationGraph.GetZSize(),
            gridOrigin = navigationGraph.GetOrigin(),
            cellSize = navigationGraph.GetCellSize(),
            cellDiameter = navigationGraph.GetCellDiameter(),
        }.Schedule(_transforms);
    }

    private void ClearArrays()
    {
        _agentPositions.Clear();

        _finalTargets.Clear();
        _targetPositions.Clear();
        _speeds.Clear();
        _rotationSpeeds.Clear();
        _stoppingDistances.Clear();
        _changeWaypointDistances.Clear();
        _autoBraking.Clear();
    }

    private void CreateArrays(int count)
    {
        _commands = new NativeArray<RaycastCommand>(count, Allocator.Persistent);
        _results = new NativeArray<RaycastHit>(count, Allocator.Persistent);
        _agentPositions = new NativeList<float3>(count, Allocator.Persistent);

        _finalTargets = new NativeList<float3>(count, Allocator.Persistent);
        _targetPositions = new NativeList<float3>(count, Allocator.Persistent);
        _speeds = new NativeList<float>(count, Allocator.Persistent);
        _rotationSpeeds = new NativeList<float>(count, Allocator.Persistent);
        _stoppingDistances = new NativeList<float>(count, Allocator.Persistent);
        _changeWaypointDistances = new NativeList<float>(count, Allocator.Persistent);
        _autoBraking = new NativeList<bool>(count, Allocator.Persistent);
    }

    private void DisposeArrays()
    {
        if (_results.IsCreated) _results.Dispose(_agentUpdateJob);
        if (_commands.IsCreated) _commands.Dispose(_agentUpdateJob);
        if (_agentPositions.IsCreated) _agentPositions.Dispose(_agentUpdateJob);

        if (_finalTargets.IsCreated) _finalTargets.Dispose(_agentUpdateJob);
        if (_targetPositions.IsCreated) _targetPositions.Dispose(_agentUpdateJob);
        if (_speeds.IsCreated) _speeds.Dispose(_agentUpdateJob);
        if (_rotationSpeeds.IsCreated) _rotationSpeeds.Dispose(_agentUpdateJob);
        if (_stoppingDistances.IsCreated) _stoppingDistances.Dispose(_agentUpdateJob);
        if (_changeWaypointDistances.IsCreated) _changeWaypointDistances.Dispose(_agentUpdateJob);
        if (_autoBraking.IsCreated) _autoBraking.Dispose(_agentUpdateJob);
    }

    private void OnDestroy()
    {
        DisposeArrays();
        if (_transforms.isCreated) _transforms.Dispose();
    }

    private enum EAccurateMovement
    {
        High,
        Low
    }
}