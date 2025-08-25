using NavigationGraph.Graph;
using NavigationGraph.RaycastCheck;
using UnityEditor;
using UnityEngine;

namespace NavigationGraph
{
    internal static class CheckFactory
    {
        public static IRaycastType Create(RaycastType checkTypes, float maxDistance, float radius, float height, LayerMask notWalkableMask, LayerMask walkableMask)
        {
            return checkTypes switch
            {
                RaycastType.Raycast => new RaycastCheckType(maxDistance, notWalkableMask, walkableMask),
                RaycastType.Sphere => new SphereCheckType(radius, maxDistance, notWalkableMask, walkableMask),
                RaycastType.Capsule => new CapsuleCheckType(height, radius, maxDistance, notWalkableMask, walkableMask),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}