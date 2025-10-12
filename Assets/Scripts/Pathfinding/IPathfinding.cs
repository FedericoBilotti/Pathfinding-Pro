using Agents;
using NavigationGraph;

namespace Pathfinding
{
    public interface IPathfinding
    {
        bool RequestPath(IAgent agent, Node start, Node end);
        void SetPathToAgent();
        void Clear();
    }
}