using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public class CapsuleCheckType : ICheckType
    {
        private readonly float _halfHeight;
        private readonly float _radius;
        private readonly float _maxDistance;
        private readonly LayerMask _notWalkableMask;
        private readonly LayerMask _walkableMask;

        public CapsuleCheckType(float height, float radius, float maxDistance, LayerMask notWalkableMask, LayerMask walkableMask)
        {
            _halfHeight = height / 2;
            _radius = radius;
            _maxDistance = maxDistance;
            _notWalkableMask = notWalkableMask;
            _walkableMask = walkableMask;
        }

        public WalkableType IsCellWalkable(Vector3 cellPosition)
        {
            Vector3 p1 = cellPosition + Vector3.up * _halfHeight;
            Vector3 p2 = cellPosition - Vector3.up * _halfHeight;

            var hitObstacles = Physics.CapsuleCast(p1, p2, _radius, Vector3.down, _maxDistance, _notWalkableMask.value);
            if (hitObstacles) return WalkableType.Obstacle;

            var hitWalkableArea = Physics.CapsuleCast(p1, p2, _radius, Vector3.down, _maxDistance, _walkableMask.value);
            if (!hitWalkableArea) return WalkableType.Air;

            return WalkableType.Walkable;
        }
    }
}