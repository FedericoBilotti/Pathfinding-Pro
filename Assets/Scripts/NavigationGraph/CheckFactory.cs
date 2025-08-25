using NavigationGraph.Graph;
using NavigationGraph.RaycastCheck;
using UnityEditor;
using UnityEngine;

namespace NavigationGraph
{
    internal static class CheckFactory
    {
        public static ICheckType Create(CheckTypes checkTypes, float maxDistance, float radius, float height, LayerMask notWalkableMask, LayerMask walkableMask)
        {
            return checkTypes switch
            {
                CheckTypes.Raycast => new RaycastCheckType(maxDistance, notWalkableMask, walkableMask),
                CheckTypes.Sphere => new SphereCheckType(radius, notWalkableMask, walkableMask),
                CheckTypes.Capsule => new CapsuleCheckType(height, radius, maxDistance, notWalkableMask, walkableMask),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}