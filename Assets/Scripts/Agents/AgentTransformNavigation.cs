using UnityEngine;

namespace Agents
{
    public class AgentTransformNavigation : AgentNavigation
    {
        protected override bool IsBraking(Vector3 direction)
        {
            float distance = direction.magnitude;

            bool braking = autoBraking && distance < GetMarginBraking();

            if (!braking) return false;

            var actualSpeed = speed * (distance / GetMarginBraking());
            ownTransform.position += actualSpeed * Time.deltaTime * direction.normalized;

            return true;
        }

        protected override void Move(Vector3 targetDistance)
        {
            Vector3 target = lastTargetPosition;
            Vector3 direction = target - ownTransform.position;
            if (StopMovement(direction)) return;
            if (IsBraking(direction)) return;

            ownTransform.position += targetDistance.normalized * (speed * Time.deltaTime);
        }

        protected override void Rotate(Vector3 targetDistance)
        {
            Quaternion lookRotation = Quaternion.LookRotation(targetDistance);
            ownTransform.rotation = Quaternion.Slerp(ownTransform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
}