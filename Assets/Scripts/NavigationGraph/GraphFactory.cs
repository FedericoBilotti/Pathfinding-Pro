using NavigationGraph.Graph;
using NavigationGraph.RaycastCheck;

namespace NavigationGraph
{
    internal static class GraphFactory
    {
        public static INavigationGraph Create(NavigationGraphType graphType, IRaycastType checkType, NavigationGraphConfig navigationGraphConfig)
        {
            return graphType switch
            {
                NavigationGraphType.Grid2D => new SimpleGridNavigationGraph(checkType, navigationGraphConfig),
                NavigationGraphType.Grid3D => new WorldNavigationGraph(checkType, navigationGraphConfig),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}