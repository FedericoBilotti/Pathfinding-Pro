using Agents;
using UnityEngine;
using UnityEngine.AI;

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
}