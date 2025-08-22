using UnityEngine;

namespace Agents
{
    /// <summary>
    /// The class is deprecated
    /// </summary>
    public class AgentLerpNavigation : AgentNavigation
    {
        protected override bool IsBraking(Vector3 targetDistance, Vector3 direction)
        {
            if (!autoBraking) return false;

            Vector3 target = lastTargetPosition;
            float distance = direction.magnitude;
            float margin = GetMarginBraking();

            if (distance >= margin)
                return false;

            float dampingFactor = 0.3f;
            float t = speed * Time.deltaTime * (distance / GetMarginBraking()) * dampingFactor;
            ownTransform.position = Vector3.Lerp(ownTransform.position, targetDistance, t);

            return true;
        }

        protected override void Move(Vector3 targetDistance)
        {
            Vector3 target = lastTargetPosition;
            Vector3 direction = target - ownTransform.position;
            if (StopMovement(direction)) return;
            if (IsBraking(targetDistance, direction)) return;

            ownTransform.position = Vector3.MoveTowards(
                ownTransform.position,
                waypointsPath[currentWaypoint],
                speed * Time.deltaTime
            );
        }

        protected override void Rotate(Vector3 targetDistance)
        {
            Quaternion lookRotation = Quaternion.LookRotation(targetDistance);
            ownTransform.rotation = Quaternion.Slerp(ownTransform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
}