#nullable enable
using System;
using System.Collections.Generic;
using Singularidi.Audio;

namespace Singularidi.Midi
{
    /// <summary>
    /// Timing-authoritative MIDI playback engine. Owns the clock, the parsed note list, and the
    /// transport state; emits <see cref="NoteTriggered"/> on the main thread for each note whose
    /// <c>StartSeconds</c> has elapsed. Hosts route the resulting events to an <see cref="IAudioEngine"/>.
    /// </summary>
    public interface IMidiEngine : IDisposable
    {
        PlaybackState State { get; }
        IReadOnlyList<NoteEvent> Notes { get; }
        double CurrentTime { get; }
        double TotalDurationSeconds { get; }

        event Action<NoteEvent>? NoteTriggered;

        void SetAudioEngine(IAudioEngine engine);

        void Load(string midiFilePath);
        void Play();
        void Pause();
        void Stop();

        /// <summary>Trigger a piano-key press immediately, regardless of playback state.</summary>
        void PlayKey(int noteNumber, int velocity = 100);

        /// <summary>Release a previously-triggered piano key.</summary>
        void ReleaseKey(int noteNumber);

        /// <summary>
        /// Called by the host's render loop each frame to emit <see cref="NoteTriggered"/> events
        /// for any note whose <c>StartSeconds</c> has elapsed and to detect end-of-file.
        /// </summary>
        void UpdateNoteEvents();
    }
}
