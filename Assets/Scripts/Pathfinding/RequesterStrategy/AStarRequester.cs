using Agents;
using NavigationGraph;
using Pathfinding.PathImplementation;
using Unity.Jobs;

namespace Pathfinding.RequesterStrategy
{
    public class AStarRequester : Pathfinding
    {
        public AStarRequester(INavigationGraph navigationGraph) : base(navigationGraph) { }

        public override bool RequestPath(IAgent agent, Node start, Node end)
        {
            if (end.walkableType == WalkableType.Obstacle) return false;

            PathRequest pathRequest = pathRequestPool.Get();

            AStarJob aStarJobData = new AStarJob
            {
                grid = navigationGraph.GetGraph(),
                allNeighbors = navigationGraph.GetNeighbors(),
                neighborCounts = navigationGraph.GetNeighborTotalCount(),
                neighborOffSet = navigationGraph.GetNeighborOffsets(),
                closedList = pathRequest.closedList,
                openList = pathRequest.openList,
                visitedNodes = pathRequest.visitedNodes,
                gridSizeX = navigationGraph.GetXSize(),
                startIndex = start.gridIndex,
                endIndex = end.gridIndex
            };

            JobHandle aStarJob = aStarJobData.ScheduleByRef();

            JobHandle addPath = new AddPath
            {
                grid = navigationGraph.GetGraph(),
                finalPath = pathRequest.path,
                visitedNodes = pathRequest.visitedNodes,
                endIndex = end.gridIndex
            }.Schedule(aStarJob);

            JobHandle reversePath = new ReversePath
            {
                finalPath = pathRequest.path
            }.Schedule(addPath);

            navigationGraph.CombineDependencies(reversePath);

            pathRequest.agent = agent;
            pathRequest.handle = reversePath;
            requests.Add(pathRequest);

            return true;
        }
    }
}