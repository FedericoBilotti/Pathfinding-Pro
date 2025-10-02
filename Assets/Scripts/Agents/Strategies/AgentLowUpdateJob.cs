using NavigationGraph;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace Agents.Strategies
{
    [BurstCompile]
    public struct AgentLowUpdateJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeList<float3> finalTargets;
        [ReadOnly] public NativeList<float3> targetPositions;

        [ReadOnly] public NativeList<float> movementSpeeds;
        [ReadOnly] public NativeList<float> rotationSpeeds;
        [ReadOnly] public NativeList<float> stoppingDistances;

        [ReadOnly] public NativeList<float> changeWaypointDistances;
        [ReadOnly] public NativeList<bool> autoBraking;

        [ReadOnly] public float deltaTime;

        [ReadOnly] public NativeArray<Cell> grid;
        [ReadOnly] public int gridX;
        [ReadOnly] public int gridZ;

        [ReadOnly] public float3 gridOrigin;
        [ReadOnly] public float cellSize;
        [ReadOnly] public float cellDiameter;

        // Must be lists of agentRadius and agentHeightOffset in the future or maybe do separate jobs for different types of agents.
        // [ReadOnly] public float agentRadius;
        // [ReadOnly] public float agentHeightOffset;

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

            float3 localPos = position - gridOrigin;
            int x0 = (int)math.clamp(math.floor(localPos.x / cellDiameter), 0, gridX - 1);
            int z0 = (int)math.clamp(math.floor(localPos.z / cellDiameter), 0, gridZ - 1);
            int x1 = math.min(x0 + 1, gridX - 1);
            int z1 = math.min(z0 + 1, gridZ - 1);

            float tx = (localPos.x / cellDiameter) - x0;
            float tz = (localPos.z / cellDiameter) - z0;

            Cell x0Cell = grid[x0 + z0 * gridX];
            Cell x1Cell = grid[x1 + z0 * gridX];
            Cell z0Cell = grid[x0 + z1 * gridX];
            Cell z1Cell = grid[x1 + z1 * gridX];

            float3 finalNormal = BilinealInterpolationNormal(ref x0Cell, ref x1Cell, ref z0Cell, ref z1Cell, tx, tz);
            float3 finalHeight = BilinealInterpolationHeight(ref x0Cell, ref x1Cell, ref z0Cell, ref z1Cell, tx, tz);
            
            Rotate(ref transform, index, direction, finalNormal);

            float3 forward = math.normalize(math_utils.project_on_plane(transform.rotation * math.forward(), finalNormal));
            float moveSpeed = movementSpeeds[index];

            if (autoBraking[index])
            {
                float distance = math.length(finalTargetDistance);
                float margin = stopDist <= 1f ? 2f : stopDist * 3f;
                if (distance < margin && distance > 0.0001f)
                    moveSpeed *= distance / margin;
            }

            float3 newPos = position + deltaTime * moveSpeed * forward;
            newPos.y = finalHeight.y;
            transform.position = SlideAlongObstacle(position, newPos);
        }

        private void Rotate(ref TransformAccess transform, int index, float3 direction, float3 finalNormal)
        {
            float3 lookDir = math.normalize(direction);
            transform.rotation = math_utils.rotate_towards(transform, lookDir, finalNormal, rotationSpeeds[index], deltaTime);
        }

        private readonly float3 BilinealInterpolationNormal(ref Cell x0, ref Cell x1, ref Cell z0, ref Cell z1, float tx, float tz)
        {
            float3 n00 = x0.normal;
            float3 n10 = x1.normal;
            float3 nX0 = math.lerp(n00, n10, tx);

            float3 n01 = z0.normal;
            float3 n11 = z1.normal;
            float3 nX1 = math.lerp(n01, n11, tx);

            float3 finalNormalUn = math.lerp(nX0, nX1, tz);
            float len = math.length(finalNormalUn);
            return len > 0.0001f ? finalNormalUn / len : math.up();
        }

        private readonly float3 BilinealInterpolationHeight(ref Cell x0, ref Cell x1, ref Cell z0, ref Cell z1, float tx, float tz)
        {
            float3 p00 = x0.position;
            float3 p10 = x1.position;
            float3 posX0 = math.lerp(p00, p10, tx);

            float3 p01 = z0.position;
            float3 p11 = z1.position;
            float3 posX1 = math.lerp(p01, p11, tx);
            float3 finalPos = math.lerp(posX0, posX1, tz);
            return finalPos;
        }

        const float LERP_SPEED = 50f;

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
            newPos = math.lerp(current, newPos, LERP_SPEED * deltaTime);
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