using Agents;
using NavigationGraph;
using UnityEngine;

[SelectionBase]
public class Player : MonoBehaviour
{
    [SerializeField] private Transform _followTarget;

    private IAgent _agentNavigation;

    private void Awake()
    {
        _agentNavigation = GetComponent<IAgent>();
    }

    private void Update()
    {
        _agentNavigation.RequestPath(_followTarget);
    }

    private Cell GetRandomTarget(INavigationGraph graph)
    {
        Cell target = graph.GetRandomCell();

        return target.walkableType == WalkableType.Obstacle ? target : GetRandomTarget(graph);
    }
}