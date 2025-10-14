using NavigationGraph;
using UnityEngine;
using UnityEngine.UIElements;

public class MapAgent : IMapAgent
{
    private readonly Transform _transform;

    public MapAgent(Transform transform) => _transform = transform;

    public Vector3 MapAgentToGrid(INavigationGraph graph, Vector3 from)
    {
        var nearestWalkableCellPosition = graph.TryGetNearestWalkableNode(from);
        float changeCell = graph.CellDiameter;

        Vector3 distance = nearestWalkableCellPosition - from;
        if (distance.sqrMagnitude >= changeCell * changeCell)
        {
            _transform.position = nearestWalkableCellPosition;
        }

        return nearestWalkableCellPosition;
    }
}