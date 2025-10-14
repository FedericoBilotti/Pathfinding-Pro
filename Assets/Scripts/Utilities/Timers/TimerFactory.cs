using Agents;
using Utilities;

public class TimerFactory : ITimerFactory
{
    public T Create<T>(float time = 0f) where T : Timer, new()
    {
        var timer = new T();
        timer.Initialize(time);
        return timer;
    }
}