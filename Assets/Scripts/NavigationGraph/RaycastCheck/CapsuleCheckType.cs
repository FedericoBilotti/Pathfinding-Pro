using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public class CapsuleCheckType : IRaycastType
    {
        private readonly float _halfHeight;
        private readonly float _radius;
        private readonly float _gridSizeY;
        private readonly float _inclineLimit;

        private readonly LayerMask _notWalkableMask;

        public CapsuleCheckType(float height, float radius, float gridSizeY, float inclineLimit, LayerMask notWalkableMask)
        {
            _halfHeight = height / 2;
            _radius = radius;
            _gridSizeY = gridSizeY;
            _inclineLimit = inclineLimit;
            _notWalkableMask = notWalkableMask;
        }

        public WalkableType IsCellWalkable(Vector3 cellPosition)
        {
            Vector3 p1 = cellPosition + Vector3.up * (_halfHeight + _gridSizeY);
            Vector3 p2 = cellPosition - Vector3.up * (_halfHeight - _gridSizeY);

            var hitObstacles = Physics.CapsuleCast(p1, p2, _radius, Vector3.down, _gridSizeY, _notWalkableMask.value);
            if (hitObstacles) return WalkableType.Obstacle;

            var hitWalkableArea = Physics.CapsuleCast(p1, p2, _radius, Vector3.down, out RaycastHit hitInfo, _gridSizeY * 2, ~_notWalkableMask.value);
            if (!hitWalkableArea) return WalkableType.Air;

            if (hitInfo.normal.y < Mathf.Cos(_inclineLimit * Mathf.Deg2Rad))
                return WalkableType.Obstacle;

            return WalkableType.Walkable;
        }
    }
}