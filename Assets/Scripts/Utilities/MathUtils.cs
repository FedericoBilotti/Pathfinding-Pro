using Unity.Mathematics;
using UnityEngine.Jobs;

namespace Utilities
{
    public static class math_utils
    {
        public static float3 project_on_plane(float3 vector, float3 planeNormal)
        {
            return vector - math.dot(vector, planeNormal) * planeNormal;
        }

        public static quaternion rotate_towards(TransformAccess transform, float3 lookDir, float3 upDir, float rotationSpeed, float deltaTime)
        {
            if (!math.all(math.isfinite(lookDir)) || !math.all(math.isfinite(upDir))) return quaternion.identity;

            quaternion targetRotation = quaternion.LookRotationSafe(lookDir, upDir);
            return math.slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        }
    }
}
