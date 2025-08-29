using NavigationGraph.RaycastCheck;
using UnityEngine;

namespace NavigationGraph
{
    internal static class CheckFactory
    {
        public static IRaycastType Create(RaycastType checkTypes, float maxDistance, float radius, float height, LayerMask notWalkableMask)
        {
            return checkTypes switch
            {
                RaycastType.Raycast => new RaycastCheckType(maxDistance, notWalkableMask),
                RaycastType.Sphere => new SphereCheckType(radius, maxDistance, notWalkableMask),
                RaycastType.Capsule => new CapsuleCheckType(height, radius, maxDistance, notWalkableMask),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}