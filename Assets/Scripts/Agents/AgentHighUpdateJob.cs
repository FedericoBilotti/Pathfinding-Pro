using NavigationGraph;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct AgentHighUpdateJob : IJobParallelForTransform
{
    [ReadOnly] public NativeList<float3> finalTargets;
    [ReadOnly] public NativeList<float3> targetPositions;

    [ReadOnly] public NativeList<float> movementSpeeds;
    [ReadOnly] public NativeList<float> rotationSpeeds;
    [ReadOnly] public NativeList<float> stoppingDistances;

    [ReadOnly] public NativeList<float> changeWaypointDistances;
    [ReadOnly] public NativeList<bool> autoBraking;

    [ReadOnly] public float deltaTime;

    [ReadOnly] public NativeArray<RaycastHit> results;
    [ReadOnly] public NativeArray<Cell> grid;
    [ReadOnly] public int gridX;
    [ReadOnly] public int gridZ;

    [ReadOnly] public float3 gridOrigin;
    [ReadOnly] public float cellSize;
    [ReadOnly] public float cellDiameter;

    public void Execute(int index, TransformAccess transform)
    {
        float3 finalTarget = finalTargets[index];
        if (finalTarget.Equals(float3.zero)) return;

        if (!math.all(math.isfinite(finalTarget))) return;

        float3 position = transform.position;
        if (!math.all(math.isfinite(position))) return;

        float3 actualTarget = targetPositions[index];
        if (!math.all(math.isfinite(actualTarget))) return;

        float3 direction = actualTarget - position;
        if (!math.all(math.isfinite(direction))) return;

        float3 finalTargetDistance = finalTarget - position;
        if (!math.all(math.isfinite(finalTargetDistance))) return;

        float stopDist = stoppingDistances[index];
        if (math.lengthsq(finalTargetDistance) < stopDist * stopDist) return;

        var hit = results[index];

        float3 lookDir = math.normalize(direction);
        RotateTowards(ref transform, lookDir, hit.normal, rotationSpeeds[index], deltaTime);

        float3 forward = math.normalize(ProjectOnPlane(transform.rotation * new float3(0, 0, 1), hit.normal));
        float moveSpeed = movementSpeeds[index];

        if (autoBraking[index])
        {
            float distance = math.length(finalTargetDistance);
            float margin = stopDist <= 1f ? 2f : stopDist * 3f;
            if (distance < margin && distance > 0.0001f)
                moveSpeed *= distance / margin;
        }

        float3 newPos = position + deltaTime * moveSpeed * forward;
        newPos.y = hit.point.y;
        transform.position = newPos;
    }

    private static void RotateTowards(ref TransformAccess transform, float3 direction, float3 up, float rotationSpeed, float deltaTime)
    {
        if (math.lengthsq(direction) < 0.01f) return;
        quaternion targetRotation = quaternion.LookRotationSafe(direction, up);
        quaternion newRotation = math.slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        transform.rotation = newRotation;
    }

    private static float3 ProjectOnPlane(float3 vector, float3 planeNormal)
    {
        return vector - math.dot(vector, planeNormal) * planeNormal;
    }
}
