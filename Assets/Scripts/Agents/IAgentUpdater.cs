namespace Agents
{
    public interface IAgentUpdater
    {
        void RegisterAgent(IAgent agent);
        void UnregisterAgent(IAgent agent);
    }
}
