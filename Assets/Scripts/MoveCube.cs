using Agents;
using NavigationGraph;
using UnityEngine;
using Utilities;

public class MoveCube : MonoBehaviour
{
    private AgentNavigation _agentNavigation;
    private INavigationGraph _gridSystem;

    private void Awake() => _agentNavigation = GetComponent<AgentNavigation>();
    private void Start()
    {
        _gridSystem = ServiceLocator.Instance.GetService<INavigationGraph>();
        if (_agentNavigation.HasPath) return;

        Node _target = GetRandomTarget(_gridSystem);
        _agentNavigation.RequestPath(_target);
    }

    private void Update()
    {
    }

    private Node GetRandomTarget(INavigationGraph graph)
    {
        Node target = graph.GetRandomCell();

        return target.walkableType == WalkableType.Walkable ? target : GetRandomTarget(graph);
    }
}