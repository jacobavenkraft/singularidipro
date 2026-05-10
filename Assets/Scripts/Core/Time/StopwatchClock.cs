using System.Diagnostics;

namespace Singularidi.Time
{
    public sealed class StopwatchClock : IPlaybackClock
    {
        private readonly Stopwatch _sw = new Stopwatch();
        private double _accumulatedSeconds;

        public double NowSeconds => _accumulatedSeconds + _sw.Elapsed.TotalSeconds;

        public void Start() => _sw.Start();

        public void Pause()
        {
            _accumulatedSeconds += _sw.Elapsed.TotalSeconds;
            _sw.Reset();
        }

        public void Reset()
        {
            _sw.Reset();
            _accumulatedSeconds = 0;
        }
    }
}
