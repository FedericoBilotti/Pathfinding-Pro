namespace Utilities
{
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
}