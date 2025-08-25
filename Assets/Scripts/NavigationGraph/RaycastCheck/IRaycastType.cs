using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public interface IRaycastType
    {
        WalkableType IsCellWalkable(Vector3 cellPosition);
    }
}