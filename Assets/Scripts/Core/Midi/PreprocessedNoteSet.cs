namespace Singularidi.Midi
{
    /// <summary>
    /// Output of <see cref="MidiPreprocessor"/>. Holds the four render-time outputs plus the
    /// audio-consolidated note set used by the offline audio renderer (Phase 9).
    /// </summary>
    public sealed class PreprocessedNoteSet
    {
        /// <summary>Notes minus same-pitch fully-shadowed notes. Sorted by start. ~42 % reduction on Rush E worst-case.</summary>
        public NoteEvent[] VisibleNotes { get; }

        /// <summary>Per-pitch sorted-by-start array, length 128. Subset of <see cref="VisibleNotes"/>.</summary>
        public NoteEvent[][] NotesByPitch { get; }

        /// <summary>Onset count per 100 ms bin across the song. Drives adaptive LOD lookahead.</summary>
        public int[] OnsetDensityWindows { get; }

        /// <summary>Track indices sorted by render priority (highest priority first). Drives draw order and effect-budget allocation.</summary>
        public int[] TrackPriorityOrder { get; }

        /// <summary>Audio-side note set with per-pitch retrigger consolidation applied. Used only by the offline audio renderer.</summary>
        public NoteEvent[] AudioConsolidatedNotes { get; }

        public PreprocessedNoteSet(
            NoteEvent[] visibleNotes,
            NoteEvent[][] notesByPitch,
            int[] onsetDensityWindows,
            int[] trackPriorityOrder,
            NoteEvent[] audioConsolidatedNotes)
        {
            VisibleNotes = visibleNotes;
            NotesByPitch = notesByPitch;
            OnsetDensityWindows = onsetDensityWindows;
            TrackPriorityOrder = trackPriorityOrder;
            AudioConsolidatedNotes = audioConsolidatedNotes;
        }
    }
}
