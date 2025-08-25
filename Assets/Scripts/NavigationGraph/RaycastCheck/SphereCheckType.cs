using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public class SphereCheckType : ICheckType
    {
        private readonly float _radius;
        private readonly LayerMask _notWalkableMask;
        private readonly LayerMask _walkableMask;

        public SphereCheckType(float radius, LayerMask notWalkableMask, LayerMask walkableMask)
        {
            _radius = radius;
            _notWalkableMask = notWalkableMask;
            _walkableMask = walkableMask;
        }

        public WalkableType IsCellWalkable(Vector3 cellPosition)
        {
            Vector3 origin = cellPosition + Vector3.up * 0.1f;

            var hitObstacles = Physics.CheckSphere(origin, _radius, _notWalkableMask.value);
            if (hitObstacles) return WalkableType.Obstacle;

            bool hitWalkableArea = Physics.CheckSphere(origin, _radius, _walkableMask.value);
            if (!hitWalkableArea) return WalkableType.Air;

            return WalkableType.Walkable;
        }
    }
}