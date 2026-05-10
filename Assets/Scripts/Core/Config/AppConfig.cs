#nullable enable
using System.Collections.Generic;
using Singularidi.Themes;

namespace Singularidi.Config
{
    public class AppConfig
    {
        public string SoundFontPath { get; set; } = "";
        public AudioOutputMode OutputMode { get; set; } = AudioOutputMode.SoundFont;
        public string PreferredMidiDevice { get; set; } = "";
        public bool HighlightActiveNotes { get; set; } = true;
        public string LastMidiFilePath { get; set; } = "";
        public string ThemeName { get; set; } = "Dark";
        public string VisualizationType { get; set; } = "Vertical Fall";
        public string GuideLineStyle { get; set; } = "KeyWidthCentered";
        public string PianoRenderMode { get; set; } = "Software";
        public List<ThemeData>? CustomThemes { get; set; }

        // Export settings
        public string? FfmpegPath { get; set; }
        public int ExportWidth { get; set; } = 1920;
        public int ExportHeight { get; set; } = 1080;
        public int ExportFps { get; set; } = 60;

        // Window placement (Phase 1 additive)
        public int WindowWidth { get; set; } = 1280;
        public int WindowHeight { get; set; } = 720;

        // Offline render mode (Phase 1 additive — wired up in Phase 9 export)
        public OfflineRenderMode OfflineRenderMode { get; set; } = OfflineRenderMode.Full128;
        public OutOfRangeBehavior OutOfRangeBehavior { get; set; } = OutOfRangeBehavior.Drop;
        public double OfflineMasterLimiterDb { get; set; } = -3.0;
        public bool OfflineReverbEnabled { get; set; } = false;
    }

    public enum AudioOutputMode
    {
        SoundFont,
        MidiDevice
    }

    public enum OfflineRenderMode
    {
        /// <summary>All 128 MIDI pitches rendered honestly. No voice cap, no pitch clamp. Default.</summary>
        Full128,

        /// <summary>Pitch-clamped to A0..C8 (MIDI 21..108) plus 88-voice cap. "Real piano cacophony" aesthetic.</summary>
        RealPiano88,

        /// <summary>Single-synth output without stems or consolidation. Matches Phase 2 realtime output for A/B comparison.</summary>
        LegacyMonolithic
    }

    public enum OutOfRangeBehavior
    {
        /// <summary>Out-of-range notes (under <c>RealPiano88</c>) are dropped.</summary>
        Drop,

        /// <summary>Out-of-range notes are transposed octave-by-octave until they fall inside A0..C8.</summary>
        TransposeToNearestOctave
    }
}
