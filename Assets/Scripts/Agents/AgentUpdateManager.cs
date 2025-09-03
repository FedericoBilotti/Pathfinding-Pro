using Agents;
using UnityEngine;

public class AgentUpdateManager : MonoBehaviour
{
    private static AgentUpdateManager _instance;

    public static AgentUpdateManager Exists => _instance != null ? _instance : null;

    public static AgentUpdateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.Log("Creating AgentUpdateManager instance");
                var obj = new GameObject("AgentUpdateManager");
                _instance = obj.AddComponent<AgentUpdateManager>();
                DontDestroyOnLoad(obj);
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private readonly SwapBackList<IUpdate> _agents = new();

    public void RegisterAgent(IUpdate agent)
    {
        if (agent == null) return;
        _agents.Add(agent);
    }

    public void UnregisterAgent(IUpdate agent)
    {
        if (agent == null) return;
        _agents.Remove(agent);
    }

    private void Update()
    {
        foreach (var agent in _agents)
        {
            agent.CustomUpdate();
        }
    }
}