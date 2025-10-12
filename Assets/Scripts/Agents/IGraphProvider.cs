using NavigationGraph;

namespace Agents
{
    public interface IGraphProvider
    {
        INavigationGraph GetGraph();
    }
}
