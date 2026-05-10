using System;
using System.Collections.Generic;

namespace Singularidi.Visualization
{
    /// <summary>
    /// Piano layout using equal-segment-within-groups positioning:
    ///   - Bottom: 75 white keys of equal width tiling the full control width
    ///   - Top: within CDE group (3 white, 2 black), 5 equal segments
    ///          within FGAB group (4 white, 3 black), 7 equal segments
    ///   - This produces natural piano geometry where boundary lines between white keys
    ///     are offset from black key centers (75% through F#/A#, 67% through C#/D#)
    /// </summary>
    public sealed class PianoLayout
    {
        public static readonly bool[] IsBlackKey =
        {
            false, true, false, true, false, false, true, false, true, false, true, false
        };

        public const double PianoHeightFraction = 0.15;
        public const double LookAheadSeconds = 4.0;
        public const double BlackKeyHeightFraction = 0.65;

        public readonly double[] XCenter = new double[128];
        public readonly double[] NoteWidth = new double[128];

        public readonly double[] WhiteKeyBottomLeft = new double[128];
        public readonly double[] WhiteKeyBottomRight = new double[128];
        public readonly double[] KeyTopLeft = new double[128];
        public readonly double[] KeyTopRight = new double[128];

        public readonly double[] GuideXUniform = new double[128];
        public readonly List<double> OctaveBoundaryX = new List<double>();

        public double WhiteKeyWidth { get; private set; }
        public double BlackKeyWidthCDE { get; private set; }
        public double BlackKeyWidthFGAB { get; private set; }
        public double BlackKeyWidth { get; private set; }
        public double SlotWidth { get; private set; }

        private double _cachedWidth = -1;

        // Semitone-to-segment mapping within each group:
        // CDE group (semitones 0-4): C=0, C#=1, D=2, D#=3, E=4  → 5 segments
        // FGAB group (semitones 5-11): F=0, F#=1, G=2, G#=3, A=4, A#=5, B=6 → 7 segments
        private static readonly int[] SegmentIndex =
        {
            0, 1, 2, 3, 4,    // C, C#, D, D#, E
            0, 1, 2, 3, 4, 5, 6 // F, F#, G, G#, A, A#, B
        };

