using UnityEngine;
using NavigationGraph;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

public partial class AgentUpdateManager
{
    [BurstCompile]
    public struct AgentUpdateJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<Cell> grid;
        [ReadOnly] public int gridX;
        [ReadOnly] public int gridZ;

        [ReadOnly] public NativeList<float3> finalTargets;
        [ReadOnly] public NativeList<float3> targetPositions;

        [ReadOnly] public NativeList<float> movementSpeeds;
        [ReadOnly] public NativeList<float> rotationSpeeds;
        [ReadOnly] public NativeList<float> stoppingDistances;

        [ReadOnly] public NativeList<float> changeWaypointDistances;
        [ReadOnly] public NativeList<bool> autoBraking;

        [ReadOnly] public float deltaTime;
        // Must be lists of agentRadius and agentHeightOffset in the future or maybe do separate jobs for different types of agents.
        [ReadOnly] public float agentRadius;
        [ReadOnly] public float agentHeightOffset;

        public void Execute(int index, TransformAccess transform)
        {
            float3 finalTarget = finalTargets[index];
            if (finalTarget.Equals(float3.zero))
                return;

            if (!math.all(math.isfinite(finalTarget)))
                return;
            float3 position = transform.position;

            if (!math.all(math.isfinite(position)))
                return;

            float3 actualTarget = targetPositions[index];
            if (!math.all(math.isfinite(actualTarget)))
                return;

            float3 direction = actualTarget - position;
            float3 finalTargetDistance = finalTarget - position;

            if (!math.all(math.isfinite(direction)) || !math.all(math.isfinite(finalTargetDistance)))
                return;

            float stopDist = stoppingDistances[index];
            if (math.lengthsq(finalTargetDistance) < stopDist * stopDist)
                return;

            position += deltaTime * movementSpeeds[index] * math.normalize(direction);

            if (autoBraking[index])
            {
                float distance = math.length(finalTargetDistance);
                float margin = stopDist <= 1f ? 2f : stopDist * 3f;

                if (distance < margin && distance > 0.0001f)
                {
                    float actualSpeed = movementSpeeds[index] * (distance / margin);
                    position = transform.position;
                    position += actualSpeed * deltaTime * math.normalize(direction);
                }
            }

            if (math.lengthsq(direction) > 0.0001f)
            {
                if (!math.all(math.isfinite(position))) return;
                RotateTowards(ref transform, direction, rotationSpeeds[index], deltaTime);
                transform.position = position;
            }
        }

        private static void RotateTowards(ref TransformAccess transform, float3 direction, float rotationSpeed, float deltaTime)
        {
            if (math.lengthsq(direction) < 0.01f) return;
            quaternion targetRotation = quaternion.LookRotationSafe(direction, math.up());
            quaternion newRotation = math.slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            transform.rotation = newRotation;
        }

        private Cell GetCellAtPosition(float3 pos)
        {
            int x = math.clamp((int)math.floor(pos.x), 0, gridX - 1);
            int z = math.clamp((int)math.floor(pos.z), 0, gridZ - 1);
            return grid[x + z * gridX];
        }

        private float3 CheckGridCollisions(float3 pos)
        {
            int x = (int)math.floor(pos.x);
            int z = (int)math.floor(pos.z);

            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx;
                    int nz = z + dz;
                    int idx = nx + nz * gridX;

                    if (idx < 0 || idx >= grid.Length) continue;

                    var cell = grid[idx];
                    if (cell.walkableType != WalkableType.Walkable)
                    {
                        // 0.5f is the CellSize
                        float dist = math.distance(new float3(nx + 0.5f, pos.y, nz + 0.5f), pos);
                        if (dist < agentRadius)
                        {
                            float3 pushDir = math.normalize(pos - new float3(nx + 0.5f, pos.y, nz + 0.5f));
                            pos += pushDir * (agentRadius - dist);
                        }
                    }
                }
            }

            return pos;
        }
    }
}