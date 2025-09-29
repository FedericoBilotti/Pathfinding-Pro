using Agents;
using NavigationGraph;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Utilities;

[DefaultExecutionOrder(-950)]
public partial class AgentUpdateManager : Singleton<AgentUpdateManager>
{
    // Maybe I can remove this "_agents" and use only the TransformAccessArray?
    // Using the interface IIndexed, i can use the Index property to remove the transform at the same index of the agent removed.
    // But now i need to manually do it in register and unregister methods.
    private SwapBackListIndexed<AgentNavigation> _agents;
    private TransformAccessArray _transforms;

    private NativeList<float3> _finalTargets;
    private NativeList<float3> _targetPositions;
    private NativeList<float> _speeds;
    private NativeList<float> _rotationSpeeds;
    private NativeList<float> _stoppingDistances;
    private NativeList<float> _changeWaypointDistances;
    private NativeList<bool> _autoBraking;
    private JobHandle _handle;

    private const int InitialCapacity = 10;

    protected override void InitializeSingleton()
    {
        _agents = new SwapBackListIndexed<AgentNavigation>(InitialCapacity);
        _transforms = new TransformAccessArray(InitialCapacity);
        CreateArrays(InitialCapacity);
    }

    #region OnEnable & OnDisable

    private void OnEnable()
    {
        var serviceLocator = ServiceLocator.Instance;
        if (serviceLocator == null) return;
        var navigationGraph = serviceLocator.GetService<INavigationGraph>();
        if (navigationGraph == null) return;

        navigationGraph.OnDeleteGrid -= DisposeArrays;
    }

    private void OnDisable()
    {
        _handle.Complete();

        var serviceLocator = ServiceLocator.Instance;
        if (serviceLocator == null) return;
        var navigationGraph = serviceLocator.GetService<INavigationGraph>();
        if (navigationGraph == null) return;

        navigationGraph.OnDeleteGrid -= DisposeArrays;
    }

    #endregion

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
        if (!_handle.IsCompleted)
            return;

        _handle.Complete();
        ClearArrays();

        for (int i = 0; i < _agents.Count; i++)
        {
            var agent = _agents[i];
            agent.UpdateTimer();

            _finalTargets.Add(math.all(agent.FinalTargetPosition == float3.zero) ? float3.zero : agent.FinalTargetPosition);
            _targetPositions.Add(agent.GetCurrentTarget());
            _speeds.Add(agent.Speed);
            _rotationSpeeds.Add(agent.RotationSpeed);
            _stoppingDistances.Add(agent.StoppingDistance);
            _changeWaypointDistances.Add(agent.ChangeWaypointDistance);
            _autoBraking.Add(agent.AutoBraking);
        }

        var navigationGraph = ServiceLocator.Instance.GetService<INavigationGraph>();
        _handle = new AgentUpdateJob
        {
            grid = navigationGraph.GetGrid(),
            gridX = navigationGraph.GetXSize(),
            gridZ = navigationGraph.GetZSize(),

            finalTargets = _finalTargets,
            targetPositions = _targetPositions,

            movementSpeeds = _speeds,
            rotationSpeeds = _rotationSpeeds,
            stoppingDistances = _stoppingDistances,

            changeWaypointDistances = _changeWaypointDistances,
            autoBraking = _autoBraking,

            deltaTime = Time.deltaTime,
            // This two will change in the future.
            agentRadius = 0.5f,
            agentHeightOffset = 1f,
        }.Schedule(_transforms);
    }

    private void ClearArrays()
    {
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
        if (_finalTargets.IsCreated) _finalTargets.Dispose();
        if (_targetPositions.IsCreated) _targetPositions.Dispose();
        if (_speeds.IsCreated) _speeds.Dispose();
        if (_rotationSpeeds.IsCreated) _rotationSpeeds.Dispose();
        if (_stoppingDistances.IsCreated) _stoppingDistances.Dispose();
        if (_changeWaypointDistances.IsCreated) _changeWaypointDistances.Dispose();
        if (_autoBraking.IsCreated) _autoBraking.Dispose();
    }

    private void OnDestroy()
    {
        DisposeArrays();
        if (_transforms.isCreated) _transforms.Dispose();
    }
}