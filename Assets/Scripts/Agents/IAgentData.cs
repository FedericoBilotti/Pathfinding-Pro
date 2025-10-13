using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Agents
{
    public interface IAgentData
    {
        public EPathStatus StatusPath { get; }
        public int CurrentWaypoint { get; }
        public List<Vector3> WaypointsPath { get; }
        public float Speed { get; }
        public float RotationSpeed { get; }
        public float ChangeWaypointDistance { get; }
        public float StoppingDistance { get; }
        public float RePath { get; }
        public float AgentRadius { get; }
        public float AgentHeightOffset { get; }
        public bool AutoBraking { get; }
        public bool AllowRePath { get; }
        public bool HasPath { get; }
        public float3 FinalTargetPosition { get; }
    }
}
