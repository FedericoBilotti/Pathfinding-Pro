using Agents;
using NavigationGraph;
using Pathfinding.PathImplementation;
using Unity.Jobs;
using UnityEngine.Assertions;

namespace Pathfinding.RequesterStrategy
{
    public class ThetaStarRequester : Pathfinding
    {
        public ThetaStarRequester(INavigationGraph navigationGraph) : base(navigationGraph) { }

        public override bool RequestPath(IAgent agent, Cell start, Cell end)
        {
            if (!end.isWalkable) return false;

            PathRequest pathRequest = pathRequestPool.Get();

            int patience = navigationGraph.GetGridSize() / 4;

            JobHandle aStarJob = new AStarJob
            {
                grid = navigationGraph.GetGrid(),
                allNeighbors = navigationGraph.GetNeighbors(),
                neighborCounts = navigationGraph.GetNeighborCounts(),
                neighborsPerCell = navigationGraph.GetNeighborsPerCellCount(),
                closedList = pathRequest.closedList,
                openList = pathRequest.openList,
                visitedNodes = pathRequest.visitedNodes,
                gridSizeX = navigationGraph.GetXSize(),
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

            JobHandle thetaStarJob = new ThetaStarJob
            {
                grid = navigationGraph.GetGrid(),
                gridSizeX = navigationGraph.GetXSize(),
                finalPath = pathRequest.path,
                simplified = pathRequest.simplified
            }.Schedule(addPath);

            JobHandle reversePath = new ReversePath
            {
                finalPath = pathRequest.path
            }.Schedule(thetaStarJob);

            pathRequest.agent = agent;
            pathRequest.handle = reversePath;
            requests.Add(pathRequest);

            return true;
        }
    }
}