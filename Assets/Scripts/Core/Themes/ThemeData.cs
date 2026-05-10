#nullable enable
using System.Collections.Generic;
using System.Linq;
using Singularidi.Visualization;

namespace Singularidi.Themes
{
    /// <summary>
    /// Serializable implementation of <see cref="IVisualTheme"/>.
    /// Color values are stored as hex strings for human-readable JSON.
    /// </summary>
    public class ThemeData : IVisualTheme
    {
        public string Name { get; set; } = "Custom";

        /// <summary>Schema version. Bumped when fields are added; reader treats unknown fields as defaults.</summary>
        public int SchemaVersion { get; set; } = 2;

        public string Background { get; set; } = "#0D0D0D";
        public string GuideLine { get; set; } = "#19FFFFFF";
        public NoteShape NoteShape { get; set; } = NoteShape.Rectangular;
        public double NoteCornerRadius { get; set; } = 2.0;
        public NoteColorMode ColorMode { get; set; } = NoteColorMode.Channel;

        public string[] ChannelColorValues { get; set; } =
        {
            "#FF5050", "#FF9040", "#FFD740", "#70E050",
            "#40D4D4", "#4080FF", "#A050FF", "#FF50C8",
            "#FF8080", "#80FF80", "#80FFFF", "#8080FF",
            "#FF80C0", "#FFC080", "#C0FF80", "#C080FF",
        };

        public List<string>? TrackColorValues { get; set; }

        public Dictionary<int, string>? NoteColorOverrideValues { get; set; }

        public string WhiteKey { get; set; } = "#F0F0F0";
        public string BlackKey { get; set; } = "#1A1A1A";
        public Dictionary<int, string>? KeyColorOverrideValues { get; set; }

        public string ActiveHighlight { get; set; } = "#FFFFFF";
        public float ActiveNoteBlend { get; set; } = 0.4f;
        public float ActiveWhiteKeyBlend { get; set; } = 0.5f;
        public float ActiveBlackKeyBlend { get; set; } = 0.3f;

        /// <summary>Per-track priority override. Higher values render on top and receive effect-budget allocation first.</summary>
        public Dictionary<int, int>? TrackPriorityOverrides { get; set; }

        /// <summary>Render quality this theme prefers. Adaptive LOD honors this when no user override is set.</summary>
        public RenderQuality RenderQualityDefault { get; set; } = RenderQuality.Realtime;

        // ── IVisualTheme computed properties ────────────────────────────────

        public RgbaColor BackgroundColor => RgbaColor.FromHex(Background);
        public RgbaColor GuideLineColor => RgbaColor.FromHex(GuideLine);

        public RgbaColor[] ChannelColors =>
            ChannelColorValues.Select(RgbaColor.FromHex).ToArray();

        public RgbaColor[] TrackColors =>
            TrackColorValues?.Select(RgbaColor.FromHex).ToArray()
            ?? ChannelColorValues.Select(RgbaColor.FromHex).ToArray();

        public Dictionary<int, RgbaColor>? NoteColorOverrides =>
            NoteColorOverrideValues?.ToDictionary(kv => kv.Key, kv => RgbaColor.FromHex(kv.Value));

        public RgbaColor WhiteKeyColor => RgbaColor.FromHex(WhiteKey);
        public RgbaColor BlackKeyColor => RgbaColor.FromHex(BlackKey);

        public Dictionary<int, RgbaColor>? KeyColorOverrides =>
            KeyColorOverrideValues?.ToDictionary(kv => kv.Key, kv => RgbaColor.FromHex(kv.Value));

        public RgbaColor ActiveHighlightColor => RgbaColor.FromHex(ActiveHighlight);

        // ── Clone ───────────────────────────────────────────────────────────

        public ThemeData Clone() => new ThemeData
        {
            Name = Name,
            SchemaVersion = SchemaVersion,
            Background = Background,
            GuideLine = GuideLine,
            NoteShape = NoteShape,
            NoteCornerRadius = NoteCornerRadius,
            ColorMode = ColorMode,
            ChannelColorValues = (string[])ChannelColorValues.Clone(),
            TrackColorValues = TrackColorValues != null ? new List<string>(TrackColorValues) : null,
            NoteColorOverrideValues = NoteColorOverrideValues != null
                ? new Dictionary<int, string>(NoteColorOverrideValues)
                : null,
            WhiteKey = WhiteKey,
            BlackKey = BlackKey,
            KeyColorOverrideValues = KeyColorOverrideValues != null
                ? new Dictionary<int, string>(KeyColorOverrideValues)
                : null,
            ActiveHighlight = ActiveHighlight,
            ActiveNoteBlend = ActiveNoteBlend,
            ActiveWhiteKeyBlend = ActiveWhiteKeyBlend,
            ActiveBlackKeyBlend = ActiveBlackKeyBlend,
            TrackPriorityOverrides = TrackPriorityOverrides != null
                ? new Dictionary<int, int>(TrackPriorityOverrides)
                : null,
            RenderQualityDefault = RenderQualityDefault,
        };
    }
}
