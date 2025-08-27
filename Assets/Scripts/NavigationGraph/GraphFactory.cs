using NavigationGraph.Graph;
using NavigationGraph.RaycastCheck;
using UnityEngine;
using static NavigationGraph.NavigationGraphSystem;

namespace NavigationGraph
{
    internal static class GraphFactory
    {
        public static INavigationGraph Create(NavigationGraphType graphType, IRaycastType checkType, TerrainType[] _terrainType, float cellSize, float maxDistance, Vector2Int gridSize, LayerMask notWalkableMask, Transform transform, float obstacleMargin, float cliffMargin)
        {
            return graphType switch
            {
                NavigationGraphType.Grid2D => new SimpleGridNavigationGraph(checkType, _terrainType, cellSize, maxDistance, gridSize, notWalkableMask, transform, obstacleMargin, cliffMargin),
                NavigationGraphType.Grid3D => new WorldNavigationGraph(checkType, _terrainType, cellSize, maxDistance, gridSize, notWalkableMask, transform, obstacleMargin, cliffMargin),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}