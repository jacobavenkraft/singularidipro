#nullable enable
using System.Collections.Generic;
using System.IO;
using Singularidi.Audio;
using Singularidi.Midi;
using Singularidi.Time;
using Singularidi.Unity.Audio;
using Singularidi.Unity.Time;
using UnityEngine;

namespace Singularidi.Unity.Bootstrap
{
    // Phase 2 verification bootstrap.
    //
    // Wires up the Phase 1 Core types (MidiPlaybackEngine + DryWetMidiFileParser) to the Phase 2
    // Unity audio engine (MeltySynthAudioEngine) with a DspTimeClock as the timing source.
    // Fields hold the platform-agnostic interfaces (IAudioEngine + IPlaybackClock) so that
    // future backends (e.g. a hardware MIDI-out engine, or a non-DSP clock for headless tests)
    // can be substituted without touching this host.
    //
    // The flow each frame:
    //   1. Detect midiEngine state transitions; on Stop, fire NoteOff for every queued release
    //      and clear the list so stale absolute EndSeconds don't leak into the next session.
    //   2. _midiEngine.UpdateNoteEvents() fires NoteTriggered for any note whose StartSeconds has elapsed.
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
        [Tooltip("Path relative to Application.streamingAssetsPath. The default resolves to " +
                 "Assets/StreamingAssets/Midi/Sample.mid where the Phase 1 sample lives.")]
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

        private IAudioEngine? _audioEngine;
        private IPlaybackClock? _clock;
        private MidiPlaybackEngine? _midiEngine;

        // (channel, note, endSeconds) for active notes awaiting release.
        // Constructed in Awake with capacity = _polyphonyBuffer (Unity populates SerializeFields
        // between ctor and Awake, so the field initializer can't see the configured value).
        // Cleared on Stop transitions so stale absolute EndSeconds don't leak into the next session.
        private List<PendingRelease> _pendingReleases = null!;

        private PlaybackState _lastObservedState = PlaybackState.Idle;

        public MidiPlaybackEngine? Engine => _midiEngine;
        public ClockSnapshot ClockSnapshot => new ClockSnapshot(_clock?.NowSeconds ?? 0.0);

        private void Awake()
        {
            _audioEngine = GetComponent<MeltySynthAudioEngine>();
            _clock = new DspTimeClock();
            _midiEngine = new MidiPlaybackEngine(_clock, new DryWetMidiFileParser());
            _midiEngine.SetAudioEngine(_audioEngine);
            _midiEngine.NoteTriggered += OnNoteTriggered;

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

            _midiEngine!.Load(fullPath);
            Debug.Log($"[PlaybackHost] Loaded '{fullPath}': {_midiEngine.Notes.Count} notes, {_midiEngine.TotalDurationSeconds:F2}s.");

            if (_autoPlayOnStart)
            {
                _midiEngine.Play();
                Debug.Log("[PlaybackHost] Auto-play started.");
            }
        }

        private void Update()
        {
            if (_midiEngine == null || _clock == null) return;

            DetectStopAndReleasePending();
            _midiEngine.UpdateNoteEvents();
            ProcessPendingReleases(_clock.NowSeconds);
        }

        // Detects Playing/Paused -> Loaded/Idle transitions (i.e. an external Stop call) and
        // releases queued notes. We fire NoteOff for every pending release so the audio engine
        // sees an explicit gate-off for each held voice — `MidiPlaybackEngine.Stop()` cascades
        // into `IAudioEngine.Stop()`, which in MeltySynth resets the synth and is sufficient
        // today, but issuing the matched NoteOffs keeps the host correct against any audio
        // backend whose Stop() doesn't unconditionally silence held voices.
        // Without this clearing step, absolute EndSeconds from the prior session can also
        // satisfy `EndSeconds <= now` on the next Play, firing NoteOff against pitches that
        // aren't held. Pause does NOT clear (held notes must release correctly on resume).
        private void DetectStopAndReleasePending()
        {
            var state = _midiEngine!.State;
            if (state == _lastObservedState) return;

            bool wasActive = _lastObservedState == PlaybackState.Playing || _lastObservedState == PlaybackState.Paused;
            bool nowStopped = state == PlaybackState.Loaded || state == PlaybackState.Idle;
            if (wasActive && nowStopped && _pendingReleases.Count > 0)
            {
                for (int i = 0; i < _pendingReleases.Count; i++)
                {
                    var release = _pendingReleases[i];
                    _audioEngine?.NoteOff(release.Channel, release.NoteNumber);
                }
                _pendingReleases.Clear();
            }

            _lastObservedState = state;
        }

        private void OnDestroy()
        {
            if (_midiEngine != null)
            {
                _midiEngine.NoteTriggered -= OnNoteTriggered;
                _midiEngine.Dispose();
                _midiEngine = null;
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
