using Agents;
using Utilities;

public class AgentUpdateManager : Singleton<AgentUpdateManager>
{
    private SwapBackList<IUpdate> _agents;

    protected override void InitializeSingleton() => _agents = new SwapBackList<IUpdate>(10);

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