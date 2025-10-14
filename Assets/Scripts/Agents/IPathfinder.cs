using NavigationGraph;

namespace Agents
{
    public interface IPathfinder
    {
        bool RequestPath(IAgent agent, Node start, Node end);
    }
}
