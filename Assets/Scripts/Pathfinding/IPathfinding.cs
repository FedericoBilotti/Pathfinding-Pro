using Agents;
using NavigationGraph;

namespace Pathfinding
{
    public interface IPathfinding
    {
        void SetPathStrategy(PathRequestType pathRequestStrategy);
        bool RequestPath(IAgent agent, Cell start, Cell end);
    }
}