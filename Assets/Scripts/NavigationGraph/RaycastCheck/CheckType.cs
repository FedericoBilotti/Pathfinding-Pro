using UnityEngine;

namespace NavigationGraph.RaycastCheck
{
    public abstract class CheckType : IRaycastType
    {
        protected readonly float gridSizeY;
        protected readonly float inclineLimit;
        protected readonly Transform gridTransform;
        protected readonly LayerMask notWalkableMask;
        protected readonly LayerMask walkableMask;

        protected CheckType(float gridSizeY, float inclineLimit, Transform gridTransform, LayerMask notWalkableMask, LayerMask walkableMask)
        {
            this.gridSizeY = gridSizeY;
            this.inclineLimit = inclineLimit;
            this.gridTransform = gridTransform;
            this.notWalkableMask = notWalkableMask;
            this.walkableMask = walkableMask;
        }

        public abstract WalkableType IsCellWalkable(Vector3 cellPosition);
    }
}