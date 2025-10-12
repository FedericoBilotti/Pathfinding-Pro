using Agents;
using Utilities;

public class TimerFactory : ITimerFactory
{
    public T Create<T>(float time = 0f, 
        System.Action onStart = null, 
        System.Action onStop = null) 
        where T : Timer, new()
    {
        var timer = new T();
        timer.Initialize(time);

        if (onStart != null)
            timer.onTimerStart += onStart;

        if (onStop != null)
            timer.onTimerStop += onStop;

        return timer;
    }
}