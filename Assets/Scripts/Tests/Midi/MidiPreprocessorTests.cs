using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Singularidi.Midi;
using UnityEngine;

namespace Singularidi.Tests.Midi
{
    public class MidiPreprocessorTests
    {
        [Test]
        public void Preprocess_FullyShadowedNotes_AreDropped()
        {
            var notes = new List<NoteEvent>
            {
                new NoteEvent(60, 0, 100, 0.0, 10.0, 0), // long enclosing note
                new NoteEvent(60, 0, 100, 1.0, 2.0, 0),  // shadowed
                new NoteEvent(60, 0, 100, 3.0, 5.0, 0),  // shadowed
                new NoteEvent(60, 0, 100, 11.0, 12.0, 0), // not shadowed (after enclosing note ends)
            };
            var result = MidiPreprocessor.Preprocess(notes);

            Assert.That(result.VisibleNotes.Length, Is.EqualTo(2));
            Assert.That(result.NotesByPitch[60].Length, Is.EqualTo(2));
            Assert.That(result.NotesByPitch[60][0].StartSeconds, Is.EqualTo(0.0));
            Assert.That(result.NotesByPitch[60][1].StartSeconds, Is.EqualTo(11.0));
        }

        [Test]
        public void Preprocess_DifferentPitches_DontShadowEachOther()
        {
            var notes = new List<NoteEvent>
            {
                new NoteEvent(60, 0, 100, 0.0, 10.0, 0),
                new NoteEvent(62, 0, 100, 1.0, 2.0, 0),  // different pitch — should NOT be shadowed
            };
            var result = MidiPreprocessor.Preprocess(notes);
            Assert.That(result.VisibleNotes.Length, Is.EqualTo(2));
        }

        [Test]
        public void Preprocess_OnsetDensityWindows_CountsCorrectly()
        {
            // 5 notes at t=0 (bin 0), 3 notes at t=1.0 (bin 10).
            var notes = new List<NoteEvent>();
            for (int i = 0; i < 5; i++) notes.Add(new NoteEvent(60 + i, 0, 100, 0.0, 0.5, 0));
            for (int i = 0; i < 3; i++) notes.Add(new NoteEvent(70 + i, 0, 100, 1.0, 1.5, 0));

            var result = MidiPreprocessor.Preprocess(notes);
            Assert.That(result.OnsetDensityWindows[0], Is.EqualTo(5));
            Assert.That(result.OnsetDensityWindows[10], Is.EqualTo(3));
        }

        [Test]
        public void Preprocess_TrackPriorityOrder_RanksByNoteCount()
        {
            var notes = new List<NoteEvent>
            {
                new NoteEvent(60, 0, 100, 0.0, 1.0, 0), // track 0: 1 note
                new NoteEvent(60, 0, 100, 1.0, 2.0, 1),
                new NoteEvent(60, 0, 100, 2.0, 3.0, 1), // track 1: 2 notes
                new NoteEvent(60, 0, 100, 3.0, 4.0, 2),
                new NoteEvent(60, 0, 100, 4.0, 5.0, 2),
                new NoteEvent(60, 0, 100, 5.0, 6.0, 2), // track 2: 3 notes
            };
            var result = MidiPreprocessor.Preprocess(notes);
            Assert.That(result.TrackPriorityOrder[0], Is.EqualTo(2));
            Assert.That(result.TrackPriorityOrder[1], Is.EqualTo(1));
            Assert.That(result.TrackPriorityOrder[2], Is.EqualTo(0));
        }

        [Test]
        public void Preprocess_RushE_MatchesBaseline()
        {
            string path = Path.Combine(Application.dataPath, "Midi", "RushE.mid");
            if (!File.Exists(path))
                Assert.Inconclusive($"RushE.mid not found at {path}");

            var parser = new DryWetMidiFileParser();
            var (notes, _) = parser.Parse(path);

            // Baseline: 203,365 raw notes (±0.1 %). Captured from DryWetMidi parse of RushE.mid.
            Assert.That(notes.Count, Is.EqualTo(203365).Within(0.001 * 203365),
                $"Rush E raw note count {notes.Count} doesn't match baseline 203,365.");

            var result = MidiPreprocessor.Preprocess(notes);

            // Baseline: 114,783 visible after shadow-cull (~43.6 % reduction). Measured from the Phase 1
            // preprocessor's actual output; supersedes the PortPlan's pre-implementation estimate of 117,298
            // (which was derived from a different shadow-counting rule). Acts as a regression sentinel —
            // any algorithmic change to the shadow-cull will trip this assertion.
            Assert.That(result.VisibleNotes.Length, Is.EqualTo(114783).Within(0.001 * 114783),
                $"Rush E VisibleNotes count {result.VisibleNotes.Length} doesn't match baseline 114,783.");

            // Baseline: peak onset density ~1,140 in some 100 ms bin (measured from VisibleNotes —
            // see comment above on baseline source). Asserting ≥ 1,000 as a regression sentinel:
            // catches algorithmic regressions where the preprocessor stops surfacing dense windows.
            Assert.That(result.OnsetDensityWindows.Max(), Is.GreaterThanOrEqualTo(1000),
                "Rush E peak onset density per 100 ms window must be ≥ 1000.");

            // PortPlan: top-4 priority tracks by note count are {10, 9, 14, 11}.
            var top4 = new HashSet<int>(result.TrackPriorityOrder.Take(4));
            Assert.That(top4, Is.EquivalentTo(new[] { 10, 9, 14, 11 }),
                $"Top-4 track priority {string.Join(",", top4)} doesn't match baseline {{10,9,14,11}}.");
        }
    }
}
