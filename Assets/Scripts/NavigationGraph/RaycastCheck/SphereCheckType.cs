using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public class SphereCheckType : IRaycastType
    {
        private readonly float _radius;
        private readonly float _maxDistance;
        private readonly LayerMask _notWalkableMask;

        public SphereCheckType(float radius, float maxDistance, LayerMask notWalkableMask)
        {
            _radius = radius;
            _maxDistance = maxDistance;
            _notWalkableMask = notWalkableMask;
        }

        public WalkableType IsCellWalkable(Vector3 cellPosition)
        {
            Vector3 origin = cellPosition + Vector3.up * _maxDistance;

            var hitObstacle = Physics.SphereCast(origin, _radius, Vector3.down, out RaycastHit hitInfo, _maxDistance, _notWalkableMask.value);
            if (hitObstacle) return WalkableType.Obstacle;

            var hitWalkableArea = Physics.SphereCast(origin, _radius, Vector3.down, out hitInfo, _maxDistance, ~_notWalkableMask.value);
            if (!hitWalkableArea) return WalkableType.Air;

            return WalkableType.Walkable;
        }
    }
}