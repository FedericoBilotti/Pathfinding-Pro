using NavigationGraph;
using Unity.Collections;
using UnityEngine;

namespace Agents
{
    public interface IAgent
    {
        bool RequestPath(Transform endPosition);
        void SetPath(NativeList<Cell> path);
    }
}