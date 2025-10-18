using NavigationGraph.Graph;
using NavigationGraph.Graph.Layered;
using NavigationGraph.Graph.Planar;

namespace NavigationGraph
{
    internal class GraphFactory
    {
        public INavigationGraph Create(NavigationGraphType graphType, NavigationGraphConfig navigationGraphConfig)
        {
            return graphType switch
            {
                NavigationGraphType.Grid2D => new PlanarGrid(navigationGraphConfig, new LoadGraphFactory()),
                NavigationGraphType.Grid3D => new LayeredGrid(navigationGraphConfig),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}   