using System.Collections.Generic;
using Agents;
using UnityEngine;

public class AgentUpdateManager : MonoBehaviour
{
    private static AgentUpdateManager _instance;
    public static AgentUpdateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var obj = new GameObject("AgentUpdateManager");
                _instance = obj.AddComponent<AgentUpdateManager>();
            }
            return _instance;
        }
    }

    // private readonly SwapBackList<IUpdate> _agents = new();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    // public void RegisterAgent(IUpdate agent)
    // {
    //     if (agent == null) return;
    //     _agents.Add(agent);
    // }

    // public void UnregisterAgent(IUpdate agent)
    // {
    //     if (agent == null) return;
    //     _agents.Remove(agent);
    // }

    // private void Update()
    // {
    //     foreach (var agent in _agents)
    //     {
    //         agent.CustomUpdate();
    //     }
    // }
}