namespace Utilities
{
    public sealed class CountdownTimer : Timer
    {
        public CountdownTimer() { }
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