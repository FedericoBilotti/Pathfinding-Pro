using NavigationGraph;
using UnityEngine;

public interface IMapAgent
{
    Vector3 MapAgentToGrid(INavigationGraph graph, Vector3 from);
}