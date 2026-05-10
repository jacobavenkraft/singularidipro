#nullable enable
using System;
using System.Collections.Generic;
using Singularidi.Themes;

namespace Singularidi.Midi
{
    /// <summary>
    /// Single-pass preprocessor that runs once after <see cref="DryWetMidiFileParser"/> finishes.
    /// Produces the render-time inputs every visualization mode shares plus the audio-side
    /// consolidated note set used by the offline renderer.
    /// </summary>
    public static class MidiPreprocessor
    {
        private const double DensityWindowSeconds = 0.1;

        public static PreprocessedNoteSet Preprocess(IReadOnlyList<NoteEvent> notes, ThemeData? theme = null)
        {
            // 1. Per-pitch shadow-cull → VisibleNotes + NotesByPitch[128][].
            var notesByPitch = new NoteEvent[128][];
            var visibleByPitch = new List<NoteEvent>[128];
            var perPitch = new List<NoteEvent>[128];
            for (int p = 0; p < 128; p++)
            {
                perPitch[p] = new List<NoteEvent>();
                visibleByPitch[p] = new List<NoteEvent>();
            }

            foreach (var note in notes)
            {
                if ((uint)note.NoteNumber >= 128u) continue; // out-of-range guard
                perPitch[note.NoteNumber].Add(note);
            }

            int totalVisible = 0;
            for (int p = 0; p < 128; p++)
            {
                var bucket = perPitch[p];
                if (bucket.Count == 0)
                {
                    notesByPitch[p] = Array.Empty<NoteEvent>();
                    continue;
                }

                // Sort by start asc; tie-break by end desc so the longer note "wins" and shadows the shorter.
                bucket.Sort((a, b) =>
                {
                    int c = a.StartSeconds.CompareTo(b.StartSeconds);
                    return c != 0 ? c : b.EndSeconds.CompareTo(a.EndSeconds);
                });

                var visible = visibleByPitch[p];
                double maxEndSoFar = double.NegativeInfinity;
                foreach (var n in bucket)
                {
                    if (n.EndSeconds > maxEndSoFar)
                    {
                        visible.Add(n);
                        maxEndSoFar = n.EndSeconds;
                    }
                }

                notesByPitch[p] = visible.ToArray();
                totalVisible += visible.Count;
            }

            var visibleFlat = new NoteEvent[totalVisible];
            int idx = 0;
            for (int p = 0; p < 128; p++)
            {
                var arr = notesByPitch[p];
                for (int i = 0; i < arr.Length; i++) visibleFlat[idx++] = arr[i];
            }
            Array.Sort(visibleFlat, (a, b) => a.StartSeconds.CompareTo(b.StartSeconds));

            // 2. Onset-density windows over VisibleNotes.
            int[] onsetDensityWindows;
            if (visibleFlat.Length == 0)
            {
                onsetDensityWindows = Array.Empty<int>();
            }
            else
            {
                double maxStart = 0;
                for (int i = 0; i < visibleFlat.Length; i++)
                {
                    if (visibleFlat[i].StartSeconds > maxStart) maxStart = visibleFlat[i].StartSeconds;
                }
                int binCount = (int)Math.Floor(maxStart / DensityWindowSeconds) + 1;
                onsetDensityWindows = new int[binCount];
                for (int i = 0; i < visibleFlat.Length; i++)
                {
                    int bin = (int)(visibleFlat[i].StartSeconds / DensityWindowSeconds);
                    if ((uint)bin < (uint)binCount) onsetDensityWindows[bin]++;
                }
            }

            // 3. Track priority — base = note count over the *unmodified* input set, theme override wins.
            var trackCounts = new Dictionary<int, int>();
            int maxTrack = -1;
            foreach (var note in notes)
            {
                if (!trackCounts.TryGetValue(note.Track, out int c)) c = 0;
                trackCounts[note.Track] = c + 1;
                if (note.Track > maxTrack) maxTrack = note.Track;
            }

            var trackKeys = new List<int>(trackCounts.Keys);
            // Effective priority: theme override > note count.
            int Priority(int track)
            {
                if (theme?.TrackPriorityOverrides != null
                    && theme.TrackPriorityOverrides.TryGetValue(track, out int over))
                    return over;
                return trackCounts.TryGetValue(track, out int c) ? c : 0;
            }

            trackKeys.Sort((a, b) =>
            {
                int pa = Priority(a);
                int pb = Priority(b);
                int c = pb.CompareTo(pa); // descending
                return c != 0 ? c : a.CompareTo(b); // tie-break by track index ascending
            });

            int[] trackPriorityOrder = trackKeys.ToArray();

            // 4. Audio consolidation runs on the unmodified input set, NOT on VisibleNotes.
            var audioConsolidated = AudioConsolidator.Consolidate(notes);

            return new PreprocessedNoteSet(
                visibleNotes: visibleFlat,
                notesByPitch: notesByPitch,
                onsetDensityWindows: onsetDensityWindows,
                trackPriorityOrder: trackPriorityOrder,
                audioConsolidatedNotes: audioConsolidated);
        }
    }
}
