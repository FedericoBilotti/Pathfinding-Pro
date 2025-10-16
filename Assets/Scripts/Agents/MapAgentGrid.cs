using NavigationGraph;
using UnityEngine;

namespace Agents
{
    public class MapAgentGrid : IMapAgent
    {
        private readonly Transform _transform;

        public MapAgentGrid(Transform transform) => _transform = transform;

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
}