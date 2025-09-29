using NavigationGraph.Graph;
using NavigationGraph.RaycastCheck;

namespace NavigationGraph
{
    internal static class GraphFactory
    {
        public static INavigationGraph Create(NavigationGraphType graphType, NavigationGraphConfig navigationGraphConfig)
        {
            return graphType switch
            {
                NavigationGraphType.Grid2D => new SimpleGridNavigationGraph(navigationGraphConfig),
                NavigationGraphType.Grid3D => new WorldNavigationGraph(navigationGraphConfig),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}