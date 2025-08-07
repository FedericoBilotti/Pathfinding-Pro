using UnityEngine;

namespace Agents
{
    public class AgentLerpNavigation : AgentNavigation
    {
        protected override void Move(Vector3 targetDistance)
        {
            // FIX THIS.
            ownTransform.position = Vector3.Lerp(ownTransform.position, targetDistance, speed * Time.deltaTime);
        }

        protected override void Rotate(Vector3 targetDistance)
        {
            Quaternion lookRotation = Quaternion.LookRotation(targetDistance);
            ownTransform.rotation = Quaternion.Slerp(ownTransform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
}