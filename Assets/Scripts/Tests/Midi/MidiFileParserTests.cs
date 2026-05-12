using System.IO;
using NUnit.Framework;
using Singularidi.Midi;
using UnityEngine;

namespace Singularidi.Tests.Midi
{
    public class MidiFileParserTests
    {
        [Test]
        public void Parse_SampleMid_ProducesNotes()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Midi", "Sample.mid");
            if (!File.Exists(path))
                Assert.Inconclusive($"Sample.mid not found at {path}");

            var parser = new DryWetMidiFileParser();
            var (notes, totalDuration) = parser.Parse(path);

            Assert.That(notes, Is.Not.Empty, "Sample.mid should contain at least one note.");
            Assert.That(totalDuration, Is.GreaterThan(0), "Total duration must be positive.");

            for (int i = 1; i < notes.Count; i++)
            {
                Assert.That(notes[i].StartSeconds, Is.GreaterThanOrEqualTo(notes[i - 1].StartSeconds),
                    $"Notes must be sorted ascending by StartSeconds; violated at index {i}.");
            }

            foreach (var n in notes)
            {
                Assert.That(n.NoteNumber, Is.InRange(0, 127));
                Assert.That(n.Channel, Is.InRange(0, 15));
                Assert.That(n.Velocity, Is.InRange(0, 127));
                Assert.That(n.EndSeconds, Is.GreaterThanOrEqualTo(n.StartSeconds));
                Assert.That(n.Track, Is.GreaterThanOrEqualTo(0));
            }
        }
    }
}
