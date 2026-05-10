using NUnit.Framework;
using Singularidi.Visualization;

namespace Singularidi.Tests.Math
{
    public class RgbaColorTests
    {
        [Test]
        public void FromHex_RGB_DefaultsAlphaToFull()
        {
            var c = RgbaColor.FromHex("#FF5050");
            Assert.That(c.ByteA, Is.EqualTo(255));
            Assert.That(c.ByteR, Is.EqualTo(0xFF));
            Assert.That(c.ByteG, Is.EqualTo(0x50));
            Assert.That(c.ByteB, Is.EqualTo(0x50));
        }

        [Test]
        public void FromHex_ARGB_ParsesAllFour()
        {
            var c = RgbaColor.FromHex("#19FFFFFF");
            Assert.That(c.ByteA, Is.EqualTo(0x19));
            Assert.That(c.ByteR, Is.EqualTo(0xFF));
            Assert.That(c.ByteG, Is.EqualTo(0xFF));
            Assert.That(c.ByteB, Is.EqualTo(0xFF));
        }

        [Test]
        public void ToHex_OutputsAARRGGBB()
        {
            var c = new RgbaColor(1f, 0f, 0f, 0.5f);
            var hex = c.ToHex();
            // 0.5f → 128 with banker's rounding; could be 0x7F or 0x80 depending on rounding mode.
            Assert.That(hex, Does.Match("^#(7F|80)FF0000$"));
        }

        [Test]
        public void RoundTrip_PreservesAllBytes()
        {
            var c = RgbaColor.FromHex("#19FFFFFF");
            Assert.That(c.ToHex(), Is.EqualTo("#19FFFFFF"));
        }

        [Test]
        public void FromHex_NoLeadingHash_StillParses()
        {
            var c = RgbaColor.FromHex("FF5050");
            Assert.That(c.ByteR, Is.EqualTo(0xFF));
        }

        [Test]
        public void FromHex_BadLength_Throws()
        {
            Assert.Throws<System.FormatException>(() => RgbaColor.FromHex("#FFF"));
            Assert.Throws<System.FormatException>(() => RgbaColor.FromHex("#FFFFFFFFF"));
        }

        [Test]
        public void Equality_Works()
        {
            var a = new RgbaColor(0.5f, 0.5f, 0.5f, 1f);
            var b = new RgbaColor(0.5f, 0.5f, 0.5f, 1f);
            var c = new RgbaColor(0.5f, 0.5f, 0.4f, 1f);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
        }
    }
}
