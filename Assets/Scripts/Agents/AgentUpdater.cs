using Utilities;

namespace Agents
{
    public class AgentUpdater : IAgentUpdater
    {
        public void RegisterAgent(AgentNavigation agent)
        {
            if (ServiceLocator.Instance.TryGetService<AgentUpdateManager>(out var agentUpdateManager))
                agentUpdateManager.RegisterAgent(agent);
        }

        public void UnregisterAgent(AgentNavigation agent)
        {
            if (ServiceLocator.Instance.TryGetService<AgentUpdateManager>(out var agentUpdateManager))
                agentUpdateManager.UnregisterAgent(agent);
        }
    }
}