        public void RebuildIfNeeded(double width)
        {
            if (Math.Abs(width - _cachedWidth) < 0.001) return;
            _cachedWidth = width;

            int totalWhiteKeys = 0;
            for (int n = 0; n < 128; n++)
                if (!IsBlackKey[n % 12]) totalWhiteKeys++;

            WhiteKeyWidth = width / totalWhiteKeys;
            BlackKeyWidthCDE = 3.0 * WhiteKeyWidth / 5.0;
            BlackKeyWidthFGAB = 4.0 * WhiteKeyWidth / 7.0;
            BlackKeyWidth = (BlackKeyWidthCDE + BlackKeyWidthFGAB) / 2.0;
            SlotWidth = BlackKeyWidth;

            int whiteIndex = 0;
            for (int note = 0; note < 128; note++)
            {
                WhiteKeyBottomLeft[note] = -1;
                WhiteKeyBottomRight[note] = -1;
                KeyTopLeft[note] = -1;
                KeyTopRight[note] = -1;

                if (!IsBlackKey[note % 12])
                {
                    WhiteKeyBottomLeft[note] = whiteIndex * WhiteKeyWidth;
                    WhiteKeyBottomRight[note] = (whiteIndex + 1) * WhiteKeyWidth;
                    XCenter[note] = (whiteIndex + 0.5) * WhiteKeyWidth;
                    NoteWidth[note] = WhiteKeyWidth;
                    whiteIndex++;
                }
            }

            for (int note = 0; note < 128; note++)
            {
                int semitone = note % 12;
                bool isCDEgroup = semitone <= 4;
                int segIdx = SegmentIndex[semitone];

                int octaveBase = note - semitone;

                double groupStartX;
                double segWidth;

                if (isCDEgroup)
                {
                    groupStartX = GetWhiteKeyBottomLeft(octaveBase);
                    segWidth = 3.0 * WhiteKeyWidth / 5.0;
                }
                else
                {
                    groupStartX = GetWhiteKeyBottomLeft(octaveBase + 5);
                    segWidth = 4.0 * WhiteKeyWidth / 7.0;
                }

                if (groupStartX < 0)
                {
                    if (!IsBlackKey[semitone])
                    {
                        KeyTopLeft[note] = WhiteKeyBottomLeft[note];
                        KeyTopRight[note] = WhiteKeyBottomRight[note];
                    }
                    continue;
                }

                double topLeft = groupStartX + segIdx * segWidth;
                double topRight = groupStartX + (segIdx + 1) * segWidth;

                KeyTopLeft[note] = topLeft;
                KeyTopRight[note] = topRight;

                if (IsBlackKey[semitone])
                {
                    XCenter[note] = (topLeft + topRight) / 2.0;
                    NoteWidth[note] = segWidth;
                }
            }

            for (int note = 0; note < 128; note++)
            {
                if (IsBlackKey[note % 12]) continue;
                if (KeyTopLeft[note] < 0) continue;

                bool hasBlackLeft = note > 0 && IsBlackKey[(note - 1) % 12];
                if (!hasBlackLeft)
                    KeyTopLeft[note] = WhiteKeyBottomLeft[note];

                bool hasBlackRight = note < 127 && IsBlackKey[(note + 1) % 12];
                if (!hasBlackRight)
                    KeyTopRight[note] = WhiteKeyBottomRight[note];
            }

            for (int note = 0; note < 128; note++)
            {
                if (KeyTopLeft[note] >= 0 && KeyTopRight[note] >= 0)
                    GuideXUniform[note] = (KeyTopLeft[note] + KeyTopRight[note]) / 2.0;
                else
                    GuideXUniform[note] = XCenter[note];
            }

            OctaveBoundaryX.Clear();
            for (int note = 0; note < 128; note++)
            {
                if (note % 12 != 0 || note == 0) continue;
                if (WhiteKeyBottomLeft[note] >= 0)
                    OctaveBoundaryX.Add(WhiteKeyBottomLeft[note]);
            }
        }

        private double GetWhiteKeyBottomLeft(int noteNumber)
        {
            if (noteNumber < 0 || noteNumber >= 128) return -1;
            return WhiteKeyBottomLeft[noteNumber];
        }

        /// <summary>
        /// Find the MIDI note at a layout-space X position.
        /// </summary>
        /// <param name="x">X coordinate in the layout's coordinate space (0 = leftmost key edge).</param>
        /// <param name="useTopGeometry">
        /// True to test the narrow-top region (where black keys exist) — black keys win over white narrow tops.
        /// False to test the wide-bottom region — only white keys can match.
        /// </param>
        public int? FindNoteAtX(double x, bool useTopGeometry)
        {
            if (useTopGeometry)
            {
                for (int note = 0; note < 128; note++)
                {
                    if (!IsBlackKey[note % 12]) continue;
                    double l = KeyTopLeft[note];
                    double r = KeyTopRight[note];
                    if (l < 0) continue;
                    if (x >= l && x < r) return note;
                }
                for (int note = 0; note < 128; note++)
                {
                    if (IsBlackKey[note % 12]) continue;
                    double l = KeyTopLeft[note];
                    double r = KeyTopRight[note];
                    if (l < 0) continue;
                    if (x >= l && x < r) return note;
                }
            }
            else
            {
                for (int note = 0; note < 128; note++)
                {
                    if (IsBlackKey[note % 12]) continue;
                    double l = WhiteKeyBottomLeft[note];
                    double r = WhiteKeyBottomRight[note];
                    if (l < 0) continue;
                    if (x >= l && x < r) return note;
                }
            }
            return null;
        }
    }
}
