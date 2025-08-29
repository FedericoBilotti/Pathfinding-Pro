using NavigationGraph.RaycastCheck;
using UnityEngine;

namespace NavigationGraph
{
    internal static class CheckFactory
    {
        public static IRaycastType Create(RaycastType checkTypes, float gridSizeY, float radius, float height, float inclineLimit, LayerMask notWalkableMask)
        {
            return checkTypes switch
            {
                RaycastType.Raycast => new RaycastCheckType(gridSizeY, inclineLimit, notWalkableMask),
                RaycastType.Sphere => new SphereCheckType(radius, gridSizeY, inclineLimit, notWalkableMask),
                RaycastType.Capsule => new CapsuleCheckType(height, radius, gridSizeY, inclineLimit, notWalkableMask),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}