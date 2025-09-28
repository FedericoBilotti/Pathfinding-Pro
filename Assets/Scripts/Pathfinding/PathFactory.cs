using System;
using NavigationGraph;
using Pathfinding.RequesterStrategy;

namespace Pathfinding
{
    public class PathFactory
    {
        public static IPathRequest CreatePathRequester(PathRequestType requestType, INavigationGraph navigationGraph)
        {
            return requestType switch
            {
                PathRequestType.AStar => new AStarRequester(navigationGraph),
                PathRequestType.ThetaStar => new ThetaStarRequester(navigationGraph),
                _ => throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null)
            };
        }
    }
}
