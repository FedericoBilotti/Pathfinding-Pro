using NavigationGraph.Graph.Layered;
using NavigationGraph.Graph.Planar;

namespace NavigationGraph
{
    internal static class GraphFactory
    {
        public static INavigationGraph Create(NavigationGraphType graphType, NavigationGraphConfig navigationGraphConfig)
        {
            return graphType switch
            {
                NavigationGraphType.Grid2D => new PlanarGrid(navigationGraphConfig),
                NavigationGraphType.Grid3D => new LayeredGrid(navigationGraphConfig),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}