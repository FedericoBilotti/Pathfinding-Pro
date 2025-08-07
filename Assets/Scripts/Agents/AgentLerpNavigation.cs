using UnityEngine;

namespace Agents
{
    public class AgentLerpNavigation : AgentNavigation
    {
        protected override void Move(Vector3 targetDistance)
        {
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