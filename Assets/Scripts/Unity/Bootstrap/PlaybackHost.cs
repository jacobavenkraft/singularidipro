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
    //   1. engine.UpdateNoteEvents() fires NoteTriggered for any note whose StartSeconds has elapsed.
    //   2. OnNoteTriggered routes NoteOn into the audio engine and queues a (channel, note, endTime)
    //      record for scheduled release.
    //   3. After UpdateNoteEvents, we walk the pending-release list and fire NoteOff for anything
    //      whose endTime has now elapsed.
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

        [Header("Diagnostics")]
        [Tooltip("If true, every NoteTriggered is logged with note number + scheduled DSP time.")]
        [SerializeField] private bool _verboseNoteLogging = false;

        private MeltySynthAudioEngine? _audioEngine;
        private DspTimeClock? _clock;
        private MidiPlaybackEngine? _engine;

        // (channel, note, endSeconds) for active notes awaiting release.
        // List is small (bounded by simultaneous polyphony) and walked once per frame.
        private readonly List<PendingRelease> _pendingReleases = new List<PendingRelease>(256);

        public MidiPlaybackEngine? Engine => _engine;
        public IPlaybackClockSnapshot ClockSnapshot => new IPlaybackClockSnapshot(_clock?.NowSeconds ?? 0.0);

        private void Awake()
        {
            _audioEngine = GetComponent<MeltySynthAudioEngine>();
            _clock = new DspTimeClock();
            _engine = new MidiPlaybackEngine(_clock, new DryWetMidiFileParser());
            _engine.SetAudioEngine(_audioEngine);
            _engine.NoteTriggered += OnNoteTriggered;
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
            if (_engine == null) return;

            _engine.UpdateNoteEvents();
            ProcessPendingReleases(_clock!.NowSeconds);
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

        // Compact snapshot exposed for inspector/debug overlays; avoids exposing the mutable clock.
        public readonly struct IPlaybackClockSnapshot
        {
            public readonly double NowSeconds;
            public IPlaybackClockSnapshot(double nowSeconds) { NowSeconds = nowSeconds; }
        }
    }
}
