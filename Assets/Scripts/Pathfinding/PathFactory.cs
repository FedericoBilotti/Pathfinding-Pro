using System;
using Agents;
using NavigationGraph;
using Pathfinding.RequesterStrategy;

namespace Pathfinding
{
    public class PathFactory
    {
        public static IPathfinding CreatePathRequester(PathRequestType requestType, INavigationGraph navigationGraph, IAgent agent)
        {
            return requestType switch
            {
                PathRequestType.AStar => new AStarRequester(navigationGraph, agent),
                PathRequestType.ThetaStar => new ThetaStarRequester(navigationGraph, agent),
                _ => throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null)
            };
        }
    }
}
