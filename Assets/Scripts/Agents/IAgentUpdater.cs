namespace Agents
{
    public interface IAgentUpdater
    {
        void RegisterAgent(AgentNavigation agent);
        void UnregisterAgent(AgentNavigation agent);
    }
}
