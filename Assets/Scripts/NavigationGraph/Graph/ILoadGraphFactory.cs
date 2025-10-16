using NavigationGraph;
using NavigationGraph.Graph;

public interface ILoadGraphFactory
{
    ILoadGraph CreateLoadGraph(GraphLoadType graphLoadType, GraphNavigation graphNavigation);
}