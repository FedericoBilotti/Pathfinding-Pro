using UnityEngine;

namespace Agents
{
    public class TransformNavigation : AgentNavigation
    {
        // private void Update() => MapToGrid();

        protected override void Move(Vector3 targetDistance)
        {
            ownTransform.position += targetDistance.normalized * (speed * Time.deltaTime);
        }

        protected override void Rotate(Vector3 targetDistance)
        {
            Quaternion lookRotation = Quaternion.LookRotation(targetDistance);
            ownTransform.rotation = Quaternion.Slerp(ownTransform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }
}