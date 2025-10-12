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
                grid = navigationGraph.Graph,
                allNeighbors = navigationGraph.Neighbors,
                neighborCounts = navigationGraph.NeighborTotalCount,
                neighborOffSet = navigationGraph.NeighborOffsets,
                closedList = pathRequest.closedList,
                openList = pathRequest.openList,
                visitedNodes = pathRequest.visitedNodes,
                gridSizeX = navigationGraph.GridSize.x,
                startIndex = start.gridIndex,
                endIndex = end.gridIndex
            };

            JobHandle aStarJob = aStarJobData.ScheduleByRef();

            JobHandle addPath = new AddPath
            {
                grid = navigationGraph.Graph,
                finalPath = pathRequest.path,
                visitedNodes = pathRequest.visitedNodes,
                endIndex = end.gridIndex
            }.Schedule(aStarJob);

            ThetaStarJob thetaStarJob = new ThetaStarJob
            {
                grid = navigationGraph.Graph,
                gridSizeX = navigationGraph.GridSize.x,
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