using Utilities;

namespace Agents
{
    public interface ITimerFactory
    {
        T Create<T>(float time = 0f) where T : Timer, new();
    }
}
