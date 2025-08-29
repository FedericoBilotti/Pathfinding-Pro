using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public class SphereCheckType : IRaycastType
    {
        private readonly float _radius;
        private readonly float _gridSizeY;
        private readonly float _inclineLimit;

        private readonly LayerMask _notWalkableMask;

        public SphereCheckType(float radius, float gridSizeY, float inclineLimit, LayerMask notWalkableMask)
        {
            _radius = radius;
            _gridSizeY = gridSizeY;
            _inclineLimit = inclineLimit;
            _notWalkableMask = notWalkableMask;
        }

        public WalkableType IsCellWalkable(Vector3 cellPosition)
        {
            Vector3 origin = cellPosition + Vector3.up * _gridSizeY;

            var hitObstacle = Physics.SphereCast(origin, _radius, Vector3.down, out RaycastHit hitInfo, _gridSizeY, _notWalkableMask.value);
            if (hitObstacle) return WalkableType.Obstacle;

            var hitWalkableArea = Physics.SphereCast(origin, _radius, Vector3.down, out hitInfo, _gridSizeY, ~_notWalkableMask.value);
            if (!hitWalkableArea) return WalkableType.Air;

            return WalkableType.Walkable;
        }
    }
}