using NavigationGraph.Graph;
using NavigationGraph.RaycastCheck;
using UnityEngine;

namespace NavigationGraph
{
    internal static class GraphFactory
    {
        public static INavigationGraph Create(NavigationGraphType graphType, ICheckType checkType, float cellSize, float maxDistance, Vector2Int gridSize, LayerMask notWalkableMask, Transform transform, LayerMask walkableMask, float obstacleMargin, float cliffMargin)
        {
            return graphType switch
            {
                NavigationGraphType.Grid2D => new SimpleGridNavigationGraph(checkType, cellSize, maxDistance, gridSize, notWalkableMask, transform, walkableMask, obstacleMargin, cliffMargin),
                NavigationGraphType.Grid3D => new WorldNavigationGraph(checkType, cellSize, maxDistance, gridSize, notWalkableMask, transform, walkableMask, obstacleMargin, cliffMargin),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}