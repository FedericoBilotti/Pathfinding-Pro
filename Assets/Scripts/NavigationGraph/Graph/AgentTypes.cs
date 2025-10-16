using System;

namespace NavigationGraph
{
    [Serializable]
    public struct AgentTypes
    {
        public string name;

        public float maxSlope; 
        public float height ;
        public float radius;
        // public LayerMask GroundMask;
    }
}