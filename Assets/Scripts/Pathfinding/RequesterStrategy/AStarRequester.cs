using Agents;
using NavigationGraph;
using NUnit.Framework;
using Pathfinding.PathImplementation;
using Unity.Jobs;

namespace Pathfinding.RequesterStrategy
{
    public class AStarRequester : Pathfinding
    {
        public AStarRequester(INavigationGraph navigationGraph) : base(navigationGraph) { }
        
        public override bool RequestPath(IAgent agent, Cell start, Cell end)
        {
            if (!end.isWalkable) return false;

            PathRequest pathRequest = pathRequestPool.Get();

            int patience = navigationGraph.GetGridSize();

            JobHandle aStarJob = new AStarJob
            {
                grid = navigationGraph.GetGrid(),
                neighborsPerCell = navigationGraph.GetNeighbors(),
                closedList = pathRequest.closedList,
                openList = pathRequest.openList,
                visitedNodes = pathRequest.visitedNodes,
                gridSizeX = navigationGraph.GetGridSizeX(),
                startIndex = start.gridIndex,
                endIndex = end.gridIndex,
                patience = patience,
            }.Schedule();

            if (patience < 0)
            {
                Assert.IsFalse(patience < 0, "Pathfinding timed out");
                return false;
            }

            JobHandle addPath = new AddPath
            {
                grid = navigationGraph.GetGrid(),
                finalPath = pathRequest.path,
                visitedNodes = pathRequest.visitedNodes,
                endIndex = end.gridIndex
            }.Schedule(aStarJob);

            JobHandle reversePath = new ReversePath
            {
                finalPath = pathRequest.path
            }.Schedule(addPath);

            pathRequest.agent = agent;
            pathRequest.handle = reversePath;
            requests.Add(pathRequest);

            return true;
        }
    }
}