using System.Collections.Generic;
using Agents;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Utilities;

[DefaultExecutionOrder(-700)]
public class AgentUpdateManager : Singleton<AgentUpdateManager>
{
    // Maybe I can remove this "_agents" and use only the TransformAccessArray?
    // Using the interface IIndexed, i can use the Index property to remove the transform at the same index of the agent removed.
    // But now i need to manually do it in register and unregister methods.
    private SwapBackList<AgentNavigation> _agents;
    private List<AgentNavigation> _agentsToAdd;
    private List<AgentNavigation> _agentsToRemove;
    private TransformAccessArray _transforms;

    private NativeArray<float3> _finalTargets;
    private NativeArray<float3> _targetPositions;
    private NativeArray<float> _speeds;
    private NativeArray<float> _rotationSpeeds;
    private NativeArray<float> _stoppingDistances;
    private NativeArray<float> _changeWaypointDistances;
    private NativeArray<bool> _autoBraking;
    private JobHandle handle;

    private const int InitialCapacity = 10;

    protected override void InitializeSingleton()
    {
        _agents = new SwapBackList<AgentNavigation>(InitialCapacity);
        _agentsToAdd = new List<AgentNavigation>(InitialCapacity);
        _agentsToRemove = new List<AgentNavigation>(InitialCapacity);
        _transforms = new TransformAccessArray(InitialCapacity);
    }

    public void RegisterAgent(AgentNavigation agent)
    {
        if (!agent || _agents.Contains(agent) || _agentsToAdd.Contains(agent)) return;
        _agentsToAdd.Add(agent);
    }

    public void UnregisterAgent(AgentNavigation agent)
    {
        if (!agent || !_agents.Contains(agent) || _agentsToRemove.Contains(agent)) return;
        _agentsToRemove.Add(agent);
    }

    private void Update()
    {
        ApplyPendingChanges();

        if (_agents.Count == 0 || _transforms.length == 0) return;

        CreateArrays();

        for (int i = 0; i < _agents.Count; i++)
        {
            var agent = _agents[i];
            agent.UpdateTimer();
            _finalTargets[i] = math.all(agent.FinalTargetPosition == float3.zero) ? float3.zero : agent.FinalTargetPosition;
            _targetPositions[i] = agent.GetCurrentTarget();
            _speeds[i] = agent.Speed;
            _rotationSpeeds[i] = agent.RotationSpeed;
            _stoppingDistances[i] = agent.StoppingDistance;
            _changeWaypointDistances[i] = agent.ChangeWaypointDistance;
            _autoBraking[i] = agent.AutoBraking;
        }

        var job = new AgentUpdateJob
        {
            finalTargets = _finalTargets,
            targetPositions = _targetPositions,
            speeds = _speeds,
            rotationSpeeds = _rotationSpeeds,
            stoppingDistances = _stoppingDistances,
            changeWaypointDistances = _changeWaypointDistances,
            autoBraking = _autoBraking,
            deltaTime = Time.deltaTime
        };

        handle = job.Schedule(_transforms);
        handle.Complete();
        DisposeArrays();
    }

    private void ApplyPendingChanges()
    {
        if (!handle.IsCompleted) handle.Complete();

        if (_agentsToAdd.Count > 0)
        {
            foreach (var agent in _agentsToAdd)
            {
                if (agent)
                {
                    // First add to transforms, to respect the index of the agent sync in all lists
                    _transforms.Add(agent.transform);
                    _agents.Add(agent);
                }
            }
            _agentsToAdd.Clear();
        }

        if (_agentsToRemove.Count > 0)
        {
            foreach (var agent in _agentsToRemove)
            {
                if (agent)
                {
                    // First remove from transforms, to respect the index of the agent sync in all lists
                    if (agent is IIndexed indexedAgent)
                    {
                        _transforms.RemoveAtSwapBack(indexedAgent.Index);
                        _agents.Remove(agent);
                    }
                }
            }

            _agentsToRemove.Clear();
        }
    }

    private void CreateArrays()
    {
        int count = _agents.Count;

        _finalTargets = new NativeArray<float3>(count, Allocator.TempJob);
        _targetPositions = new NativeArray<float3>(count, Allocator.TempJob);
        _speeds = new NativeArray<float>(count, Allocator.TempJob);
        _rotationSpeeds = new NativeArray<float>(count, Allocator.TempJob);
        _stoppingDistances = new NativeArray<float>(count, Allocator.TempJob);
        _changeWaypointDistances = new NativeArray<float>(count, Allocator.TempJob);
        _autoBraking = new NativeArray<bool>(count, Allocator.TempJob);
    }

    private void DisposeArrays()
    {
        if (!handle.IsCompleted) handle.Complete();
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
        if (_transforms.isCreated) _transforms.Dispose();
        DisposeArrays();
    }

    [BurstCompile]
    public struct AgentUpdateJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float3> finalTargets;
        [ReadOnly] public NativeArray<float3> targetPositions;
        [ReadOnly] public NativeArray<float> speeds;
        [ReadOnly] public NativeArray<float> rotationSpeeds;
        [ReadOnly] public NativeArray<float> stoppingDistances;
        [ReadOnly] public NativeArray<float> changeWaypointDistances;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public NativeArray<bool> autoBraking;

        public void Execute(int index, TransformAccess transform)
        {
            float3 finalTarget = finalTargets[index];
            if (finalTarget.Equals(float3.zero)) return;

            float3 position = transform.position;
            if (!math.all(math.isfinite(position))) return;

            float3 actualTarget = targetPositions[index];
            if (!math.all(math.isfinite(actualTarget))) return;

            float3 direction = actualTarget - position;
            float3 finalTargetDistance = finalTarget - position;

            if (!math.all(math.isfinite(direction)) || !math.all(math.isfinite(finalTargetDistance)))
                return;

            float stopDist = stoppingDistances[index];
            if (math.lengthsq(finalTargetDistance) < stopDist * stopDist) return;

            float speed = speeds[index];
            float rotationSpeed = rotationSpeeds[index];
            bool braking = autoBraking[index];

            if (braking)
            {
                float distance = math.length(finalTargetDistance);
                float margin = stopDist <= 1f ? 2f : stopDist * 3f;

                if (distance < margin && distance > 0.0001f)
                {
                    float actualSpeed = speed * (distance / margin);
                    position += actualSpeed * deltaTime * math.normalize(direction);
                    RotateTowards(ref transform, direction, rotationSpeed, deltaTime);
                    transform.position = position;
                    return;
                }
            }

            if (math.lengthsq(direction) > 0.0001f)
            {
                position += deltaTime * speed * math.normalize(direction);
                RotateTowards(ref transform, direction, rotationSpeed, deltaTime);
                transform.position = position;
            }
        }

        private static void RotateTowards(ref TransformAccess transform, float3 direction, float rotationSpeed, float deltaTime)
        {
            if (math.lengthsq(direction) < 0.01f) return;
            quaternion targetRotation = quaternion.LookRotationSafe(direction, math.up());
            quaternion newRotation = math.slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            transform.rotation = newRotation;
        }
    }
}