using System.Collections.Generic;
using NUnit.Framework;
using Singularidi.Themes;

namespace Singularidi.Tests.Themes
{
    public class ThemeDataTests
    {
        [Test]
        public void Default_HasSchemaVersion2()
        {
            var theme = new ThemeData();
            Assert.That(theme.SchemaVersion, Is.EqualTo(2));
        }

        [Test]
        public void Default_RenderQualityDefaultsToRealtime()
        {
            var theme = new ThemeData();
            Assert.That(theme.RenderQualityDefault, Is.EqualTo(RenderQuality.Realtime));
        }

        [Test]
        public void Default_ChannelColors_HasSixteenEntries()
        {
            var theme = new ThemeData();
            Assert.That(theme.ChannelColors.Length, Is.EqualTo(16));
        }

        [Test]
        public void BackgroundColor_ParsesHexCorrectly()
        {
            var theme = new ThemeData { Background = "#19FFFFFF" };
            var c = theme.BackgroundColor;
            Assert.That(c.ByteA, Is.EqualTo(0x19));
            Assert.That(c.ByteR, Is.EqualTo(0xFF));
            Assert.That(c.ByteG, Is.EqualTo(0xFF));
            Assert.That(c.ByteB, Is.EqualTo(0xFF));
        }

        [Test]
        public void TrackColors_FallsBackToChannelColors_WhenTrackColorValuesNull()
        {
            var theme = new ThemeData();
            Assert.That(theme.TrackColorValues, Is.Null);
            Assert.That(theme.TrackColors.Length, Is.EqualTo(theme.ChannelColors.Length));
            Assert.That(theme.TrackColors[0], Is.EqualTo(theme.ChannelColors[0]));
        }

        [Test]
        public void TrackColors_UsesTrackColorValues_WhenSet()
        {
            var theme = new ThemeData
            {
                TrackColorValues = new List<string> { "#FF0000", "#00FF00", "#0000FF" }
            };
            Assert.That(theme.TrackColors.Length, Is.EqualTo(3));
            Assert.That(theme.TrackColors[0].ByteR, Is.EqualTo(0xFF));
            Assert.That(theme.TrackColors[1].ByteG, Is.EqualTo(0xFF));
            Assert.That(theme.TrackColors[2].ByteB, Is.EqualTo(0xFF));
        }

        [Test]
        public void NoteColorOverrides_ResolvesViaDictionary()
        {
            var theme = new ThemeData
            {
                NoteColorOverrideValues = new Dictionary<int, string>
                {
                    { 60, "#FFFF00" },
                    { 64, "#00FFFF" },
                }
            };
            var overrides = theme.NoteColorOverrides;
            Assert.That(overrides, Is.Not.Null);
            Assert.That(overrides[60].ByteR, Is.EqualTo(0xFF));
            Assert.That(overrides[60].ByteG, Is.EqualTo(0xFF));
            Assert.That(overrides[64].ByteB, Is.EqualTo(0xFF));
        }

        [Test]
        public void KeyColorOverrides_NullByDefault()
        {
            var theme = new ThemeData();
            Assert.That(theme.KeyColorOverrides, Is.Null);
        }

        [Test]
        public void Clone_DeepCopiesAllFields()
        {
            var theme = new ThemeData
            {
                Name = "TestTheme",
                Background = "#101010",
                TrackColorValues = new List<string> { "#FF0000", "#00FF00" },
                NoteColorOverrideValues = new Dictionary<int, string> { { 60, "#FFFF00" } },
                KeyColorOverrideValues = new Dictionary<int, string> { { 64, "#0000FF" } },
                ColorMode = NoteColorMode.Track,
                TrackPriorityOverrides = new Dictionary<int, int> { { 0, 100 } },
                RenderQualityDefault = RenderQuality.RealtimeHQ,
                SchemaVersion = 2,
            };

            var clone = theme.Clone();

            Assert.That(clone.Name, Is.EqualTo("TestTheme"));
            Assert.That(clone.Background, Is.EqualTo("#101010"));
            Assert.That(clone.ColorMode, Is.EqualTo(NoteColorMode.Track));
            Assert.That(clone.RenderQualityDefault, Is.EqualTo(RenderQuality.RealtimeHQ));

            // Mutating clone collections must not affect original.
            clone.TrackColorValues!.Add("#0000FF");
            Assert.That(theme.TrackColorValues!.Count, Is.EqualTo(2));

            clone.NoteColorOverrideValues![72] = "#888888";
            Assert.That(theme.NoteColorOverrideValues!.Count, Is.EqualTo(1));

            clone.TrackPriorityOverrides![1] = 50;
            Assert.That(theme.TrackPriorityOverrides!.Count, Is.EqualTo(1));
        }

        [Test]
        public void BuiltInThemes_DarkAndLight_AreDistinct()
        {
            var dark = BuiltInThemes.Dark();
            var light = BuiltInThemes.Light();
            Assert.That(dark.Name, Is.EqualTo("Dark"));
            Assert.That(light.Name, Is.EqualTo("Light"));
            Assert.That(dark.Background, Is.Not.EqualTo(light.Background));
        }
    }
}
