namespace Singularidi.Time
{
    /// <summary>
    /// Abstraction over the playback clock so `MidiPlaybackEngine` can be driven by either
    /// <c>Stopwatch</c> (legacy / tests) or <c>AudioSettings.dspTime</c> (Unity, Phase 2).
    /// </summary>
    public interface IPlaybackClock
    {
        /// <summary>Total elapsed seconds across all running periods (excluding paused intervals). Monotonically non-decreasing.</summary>
        double NowSeconds { get; }

        /// <summary>Begin or resume ticking. Idempotent if already running.</summary>
        void Start();

        /// <summary>Stop ticking; preserve accumulated time so the next <c>Start</c> resumes from where we left off.</summary>
        void Pause();

        /// <summary>Zero accumulated time and stop ticking.</summary>
        void Reset();
    }
}
