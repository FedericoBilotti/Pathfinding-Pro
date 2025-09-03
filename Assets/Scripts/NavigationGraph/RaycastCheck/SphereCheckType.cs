using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public class SphereCheckType : CheckType
    {
        private readonly float _radius;

        public SphereCheckType(float gridSizeY, float inclineLimit, float radius, Transform gridTransform, LayerMask notWalkableMask, LayerMask walkableMask) : base(gridSizeY, inclineLimit, gridTransform, notWalkableMask, walkableMask)
        {
            _radius = radius;
        }

        public override WalkableType IsCellWalkable(Vector3 cellPosition)
        {
            float maxHeight = gridTransform.position.y + gridSizeY;
            Vector3 cellPos = new(cellPosition.x, gridTransform.position.y, cellPosition.z);
            Vector3 origin = cellPos + Vector3.up * maxHeight;

            var hitObstacle = Physics.SphereCast(origin, _radius, Vector3.down, out RaycastHit hitInfo, gridSizeY, notWalkableMask.value);
            if (hitObstacle) return WalkableType.Obstacle;

            var hitWalkableArea = Physics.SphereCast(origin, _radius, Vector3.down, out hitInfo, gridSizeY, walkableMask.value);
            if (!hitWalkableArea) return WalkableType.Air;

            if (hitInfo.normal.y <= Mathf.Cos(inclineLimit * Mathf.Deg2Rad))
                return WalkableType.Air;

            return WalkableType.Walkable;
        }
    }

}