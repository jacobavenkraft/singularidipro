#nullable enable
using System.Collections.Generic;
using Singularidi.Midi;
using Singularidi.Themes;

namespace Singularidi.Visualization
{
    public static class ColorHelper
    {
        public static RgbaColor LerpToColor(RgbaColor c, RgbaColor target, float t)
        {
            float r = c.R + (target.R - c.R) * t;
            float g = c.G + (target.G - c.G) * t;
            float b = c.B + (target.B - c.B) * t;
            return new RgbaColor(r, g, b);
        }

        public static RgbaColor ResolveNoteColor(
            NoteEvent note,
            NoteColorMode colorMode,
            RgbaColor[] channelColors,
            RgbaColor[] trackColors,
            Dictionary<int, RgbaColor>? noteColorOverrides)
        {
            if (noteColorOverrides != null && noteColorOverrides.TryGetValue(note.NoteNumber, out var overrideColor))
                return overrideColor;

            if (colorMode == NoteColorMode.Track && trackColors.Length > 0)
                return trackColors[note.Track % trackColors.Length];

            return channelColors[note.Channel % 16];
        }

        public static RgbaColor ResolveActiveKeyColor(
            int noteNumber,
            int[] activeKeyChannel,
            int[] activeKeyTrack,
            NoteColorMode colorMode,
            RgbaColor[] channelColors,
            RgbaColor[] trackColors,
            RgbaColor highlightColor,
            float blendFactor)
        {
            int channel = activeKeyChannel[noteNumber];
            if (channel < 0) return default;

            RgbaColor keyBase;
            if (colorMode == NoteColorMode.Track && trackColors.Length > 0)
            {
                int track = activeKeyTrack[noteNumber];
                keyBase = trackColors[track >= 0 ? track % trackColors.Length : 0];
            }
            else
            {
                keyBase = channelColors[channel % 16];
            }

            return LerpToColor(keyBase, highlightColor, blendFactor);
        }

        public static RgbaColor Lighten(RgbaColor c, double amount)
        {
            float a = (float)amount;
            float r = c.R + (1f - c.R) * a;
            float g = c.G + (1f - c.G) * a;
            float b = c.B + (1f - c.B) * a;
            return new RgbaColor(r, g, b, c.A);
        }

        public static RgbaColor Darken(RgbaColor c, double amount)
        {
            float a = (float)amount;
            float r = c.R * (1f - a);
            float g = c.G * (1f - a);
            float b = c.B * (1f - a);
            return new RgbaColor(r, g, b, c.A);
        }
    }
}
