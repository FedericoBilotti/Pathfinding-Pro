using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public interface ICheckType
    {
        WalkableType IsCellWalkable(Vector3 cellPosition);
    }
}