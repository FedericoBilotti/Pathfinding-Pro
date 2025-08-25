using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public class RaycastCheckType : ICheckType
    {
        private readonly float _maxDistance;
        private readonly LayerMask _notWalkableMask;
        private readonly LayerMask _walkableMask;

        public RaycastCheckType(float maxDistance, LayerMask notWalkableMask, LayerMask walkableMask)
        {
            _maxDistance = maxDistance;
            _notWalkableMask = notWalkableMask;
            _walkableMask = walkableMask;
        }

        // Pass this to Jobs -> This are raycast, so it can be passed to jobs.
        public WalkableType IsCellWalkable(Vector3 cellPosition)
        {
            Vector3 origin = cellPosition + Vector3.up * _maxDistance;

            var hitObstacle = Physics.Raycast(origin, Vector3.down, _maxDistance, _notWalkableMask.value);
            if (hitObstacle) return WalkableType.Obstacle;

            var hitWalkableArea = Physics.Raycast(origin, Vector3.down, _maxDistance, _walkableMask.value);
            if (!hitWalkableArea) return WalkableType.Air;

            return WalkableType.Walkable;
        }
    }
}