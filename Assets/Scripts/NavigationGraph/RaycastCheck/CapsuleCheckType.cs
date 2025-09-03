using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public class CapsuleCheckType : CheckType
    {
        private readonly float _halfHeight;
        private readonly float _radius;

        public CapsuleCheckType(float gridSizeY, float inclineLimit, float radius, float height, Transform gridTransform, LayerMask notWalkableMask, LayerMask walkableMask) : base(gridSizeY, inclineLimit, gridTransform, notWalkableMask, walkableMask)
        {
            _halfHeight = height / 2;
            _radius = radius;
        }

        public override WalkableType IsCellWalkable(Vector3 cellPosition)
        {
            float maxHeight = gridTransform.position.y + gridSizeY;
            Vector3 cellPos = new(cellPosition.x, gridTransform.position.y, cellPosition.z);

            Vector3 p1 = cellPos + Vector3.up * (_halfHeight + maxHeight);
            Vector3 p2 = cellPos - Vector3.up * (_halfHeight - maxHeight);

            var hitObstacles = Physics.CapsuleCast(p1, p2, _radius, Vector3.down, gridSizeY, notWalkableMask.value);
            if (hitObstacles) return WalkableType.Obstacle;

            var hitWalkableArea = Physics.CapsuleCast(p1, p2, _radius, Vector3.down, out RaycastHit hitInfo, gridSizeY * 2, walkableMask.value);
            if (!hitWalkableArea) return WalkableType.Air;

            if (hitInfo.normal.y <= Mathf.Cos(inclineLimit * Mathf.Deg2Rad))
                return WalkableType.Air;

            return WalkableType.Walkable;
        }
    }
}