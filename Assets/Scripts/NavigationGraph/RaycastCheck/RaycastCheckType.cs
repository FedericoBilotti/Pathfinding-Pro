using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public class RaycastCheckType : IRaycastType
    {
        private readonly float _gridSizeY;
        private readonly float _inclineLimit;

        private readonly LayerMask _notWalkableMask;

        public RaycastCheckType(float gridSizeY, float inclineLimit, LayerMask notWalkableMask)
        {
            _gridSizeY = gridSizeY;
            _inclineLimit = inclineLimit;
            _notWalkableMask = notWalkableMask;
        }

        // Pass this to Jobs -> This are raycast, so it can be passed to jobs.
        public WalkableType IsCellWalkable(Vector3 cellPosition)
        {
            Vector3 origin = cellPosition + Vector3.up * _gridSizeY;

            var hitObstacle = Physics.Raycast(origin, Vector3.down, _gridSizeY, _notWalkableMask.value);
            if (hitObstacle) return WalkableType.Obstacle;

            var hitWalkableArea = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _gridSizeY, ~_notWalkableMask.value);
            if (!hitWalkableArea) return WalkableType.Air;

            if (hit.normal.y < Mathf.Cos(_inclineLimit * Mathf.Deg2Rad))
                return WalkableType.Obstacle;

            return WalkableType.Walkable;
        }
    }
}