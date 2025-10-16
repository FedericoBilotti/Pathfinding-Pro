using NavigationGraph.Graph;
using NavigationGraph.Graph.Layered;
using NavigationGraph.Graph.Planar;

namespace NavigationGraph
{
    internal static class GraphFactory
    {
        public static INavigationGraph Create(NavigationGraphType graphType, NavigationGraphConfig navigationGraphConfig)
        {
            ILoadGraphFactory loadGraphFactory = new LoadGraphFactory();

            return graphType switch
            {
                NavigationGraphType.Grid2D => new PlanarGrid(navigationGraphConfig, loadGraphFactory),
                NavigationGraphType.Grid3D => new LayeredGrid(navigationGraphConfig),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}