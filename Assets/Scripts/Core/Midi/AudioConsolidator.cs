using System.Collections.Generic;

namespace Singularidi.Midi
{
    /// <summary>
    /// Per-(track, pitch) retrigger consolidation. When a NoteOn collides with an active same-pitch
    /// same-track voice, the prior voice's EndSeconds is shortened to the new note's StartSeconds —
    /// i.e., a synthetic NoteOff is emitted before the retrigger. Mirrors real-piano hammer behavior.
    ///
    /// Output retains every input note (none dropped), only EndSeconds may be shortened.
    /// </summary>
    public static class AudioConsolidator
    {
        public static NoteEvent[] Consolidate(IReadOnlyList<NoteEvent> notes)
        {
            int n = notes.Count;
            if (n == 0) return System.Array.Empty<NoteEvent>();

            // Bucket by (track, pitch) preserving original index so we can write back into the result.
            var buckets = new Dictionary<(int track, int pitch), List<int>>(capacity: 128);
            for (int i = 0; i < n; i++)
            {
                var key = (notes[i].Track, notes[i].NoteNumber);
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    buckets[key] = list;
                }
                list.Add(i);
            }

            var result = new NoteEvent[n];
            for (int i = 0; i < n; i++) result[i] = notes[i];

            foreach (var bucket in buckets.Values)
            {
                if (bucket.Count < 2) continue;

                // Sort indices by StartSeconds ascending; tie-break by original index for determinism.
                bucket.Sort((aIdx, bIdx) =>
                {
                    int c = result[aIdx].StartSeconds.CompareTo(result[bIdx].StartSeconds);
                    return c != 0 ? c : aIdx.CompareTo(bIdx);
                });

                for (int k = 0; k < bucket.Count - 1; k++)
                {
                    int prior = bucket[k];
                    int next = bucket[k + 1];
                    double nextStart = result[next].StartSeconds;
                    if (result[prior].EndSeconds > nextStart)
                    {
                        result[prior] = result[prior] with { EndSeconds = nextStart };
                    }
                }
            }

            return result;
        }
    }
}
