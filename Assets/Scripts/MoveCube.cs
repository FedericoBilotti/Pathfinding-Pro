using Agents;
using NavigationGraph;
using UnityEngine;

[SelectionBase]
public class MoveCube : MonoBehaviour
{
    private AgentNavigation _agentNavigation;
    private INavigationGraph _gridSystem;

    private void Awake() => _agentNavigation = GetComponent<AgentNavigation>();
    private void Start() => _gridSystem = ServiceLocator.Instance.GetService<INavigationGraph>();

    private void Update()
    {
        if (_agentNavigation.HasPath) return;

        var target = GetRandomTarget(_gridSystem);
        _agentNavigation.RequestPath(target);
    }

    private Cell GetRandomTarget(INavigationGraph graph)
    {
        Cell target = graph.GetRandomCell();

        return target.isWalkable ? target : GetRandomTarget(graph);
    }
}
