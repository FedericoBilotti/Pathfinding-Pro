using UnityEngine;

namespace Agents
{
    public class AgentRigidbodyNavigation : AgentNavigation
    {
        [SerializeField] private Rigidbody _rigidbody;

        protected override void Initialize()
        {
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

        protected override void Move(Vector3 targetDistance)
        {
            if (IsBraking())
                return;
                
            // If makes the camera fill buggy, use AddForce instead of MovePosition or maybe it's the rigidbody that doesn't allow 
            _rigidbody.MovePosition(_rigidbody.position + targetDistance.normalized * (speed * Time.deltaTime));
        }

        protected override void Rotate(Vector3 targetDistance)
        {
            var lookRotation = Quaternion.LookRotation(targetDistance);
            var actualRotation = Quaternion.Slerp(ownTransform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            _rigidbody.MoveRotation(actualRotation);
        }

        protected override bool IsBraking()
        {
            Vector3 target = lastTargetPosition;
            Vector3 direction = target - _rigidbody.position;
            float distance = direction.magnitude;

            if (distance > 0.001f)
            {
                Vector3 move = direction * Time.fixedDeltaTime;

                if (move.magnitude > distance)
                    move = direction;

                _rigidbody.MovePosition(_rigidbody.position + move);

                return true;
            }

            return false;
        }

    }
}