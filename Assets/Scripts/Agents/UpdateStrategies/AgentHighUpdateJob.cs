using NavigationGraph;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Utilities;

namespace Agents.UpdateStrategies
{
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
        [ReadOnly] public NativeArray<Node> grid;
        [ReadOnly] public int gridX;
        [ReadOnly] public int gridZ;

        [ReadOnly] public float3 gridOrigin;
        [ReadOnly] public float cellSize;
        [ReadOnly] public float cellDiameter;

        const float SLIDE_LERP_SPEED = 50f;
        const float Y_POS_LERP_SPEED = 5f;

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

            Rotate(index, ref transform, direction, ref hit);

            float3 forward = math.normalize(math_utils.project_on_plane(transform.rotation * math.forward(), hit.normal));
            float moveSpeed = movementSpeeds[index];

            if (autoBraking[index])
            {
                float distance = math.length(finalTargetDistance);
                float margin = stopDist <= 1f ? 2f : stopDist * 3f;
                if (distance < margin && distance > 0.0001f)
                    moveSpeed *= distance / margin;
            }

            float3 newPos = position + deltaTime * moveSpeed * forward;
            //newPos.y = hit.point.y;
            newPos.y = math.lerp(newPos.y, hit.point.y, deltaTime * Y_POS_LERP_SPEED);
            transform.position = SlideAlongObstacle(position, newPos);
        }

        private void Rotate(int index, ref TransformAccess transform, float3 direction, ref RaycastHit hit)
        {
            float3 lookDir = math.normalize(direction);
            transform.rotation = math_utils.rotate_towards(transform, lookDir, hit.normal, rotationSpeeds[index], deltaTime);
        }

        private float3 SlideAlongObstacle(float3 current, float3 target)
        {
            if (!IsBlocked(target))
                return target;

            float3 delta = target - current;
            float3 move = float3.zero;

            float3 tryX = new(target.x, current.y, current.z);
            if (!IsBlocked(tryX))
            {
                move.x = delta.x;
            }

            float3 tryZ = new(current.x, current.y, target.z);
            if (!IsBlocked(tryZ))
            {
                move.z = delta.z;
            }

            float3 newPos = current + move;
            newPos = math.lerp(current, newPos, SLIDE_LERP_SPEED * deltaTime);
            return newPos;
        }

        private bool IsBlocked(float3 pos)
        {
            float3 localPos = pos - gridOrigin;
            int x = (int)math.clamp(math.floor(localPos.x / cellDiameter), 0, gridX - 1);
            int z = (int)math.clamp(math.floor(localPos.z / cellDiameter), 0, gridZ - 1);

            var cell = grid[x + z * gridX];
            return cell.walkableType != WalkableType.Walkable;
        }
    }
}