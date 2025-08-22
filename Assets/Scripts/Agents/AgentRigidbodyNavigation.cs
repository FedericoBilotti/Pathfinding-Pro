using System.Collections;
using UnityEngine;

namespace Agents
{
    /// <summary>
    /// The class is deprecated
    /// </summary>
    public class AgentRigidbodyNavigation : AgentNavigation
    {
        [Header("Rigidbody Settings")]
        [SerializeField] private bool _initializeRigidbody;
        [SerializeField] private Rigidbody _rigidbody;

        protected override void Initialize()
        {
            if (_initializeRigidbody) return;

            if (_rigidbody)
            {
                InitializeRigidbody();
                return;
            }

            _rigidbody = gameObject.GetOrAdd<Rigidbody>();
            InitializeRigidbody();
        }

        public void InitializeRigidbody()
        {
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationY;
        }

        protected override IEnumerator MoveAgent()
        {
            if (allowRePath)
                timer.Start();

            while (currentWaypoint < waypointsPath.Count)
            {
                Vector3 distanceToTarget = waypointsPath[currentWaypoint] - ownTransform.position;

                Move(distanceToTarget);
                Rotate(distanceToTarget);
                CheckWaypoints(distanceToTarget);
                yield return new WaitForFixedUpdate();
            }

            ClearPath();
            timer.Pause();
            timer.Reset(rePath);
            StatusPath = PathStatus.Idle;
        }

        protected override void Move(Vector3 targetDistance)
        {
            Vector3 target = lastTargetPosition;
            Vector3 direction = target - ownTransform.position;
            if (StopMovement(direction)) return;
            if (IsBraking(targetDistance, direction)) return;

            // If makes the camera fill buggy, use AddForce instead of MovePosition or maybe it's the rigidbody that doesn't allow 
            _rigidbody.MovePosition(_rigidbody.position + targetDistance.normalized * (speed * Time.deltaTime));
        }

        protected override void Rotate(Vector3 targetDistance)
        {
            var lookRotation = Quaternion.LookRotation(targetDistance);
            var actualRotation = Quaternion.Slerp(ownTransform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            _rigidbody.MoveRotation(actualRotation);
        }

        protected override bool IsBraking(Vector3 targetDistance, Vector3 direction)
        {
            if (!autoBraking) return false;

            float distance = direction.magnitude;
            float margin = GetMarginBraking();

            if (distance >= margin)
                return false;

            float actualSpeed = speed * (distance / GetMarginBraking());
            _rigidbody.MovePosition(_rigidbody.position + targetDistance.normalized * (actualSpeed * Time.fixedDeltaTime));

            return true;
        }
    }
}