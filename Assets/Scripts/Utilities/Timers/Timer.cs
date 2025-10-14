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

        protected Timer() { }

        protected Timer(float value)
        {
            initialTime = value;
            IsRunning = false;
        }

        public void Initialize(float value)
        {
            initialTime = value;
            Time = initialTime;
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
}