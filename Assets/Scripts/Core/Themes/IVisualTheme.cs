#nullable enable
using System.Collections.Generic;
using Singularidi.Visualization;

namespace Singularidi.Themes
{
    public interface IVisualTheme
    {
        string Name { get; }

        RgbaColor BackgroundColor { get; }
        RgbaColor GuideLineColor { get; }

        NoteShape NoteShape { get; }

        /// <summary>Corner radius for rectangular notes (0 = sharp corners). Ignored for DotBlock.</summary>
        double NoteCornerRadius { get; }

        NoteColorMode ColorMode { get; }

        /// <summary>16 colors, one per MIDI channel (0–15).</summary>
        RgbaColor[] ChannelColors { get; }

        /// <summary>Variable-length track colors. Falls back to ChannelColors when not set.</summary>
        RgbaColor[] TrackColors { get; }

        /// <summary>Sparse per-note overrides keyed by MIDI note number (0–127). Null = use ChannelColors.</summary>
        Dictionary<int, RgbaColor>? NoteColorOverrides { get; }

        RgbaColor WhiteKeyColor { get; }
        RgbaColor BlackKeyColor { get; }

        /// <summary>Sparse per-key overrides keyed by MIDI note number (0–127). Null = use WhiteKeyColor/BlackKeyColor.</summary>
        Dictionary<int, RgbaColor>? KeyColorOverrides { get; }

        RgbaColor ActiveHighlightColor { get; }
        float ActiveNoteBlend { get; }
        float ActiveWhiteKeyBlend { get; }
        float ActiveBlackKeyBlend { get; }
    }

    public enum NoteShape
    {
        Rectangular,
        DotBlock
    }

    public enum NoteColorMode
    {
        Channel,
        Track
    }
}
