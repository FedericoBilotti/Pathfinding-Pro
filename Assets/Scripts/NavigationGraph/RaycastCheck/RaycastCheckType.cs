using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public class RaycastCheckType : CheckType
    {
        public RaycastCheckType(float gridSizeY, float inclineLimit, Transform gridTransform, LayerMask notWalkableMask, LayerMask walkableMask) : base(gridSizeY, inclineLimit, gridTransform, notWalkableMask, walkableMask) { }

        public override WalkableType IsCellWalkable(Vector3 cellPosition)
        {
            float maxHeight = gridTransform.position.y + gridSizeY;
            Vector3 cellPos = new(cellPosition.x, gridTransform.position.y, cellPosition.z);
            Vector3 origin = cellPos + Vector3.up * maxHeight;

            var hitObstacle = Physics.Raycast(origin, Vector3.down, out RaycastHit hitInfo, gridSizeY, notWalkableMask);
            if (hitObstacle) return WalkableType.Obstacle;

            var hitWalkableArea = Physics.Raycast(origin, Vector3.down, out hitInfo, gridSizeY, walkableMask);
            if (!hitWalkableArea) return WalkableType.Air;

            if (hitInfo.normal.y <= Mathf.Cos(inclineLimit * Mathf.Deg2Rad))
                return WalkableType.Air;

            return WalkableType.Walkable;
        }
    }
}