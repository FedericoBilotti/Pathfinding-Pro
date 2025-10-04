using Agents;
using NavigationGraph;
using Pathfinding.PathImplementation;
using Unity.Jobs;

namespace Pathfinding.RequesterStrategy
{
    public class ThetaStarRequester : Pathfinding
    {
        public ThetaStarRequester(INavigationGraph navigationGraph) : base(navigationGraph) { }

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

            ThetaStarJob thetaStarJob = new ThetaStarJob
            {
                grid = navigationGraph.GetGraph(),
                gridSizeX = navigationGraph.GetXSize(),
                finalPath = pathRequest.path,
                simplified = pathRequest.simplified
            };

            JobHandle thetaStarHandle = thetaStarJob.ScheduleByRef(addPath);

            JobHandle reversePath = new ReversePath
            {
                finalPath = pathRequest.path
            }.Schedule(thetaStarHandle);

            navigationGraph.CombineDependencies(reversePath);

            pathRequest.agent = agent;
            pathRequest.handle = reversePath;
            requests.Add(pathRequest);

            return true;
        }
    }
}