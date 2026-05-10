#nullable enable
using System;
using System.Collections.Generic;
using Singularidi.Audio;
using Singularidi.Time;

namespace Singularidi.Midi
{
    public enum PlaybackState { Idle, Loaded, Playing, Paused, Finished }

    public sealed class MidiPlaybackEngine : IDisposable
    {
        private readonly IPlaybackClock _clock;
        private readonly IMidiFileParser _parser;
        private IAudioEngine? _audioEngine;
        private List<NoteEvent> _notes = new List<NoteEvent>();
        private double _totalDuration;
        private int _nextNoteIndex;
        private string? _currentFilePath;

        public MidiPlaybackEngine() : this(new Singularidi.Time.StopwatchClock(), new DryWetMidiFileParser()) { }

        public MidiPlaybackEngine(IPlaybackClock clock, IMidiFileParser parser)
        {
            _clock = clock;
            _parser = parser;
        }

        public PlaybackState State { get; private set; } = PlaybackState.Idle;
        public IReadOnlyList<NoteEvent> Notes => _notes;
        public double CurrentTime => _clock.NowSeconds;
        public double TotalDurationSeconds => _totalDuration;

        public event Action<NoteEvent>? NoteTriggered;

        public void SetAudioEngine(IAudioEngine engine)
        {
            bool wasPlaying = State == PlaybackState.Playing;
            if (wasPlaying) _audioEngine?.Stop();
            _audioEngine?.Dispose();
            _audioEngine = engine;

            if (_currentFilePath != null && State is PlaybackState.Loaded or PlaybackState.Paused
                    or PlaybackState.Playing or PlaybackState.Finished)
            {
                _audioEngine.LoadFile(_currentFilePath);
            }
        }

        public void Load(string midiFilePath)
        {
            var prevState = State;
            if (prevState == PlaybackState.Playing || prevState == PlaybackState.Paused)
                Stop();

            var (notes, duration) = _parser.Parse(midiFilePath);
            _notes = notes;
            _totalDuration = duration;
            _nextNoteIndex = 0;
            _clock.Reset();
            _currentFilePath = midiFilePath;
            _audioEngine?.LoadFile(midiFilePath);
            State = PlaybackState.Loaded;
        }

        public void Play()
        {
            if (State is not (PlaybackState.Loaded or PlaybackState.Paused or PlaybackState.Finished))
                return;

            if (State == PlaybackState.Finished)
            {
                _clock.Reset();
                _nextNoteIndex = 0;
            }

            _clock.Start();
            _audioEngine?.Play();
            State = PlaybackState.Playing;
        }

        public void Pause()
        {
            if (State != PlaybackState.Playing) return;
            _clock.Pause();
            _audioEngine?.Pause();
            State = PlaybackState.Paused;
        }

        public void Stop()
        {
            if (State == PlaybackState.Idle) return;
            _clock.Reset();
            _nextNoteIndex = 0;
            _audioEngine?.Stop();
            State = _currentFilePath != null ? PlaybackState.Loaded : PlaybackState.Idle;
        }

        /// <summary>Trigger a piano-key press immediately, regardless of playback state.</summary>
        public void PlayKey(int noteNumber, int velocity = 100)
            => _audioEngine?.NoteOn(channel: 0, noteNumber, velocity);

        /// <summary>Release a previously-triggered piano key.</summary>
        public void ReleaseKey(int noteNumber)
            => _audioEngine?.NoteOff(channel: 0, noteNumber);

        /// <summary>
        /// Called by the visualizer render loop each frame to emit NoteTriggered events
        /// and detect end-of-file.
        /// </summary>
        public void UpdateNoteEvents()
        {
            if (State != PlaybackState.Playing) return;
            double now = CurrentTime;

            while (_nextNoteIndex < _notes.Count && _notes[_nextNoteIndex].StartSeconds <= now)
            {
                NoteTriggered?.Invoke(_notes[_nextNoteIndex]);
                _nextNoteIndex++;
            }

            if (_totalDuration > 0 && now >= _totalDuration)
            {
                _clock.Pause();
                State = PlaybackState.Finished;
            }
        }

        public void Dispose()
        {
            _audioEngine?.Dispose();
            _audioEngine = null;
        }
    }
}
