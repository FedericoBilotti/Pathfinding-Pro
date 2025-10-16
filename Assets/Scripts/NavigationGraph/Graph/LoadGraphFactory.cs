namespace NavigationGraph.Graph
{
    public class LoadGraphFactory : ILoadGraphFactory
    {
        public ILoadGraph CreateLoadGraph(GraphLoadType graphLoadType, GraphNavigation graphNavigation)
        {
            return graphLoadType switch
            {
                GraphLoadType.Memory => new LoadGridFromMemory(graphNavigation),
                _ => throw new System.NotImplementedException()
            };
        }

    }

    public enum GraphLoadType
    {
        Memory
    }
}