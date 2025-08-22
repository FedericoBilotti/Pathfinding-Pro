using UnityEngine;

namespace Agents
{
    public class AgentTransformNavigation : AgentNavigation
    {
        protected override bool IsBraking(Vector3 targetDistance, Vector3 direction)
        {
            if (!autoBraking) return false;

            float distance = direction.magnitude;
            float margin = GetMarginBraking();

            if (distance >= margin)
                return false;

            var actualSpeed = speed * (distance / GetMarginBraking());
            ownTransform.position += actualSpeed * Time.deltaTime * targetDistance.normalized;

            return true;
        }

        protected override void Move(Vector3 targetDistance)
        {
            Vector3 target = lastTargetPosition;
            Vector3 direction = target - ownTransform.position;
            if (StopMovement(direction)) return;
            if (IsBraking(targetDistance, direction)) return;

            ownTransform.position += targetDistance.normalized * (speed * Time.deltaTime);
        }

        protected override void Rotate(Vector3 targetDistance)
        {
            Quaternion lookRotation = Quaternion.LookRotation(targetDistance);
            ownTransform.rotation = Quaternion.Slerp(ownTransform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
}