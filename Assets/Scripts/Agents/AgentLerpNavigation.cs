using UnityEngine;

namespace Agents
{
    public class AgentLerpNavigation : AgentNavigation
    {
        protected override bool IsBraking()
        {
            Vector3 target = lastTargetPosition;
            Vector3 direction = target - ownTransform.position;
            float distance = direction.magnitude;

            bool braking = autoBraking && distance < stoppingDistance;

            if (braking && distance > 0.001f)
            {
                float dampingFactor = 0.3f;
                float t = 1 - Mathf.Exp(-speed * Time.deltaTime) * dampingFactor;
                ownTransform.position = Vector3.Lerp(ownTransform.position, target, t);

                return true;
            }

            return false;
        }

        protected override void Move(Vector3 targetDistance)
        {
            if (IsBraking())
                return;

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