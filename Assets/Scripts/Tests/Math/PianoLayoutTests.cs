using NUnit.Framework;
using Singularidi.Visualization;

namespace Singularidi.Tests.Math
{
    public class PianoLayoutTests
    {
        [Test]
        public void Rebuild_AssignsNonZeroWidths()
        {
            var layout = new PianoLayout();
            layout.RebuildIfNeeded(1920);

            Assert.That(layout.WhiteKeyWidth, Is.GreaterThan(0));
            Assert.That(layout.BlackKeyWidthCDE, Is.GreaterThan(0));
            Assert.That(layout.BlackKeyWidthFGAB, Is.GreaterThan(0));
            Assert.That(layout.NoteWidth[60], Is.GreaterThan(0)); // middle C
        }

        [Test]
        public void MiddleC_HasExpectedXCenter()
        {
            // 75 white keys tile width 1920, so each white key = 25.6 px wide.
            // Middle C (note 60) is the 36th white key (counting from A0=21? — actually note 0 is C-1, and white keys before 60: count of !IsBlackKey[0..59] = 35).
            // So middle C white index = 35 (0-indexed), XCenter = (35 + 0.5) * 25.6 = 908.8.
            var layout = new PianoLayout();
            layout.RebuildIfNeeded(1920);

            int whitesBefore60 = 0;
            for (int i = 0; i < 60; i++)
                if (!PianoLayout.IsBlackKey[i % 12]) whitesBefore60++;

            double expectedXCenter = (whitesBefore60 + 0.5) * layout.WhiteKeyWidth;
            Assert.That(layout.XCenter[60], Is.EqualTo(expectedXCenter).Within(0.001));
        }

        [TestCase(60)]
        [TestCase(0)]
        [TestCase(127)]
        [TestCase(45)]
        [TestCase(72)]
        public void FindNoteAtX_RoundTripsXCenter(int note)
        {
            var layout = new PianoLayout();
            layout.RebuildIfNeeded(1920);

            int? found = layout.FindNoteAtX(layout.XCenter[note], useTopGeometry: true);
            Assert.That(found, Is.EqualTo(note));
        }

        [Test]
        public void OctaveBoundaryX_HasExpectedCount()
        {
            var layout = new PianoLayout();
            layout.RebuildIfNeeded(1920);

            // Boundaries between octaves: at C12, C24, ..., C120 (10 boundaries; first C is at note 0 and skipped per legacy logic).
            Assert.That(layout.OctaveBoundaryX.Count, Is.GreaterThanOrEqualTo(9));
            Assert.That(layout.OctaveBoundaryX.Count, Is.LessThanOrEqualTo(10));
        }
    }
}
