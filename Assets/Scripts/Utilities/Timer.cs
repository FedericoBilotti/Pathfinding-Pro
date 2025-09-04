namespace Utilities
{
    public abstract class Timer
    {
        protected float initialTime;

        protected float Time { get; set; }

        public float Progress => Time / initialTime;

        public bool IsRunning { get; private set; }

        public System.Action onTimerStart = delegate { };
        public System.Action onTimerStop = delegate { };

        protected Timer(float value)
        {
            initialTime = value;
            IsRunning = false;
        }

        public void Start()
        {
            Time = initialTime;
            if (IsRunning) return;

            IsRunning = true;
            onTimerStart.Invoke();
        }

        public void Stop()
        {
            if (!IsRunning) return;

            IsRunning = false;
            onTimerStop.Invoke();
        }

        public void Resume() => IsRunning = true;
        public void Pause() => IsRunning = false;
        public float GetTime() => Time;

        public virtual void Reset(float newTime = 0)
        {
            if (newTime >= 0) initialTime = newTime;
            
            Time = initialTime;
        }

        public abstract void Tick(float deltaTime);
    }

    public sealed class StopWatchTimer : Timer
    {
        public StopWatchTimer(float value) : base(0) { }

        public override void Tick(float deltaTime)
        {
            if (!IsRunning) return;
            Time += deltaTime;
        }

        public override void Reset(float newTime = 0) => Time = 0;
    }

    public sealed class CountdownTimer : Timer
    {
        public CountdownTimer(float value) : base(value) { }

        public override void Tick(float deltaTime)
        {
            if (IsRunning && Time > 0)
            {
                Time -= deltaTime;
            }

            if (IsRunning && Time <= 0)
            {
                Stop();
            }
        }

        public bool IsFinished() => Time <= 0;
    }
}