using NavigationGraph;
using Unity.Collections;
using UnityEngine;

namespace Agents
{
    public interface IAgent
    {
        bool RequestPath(Transform targetTransform);
        bool RequestPath(Vector3 targetPosition);
        bool RequestPath(Node targetCell);
        void SetPath(in NativeList<Node> path);
    }
}