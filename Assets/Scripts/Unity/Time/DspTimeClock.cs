#nullable enable
using System;
using Singularidi.Time;
using UnityEngine;

namespace Singularidi.Unity.Time
{
    // IPlaybackClock driven by AudioSettings.dspTime.
    //
    // dspTime is updated on the audio thread but is safe to read from the main thread.
    // It's monotonically non-decreasing and tied to actual audio samples played, which makes it
    // the right clock for visualization that must align with what the user hears.
    //
    // The dsp-time source is injectable so tests can drive the clock without a running audio system.
    public sealed class DspTimeClock : IPlaybackClock
    {
        private readonly Func<double> _dspTimeSource;

        private double _accumulatedSeconds;
        private double _runStartDspTime;
        private bool _running;

        public DspTimeClock() : this(() => AudioSettings.dspTime) { }

        public DspTimeClock(Func<double> dspTimeSource)
        {
            _dspTimeSource = dspTimeSource ?? throw new ArgumentNullException(nameof(dspTimeSource));
        }

        public double NowSeconds
        {
            get
            {
                if (!_running)
                    return _accumulatedSeconds;

                return _accumulatedSeconds + (_dspTimeSource() - _runStartDspTime);
            }
        }

        public void Start()
        {
            if (_running) return;
            _runStartDspTime = _dspTimeSource();
            _running = true;
        }

        public void Pause()
        {
            if (!_running) return;
            _accumulatedSeconds += _dspTimeSource() - _runStartDspTime;
            _running = false;
        }

        public void Reset()
        {
            _running = false;
            _accumulatedSeconds = 0;
            _runStartDspTime = 0;
        }
    }
}
