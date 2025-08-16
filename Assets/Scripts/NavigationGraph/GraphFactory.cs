using NavigationGraph;
using NavigationGraph.Graph;
using UnityEngine;

// Convert it to MonoBehavior, and register to the ServiceLocator???
public class GraphFactory
{
    public INavigationGraph Create(NavigationGraphType graphType, float cellSize, float maxDistance, Vector2Int gridSize, LayerMask notWalkableMask, Transform transform, LayerMask walkableMask, LayerMask agentMask,
    float obstacleMargin)
    {
        return graphType switch
        {
            NavigationGraphType.Grid2D => new SimpleGridNavigationGraph(cellSize, maxDistance, gridSize, notWalkableMask, transform, walkableMask, agentMask, obstacleMargin),
            NavigationGraphType.Grid3D => new WorldNavigationGraph(cellSize, maxDistance, gridSize, notWalkableMask, transform, walkableMask, agentMask, obstacleMargin),
            _ => throw new System.NotImplementedException()
        };
    }
}