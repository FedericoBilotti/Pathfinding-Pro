using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public class RaycastCheckType : IRaycastType
    {
        private readonly float _maxDistance;
        private readonly LayerMask _notWalkableMask;

        public RaycastCheckType(float maxDistance, LayerMask notWalkableMask)
        {
            _maxDistance = maxDistance;
            _notWalkableMask = notWalkableMask;
        }

        // Pass this to Jobs -> This are raycast, so it can be passed to jobs.
        public WalkableType IsCellWalkable(Vector3 cellPosition)
        {
            Vector3 origin = cellPosition + Vector3.up * _maxDistance;

            var hitObstacle = Physics.Raycast(origin, Vector3.down, _maxDistance, _notWalkableMask.value);
            if (hitObstacle) return WalkableType.Obstacle;

            var hitWalkableArea = Physics.Raycast(origin, Vector3.down, _maxDistance, ~_notWalkableMask.value);
            if (!hitWalkableArea) return WalkableType.Air;

            return WalkableType.Walkable;
        }
    }
}