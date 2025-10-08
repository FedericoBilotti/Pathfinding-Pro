using Agents;
using NavigationGraph;

namespace Pathfinding
{
    public interface IPathRequest
    {
        public bool RequestPath(IAgent agent, Node start, Node end);
        public void SetPathToAgent();
        public void Clear();
    }
}