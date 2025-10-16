using NavigationGraph;
using NavigationGraph.Graph;
using Unity.Collections;

public class LoadGridFromMemory : ILoadGraph
{
    private readonly GraphNavigation _graphNavigation;

    public LoadGridFromMemory(GraphNavigation graphNavigation)
    {
        _graphNavigation = graphNavigation;
    }

    public void LoadGraph(IGraphDataAsset graphDataAsset)
    {
        if (graphDataAsset is not GridDataAsset gridBaked) return;

        int totalGridSize = _graphNavigation.GetGridSizeLength();
        int lengthNeighbors = gridBaked.neighborsCell.neighbors.Length;
        int lengthCounts = gridBaked.neighborsCell.neighborTotalCount.Length;
        int lengthOffsets = gridBaked.neighborsCell.neighborOffsets.Length;

        _graphNavigation.Graph = new NativeArray<Node>(totalGridSize, Allocator.Persistent);
        _graphNavigation.Neighbors = new NativeArray<int>(lengthNeighbors, Allocator.Persistent);
        _graphNavigation.NeighborTotalCount = new NativeArray<int>(lengthCounts, Allocator.Persistent);
        _graphNavigation.NeighborOffsets = new NativeArray<int>(lengthOffsets, Allocator.Persistent);

        for (int x = 0; x < _graphNavigation.GridSize.x; x++)
        {
            for (int y = 0; y < _graphNavigation.GridSize.z; y++)
            {
                int index = x + y * _graphNavigation.GridSize.x;
                NodeData actualNode = gridBaked.cells[index];

                _graphNavigation.Graph[index] = new Node
                {
                    position = actualNode.position,
                    normal = actualNode.normal,
                    height = actualNode.height,
                    gridX = actualNode.gridX,
                    gridZ = actualNode.gridZ,
                    gridIndex = actualNode.gridIndex,
                    cellCostPenalty = actualNode.cellCostPenalty,
                    walkableType = actualNode.walkableType
                };
            }
        }

        for (int i = 0; i < lengthNeighbors; i++)
            _graphNavigation.Neighbors[i] = gridBaked.neighborsCell.neighbors[i];

        for (int i = 0; i < lengthCounts; i++)
            _graphNavigation.NeighborTotalCount[i] = gridBaked.neighborsCell.neighborTotalCount[i];

        for (int i = 0; i < lengthOffsets; i++)
            _graphNavigation.NeighborOffsets[i] = gridBaked.neighborsCell.neighborOffsets[i];
    }
}