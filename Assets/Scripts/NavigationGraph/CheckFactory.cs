using NavigationGraph.RaycastCheck;
using UnityEngine;

namespace NavigationGraph
{
    internal static class CheckFactory
    {
        public static IRaycastType Create(RaycastType checkTypes, float gridSizeY, float inclineLimit, float radius, float height, Transform gridTransform, LayerMask notWalkableMask, LayerMask walkableMask)
        {
            return checkTypes switch
            {
                RaycastType.Raycast => new RaycastCheckType(gridSizeY, inclineLimit, gridTransform, notWalkableMask, walkableMask),
                RaycastType.Sphere => new SphereCheckType(gridSizeY, inclineLimit, radius, gridTransform, notWalkableMask, walkableMask),
                RaycastType.Capsule => new CapsuleCheckType(gridSizeY, inclineLimit, radius, height, gridTransform, notWalkableMask, walkableMask),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}