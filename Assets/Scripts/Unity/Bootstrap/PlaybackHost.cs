#nullable enable
using System.Collections.Generic;
using System.IO;
using Singularidi.Midi;
using Singularidi.Unity.Audio;
using Singularidi.Unity.Time;
using UnityEngine;

namespace Singularidi.Unity.Bootstrap
{
    // Phase 2 verification bootstrap.
    //
    // Wires up the Phase 1 Core types (MidiPlaybackEngine + DryWetMidiFileParser) to the Phase 2
    // Unity audio engine (MeltySynthAudioEngine) with a DspTimeClock as the timing source.
    //
    // The flow each frame:
    //   1. Detect engine state transitions; on Stop, drop any stale pending releases so the
    //      next session doesn't fire NoteOff for absolute EndSeconds carried over from before.
    //   2. engine.UpdateNoteEvents() fires NoteTriggered for any note whose StartSeconds has elapsed.
    //   3. OnNoteTriggered routes NoteOn into the audio engine and queues a (channel, note, endTime)
    //      record for scheduled release.
    //   4. ProcessPendingReleases walks the list and fires NoteOff for anything whose endTime
    //      has now elapsed.
    //
    // Phase 5 will replace the pending-release walk with the MidiPreprocessor's per-pitch index.
    // For Phase 2 it's the simplest thing that exercises end-to-end timing + audio.
    [RequireComponent(typeof(MeltySynthAudioEngine))]
    public sealed class PlaybackHost : MonoBehaviour
    {
        [Header("MIDI source")]
        [Tooltip("Path relative to Application.streamingAssetsPath. Defaults to the Phase 1 sample.")]
        [SerializeField] private string _midiRelativePath = "Midi/Sample.mid";

        [Header("Playback")]
        [SerializeField] private bool _autoPlayOnStart = true;

        [Tooltip("Initial capacity for the pending-release list, sized to expected peak polyphony. " +
                 "Default 1024 covers Rush E's 897-voice climax. Bump higher for black-MIDI files " +
                 "with sustained polyphony above 1024 to avoid List<T> reallocations on the " +
                 "NoteOn hot path.")]
        [SerializeField] private int _polyphonyBuffer = 1024;

        [Header("Diagnostics")]
        [Tooltip("If true, every NoteTriggered is logged with note number + scheduled DSP time.")]
        [SerializeField] private bool _verboseNoteLogging = false;

        private MeltySynthAudioEngine? _audioEngine;
        private DspTimeClock? _clock;
        private MidiPlaybackEngine? _engine;

        // (channel, note, endSeconds) for active notes awaiting release.
        // Constructed in Awake with capacity = _polyphonyBuffer (Unity populates SerializeFields
        // between ctor and Awake, so the field initializer can't see the configured value).
        // Cleared on Stop transitions so stale absolute EndSeconds don't leak into the next session.
        private List<PendingRelease> _pendingReleases = null!;

        private PlaybackState _lastObservedState = PlaybackState.Idle;

        public MidiPlaybackEngine? Engine => _engine;
        public ClockSnapshot ClockSnapshot => new ClockSnapshot(_clock?.NowSeconds ?? 0.0);

        private void Awake()
        {
            _audioEngine = GetComponent<MeltySynthAudioEngine>();
            _clock = new DspTimeClock();
            _engine = new MidiPlaybackEngine(_clock, new DryWetMidiFileParser());
            _engine.SetAudioEngine(_audioEngine);
            _engine.NoteTriggered += OnNoteTriggered;

            _pendingReleases = new List<PendingRelease>(Mathf.Max(1, _polyphonyBuffer));
        }

        private void Start()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, _midiRelativePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[PlaybackHost] MIDI file not found at '{fullPath}'. Set _midiRelativePath to a file under StreamingAssets/.");
                return;
            }

            _engine!.Load(fullPath);
            Debug.Log($"[PlaybackHost] Loaded '{fullPath}': {_engine.Notes.Count} notes, {_engine.TotalDurationSeconds:F2}s.");

            if (_autoPlayOnStart)
            {
                _engine.Play();
                Debug.Log("[PlaybackHost] Auto-play started.");
            }
        }

        private void Update()
        {
            if (_engine == null || _clock == null) return;

            DetectStopAndClearReleases();
            _engine.UpdateNoteEvents();
            ProcessPendingReleases(_clock.NowSeconds);
        }

        // Detects Playing/Paused -> Loaded/Idle transitions (i.e. an external Stop call) and
        // drops queued releases. Without this, absolute EndSeconds from the prior session can
        // satisfy `EndSeconds <= now` on the next Play, firing NoteOff against pitches that
        // aren't held. Pause does NOT clear (held notes must release correctly on resume).
        private void DetectStopAndClearReleases()
        {
            var state = _engine!.State;
            if (state == _lastObservedState) return;

            bool wasActive = _lastObservedState == PlaybackState.Playing || _lastObservedState == PlaybackState.Paused;
            bool nowStopped = state == PlaybackState.Loaded || state == PlaybackState.Idle;
            if (wasActive && nowStopped && _pendingReleases.Count > 0)
                _pendingReleases.Clear();

            _lastObservedState = state;
        }

        private void OnDestroy()
        {
            if (_engine != null)
            {
                _engine.NoteTriggered -= OnNoteTriggered;
                _engine.Dispose();
                _engine = null;
            }
        }

        private void OnNoteTriggered(NoteEvent note)
        {
            _audioEngine?.NoteOn(note.Channel, note.NoteNumber, note.Velocity);
            _pendingReleases.Add(new PendingRelease(note.Channel, note.NoteNumber, note.EndSeconds));

            if (_verboseNoteLogging)
                Debug.Log($"[PlaybackHost] NoteOn ch={note.Channel} pitch={note.NoteNumber} vel={note.Velocity} dspNow={_clock!.NowSeconds:F3}s end={note.EndSeconds:F3}s");
        }

        private void ProcessPendingReleases(double now)
        {
            // Swap-remove walk: O(active polyphony) per frame, no allocation.
            for (int i = _pendingReleases.Count - 1; i >= 0; i--)
            {
                var release = _pendingReleases[i];
                if (release.EndSeconds > now) continue;

                _audioEngine?.NoteOff(release.Channel, release.NoteNumber);
                int last = _pendingReleases.Count - 1;
                _pendingReleases[i] = _pendingReleases[last];
                _pendingReleases.RemoveAt(last);
            }
        }

        private readonly struct PendingRelease
        {
            public readonly int Channel;
            public readonly int NoteNumber;
            public readonly double EndSeconds;

            public PendingRelease(int channel, int noteNumber, double endSeconds)
            {
                Channel = channel;
                NoteNumber = noteNumber;
                EndSeconds = endSeconds;
            }
        }
    }

    // Compact snapshot exposed for inspector/debug overlays; avoids exposing the mutable clock.
    // Top-level (not nested in PlaybackHost) so the property `PlaybackHost.ClockSnapshot` can
    // share the type's identifier without C# member-name collision.
    public readonly struct ClockSnapshot
    {
        public readonly double NowSeconds;
        public ClockSnapshot(double nowSeconds) { NowSeconds = nowSeconds; }
    }
}
