using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Singularidi.Midi;
using UnityEngine;

namespace Singularidi.Tests.Midi
{
    public class AudioConsolidatorTests
    {
        [Test]
        public void Consolidate_OverlappingSamePitchSameTrack_ShortensPriorEnd()
        {
            var notes = new List<NoteEvent>
            {
                new NoteEvent(60, 0, 100, 0.0, 5.0, 0),
                new NoteEvent(60, 0, 100, 2.0, 4.0, 0), // overlaps prior; prior should be cut to end at 2.0
            };
            var result = AudioConsolidator.Consolidate(notes);
            Assert.That(result.Length, Is.EqualTo(2));

            var sorted = result.OrderBy(n => n.StartSeconds).ToArray();
            Assert.That(sorted[0].EndSeconds, Is.EqualTo(2.0));
            Assert.That(sorted[1].EndSeconds, Is.EqualTo(4.0));
        }

        [Test]
        public void Consolidate_DifferentTracks_DontInterfere()
        {
            var notes = new List<NoteEvent>
            {
                new NoteEvent(60, 0, 100, 0.0, 5.0, 0),
                new NoteEvent(60, 0, 100, 2.0, 4.0, 1), // different track — both kept untouched
            };
            var result = AudioConsolidator.Consolidate(notes);
            Assert.That(result[0].EndSeconds, Is.EqualTo(5.0));
            Assert.That(result[1].EndSeconds, Is.EqualTo(4.0));
        }

        [Test]
        public void Consolidate_DifferentPitches_DontInterfere()
        {
            var notes = new List<NoteEvent>
            {
                new NoteEvent(60, 0, 100, 0.0, 5.0, 0),
                new NoteEvent(62, 0, 100, 2.0, 4.0, 0), // different pitch — both kept untouched
            };
            var result = AudioConsolidator.Consolidate(notes);
            Assert.That(result[0].EndSeconds, Is.EqualTo(5.0));
            Assert.That(result[1].EndSeconds, Is.EqualTo(4.0));
        }

        [Test]
        public void Consolidate_ChainedRetriggers_EachShortenedToNext()
        {
            var notes = new List<NoteEvent>
            {
                new NoteEvent(60, 0, 100, 0.0, 10.0, 0),
                new NoteEvent(60, 0, 100, 1.0, 10.0, 0),
                new NoteEvent(60, 0, 100, 2.0, 10.0, 0),
            };
            var result = AudioConsolidator.Consolidate(notes);
            var sorted = result.OrderBy(n => n.StartSeconds).ToArray();
            Assert.That(sorted[0].EndSeconds, Is.EqualTo(1.0));
            Assert.That(sorted[1].EndSeconds, Is.EqualTo(2.0));
            Assert.That(sorted[2].EndSeconds, Is.EqualTo(10.0));
        }

        [Test]
        public void Consolidate_OutputCountEqualsInputCount()
        {
            var notes = new List<NoteEvent>
            {
                new NoteEvent(60, 0, 100, 0.0, 5.0, 0),
                new NoteEvent(60, 0, 100, 1.0, 4.0, 0),
                new NoteEvent(60, 0, 100, 2.0, 3.0, 0),
            };
            var result = AudioConsolidator.Consolidate(notes);
            Assert.That(result.Length, Is.EqualTo(3), "Consolidate must not drop notes — only shorten them.");
        }

        [Test]
        public void Consolidate_RushE_NeverHasOverlapPerTrackPitch()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Midi", "RushE.mid");
            if (!File.Exists(path))
                Assert.Inconclusive($"RushE.mid not found at {path}");

            var parser = new DryWetMidiFileParser();
            var (notes, _) = parser.Parse(path);
            var result = AudioConsolidator.Consolidate(notes);

            Assert.That(result.Length, Is.EqualTo(notes.Count),
                "Output count must equal input count — Consolidate doesn't drop notes.");

            var groups = result.GroupBy(n => (n.Track, n.NoteNumber));
            int violations = 0;
            foreach (var group in groups)
            {
                var sorted = group.OrderBy(n => n.StartSeconds).ToArray();
                for (int i = 1; i < sorted.Length; i++)
                {
                    if (sorted[i].StartSeconds < sorted[i - 1].EndSeconds)
                        violations++;
                }
            }
            Assert.That(violations, Is.EqualTo(0),
                $"After consolidation, {violations} (track, pitch) pairs still have overlapping voices.");
        }
    }
}
