using Agents;
using NavigationGraph;

namespace Pathfinding
{
    public interface IPathRequest
    {
        void SetPathStrategy(PathRequestType pathRequestStrategy);
        bool RequestPath(IAgent agent, Node start, Node end);
    }
}