using Utilities;

namespace Agents
{
    public interface ITimerFactory
    {
        T Create<T>(float time = 0f, System.Action onStart = null, System.Action onStop = null) where T : Timer, new();
    }
}
