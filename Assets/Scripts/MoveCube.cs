using Agents;
using NavigationGraph;
using UnityEngine;

public class MoveCube : MonoBehaviour
{
    private AgentNavigation _agentNavigation;

    private void Awake()
    {
        _agentNavigation = GetComponent<AgentNavigation>();
    }

    private void Update()
    {
        if (_agentNavigation.HasPath) return;

        var gridSystem = ServiceLocator.Instance.GetService<INavigationGraph>();
        var target = GetRandomTarget(gridSystem);
        
        _agentNavigation.RequestPath(target);
    }

    private Cell GetRandomTarget(INavigationGraph graph)
    {
        Cell target = graph.GetRandomCell();

        return target.isWalkable ? target : GetRandomTarget(graph);
    }
}
