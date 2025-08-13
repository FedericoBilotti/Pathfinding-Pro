using UnityEngine;

namespace Agents
{
    public class TransformNavigation : AgentNavigation
    {
        protected override bool IsBraking()
        {
            Vector3 target = lastTargetPosition;
            Vector3 direction = target - ownTransform.position;
            float distance = direction.magnitude;

            bool braking = autoBraking && distance < stoppingDistance;

            if (braking && distance > 0.001f)
            {
                Vector3 move = Time.deltaTime * direction;

                if (move.magnitude > distance)
                    move = direction;

                ownTransform.position += move;

                return true;
            }

            return false;
        }

        protected override void Move(Vector3 targetDistance)
        {
            if (IsBraking())
                return;

            ownTransform.position += targetDistance.normalized * (speed * Time.deltaTime);
        }

        protected override void Rotate(Vector3 targetDistance)
        {
            Quaternion lookRotation = Quaternion.LookRotation(targetDistance);
            ownTransform.rotation = Quaternion.Slerp(ownTransform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
}