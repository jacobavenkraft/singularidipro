#nullable enable
using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Singularidi.Config;
using Singularidi.Unity.Config;
using UnityEngine;
using UnityEngine.TestTools;

namespace Singularidi.Tests.Config
{
    public class UnityConfigServiceTests
    {
        private string _persistentDir = null!;
        private string _legacyDir = null!;

        [SetUp]
        public void SetUp()
        {
            // Each test gets isolated temp directories so concurrent runs and prior failures
            // never bleed into the current case.
            string root = Path.Combine(Path.GetTempPath(), "SingularidiTests_" + Guid.NewGuid().ToString("N"));
            _persistentDir = Path.Combine(root, "persistent");
            _legacyDir = Path.Combine(root, "legacy");
            Directory.CreateDirectory(_persistentDir);
            Directory.CreateDirectory(_legacyDir);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                string root = Directory.GetParent(_persistentDir)!.FullName;
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
            catch
            {
                // Cleanup is best-effort; a stuck file handle should not fail the test.
            }
        }

        [Test]
        public void Load_NoConfigOrLegacy_WritesDefaultsAndReturnsThem()
        {
            var svc = new UnityConfigService(_persistentDir, legacyConfigPath: null);

            var cfg = svc.Load();

            Assert.That(cfg, Is.Not.Null);
            Assert.That(File.Exists(svc.ConfigPath), "First-run Load must persist defaults to disk.");
            Assert.That(cfg.ThemeName, Is.EqualTo("Dark"), "AppConfig default ThemeName must round-trip.");
            Assert.That(cfg.OfflineRenderMode, Is.EqualTo(OfflineRenderMode.Full128));
        }

        [Test]
        public void SaveThenLoad_RoundTripsPropertyValues()
        {
            var svc = new UnityConfigService(_persistentDir, legacyConfigPath: null);
            var original = new AppConfig
            {
                SoundFontPath = "/test/path/sample.sf2",
                ThemeName = "CustomTheme",
                ExportWidth = 3840,
                ExportHeight = 2160,
                ExportFps = 120,
                OfflineRenderMode = OfflineRenderMode.RealPiano88,
                OutOfRangeBehavior = OutOfRangeBehavior.TransposeToNearestOctave,
                OfflineMasterLimiterDb = -6.0,
                OfflineReverbEnabled = true,
            };

            svc.Save(original);
            var loaded = svc.Load();

            Assert.That(loaded.SoundFontPath, Is.EqualTo("/test/path/sample.sf2"));
            Assert.That(loaded.ThemeName, Is.EqualTo("CustomTheme"));
            Assert.That(loaded.ExportWidth, Is.EqualTo(3840));
            Assert.That(loaded.ExportHeight, Is.EqualTo(2160));
            Assert.That(loaded.ExportFps, Is.EqualTo(120));
            Assert.That(loaded.OfflineRenderMode, Is.EqualTo(OfflineRenderMode.RealPiano88));
            Assert.That(loaded.OutOfRangeBehavior, Is.EqualTo(OutOfRangeBehavior.TransposeToNearestOctave));
            Assert.That(loaded.OfflineMasterLimiterDb, Is.EqualTo(-6.0));
            Assert.That(loaded.OfflineReverbEnabled, Is.True);
        }

        [Test]
        public void Load_MalformedJson_ReturnsDefaultsInsteadOfThrowing()
        {
            string configPath = Path.Combine(_persistentDir, "config.json");
            File.WriteAllText(configPath, "{this is not valid json}");

            // UnityConfigService logs an error for the parse failure before falling back. The
            // logged error is expected; the test framework would otherwise fail the test on it.
            LogAssert.Expect(LogType.Error, new Regex(@"\[UnityConfigService\] Failed to read"));

            var svc = new UnityConfigService(_persistentDir, legacyConfigPath: null);
            var cfg = svc.Load();

            Assert.That(cfg, Is.Not.Null, "Malformed JSON must not crash Load().");
            Assert.That(cfg.ThemeName, Is.EqualTo("Dark"), "Malformed JSON must fall back to AppConfig defaults.");
        }

        [Test]
        public void Load_MigratesLegacyConfig_WhenNewPathMissing()
        {
            string legacyPath = Path.Combine(_legacyDir, "config.json");
            File.WriteAllText(legacyPath,
                "{\"SoundFontPath\":\"/legacy/font.sf2\",\"ThemeName\":\"LegacyTheme\",\"ExportFps\":24}");

            var svc = new UnityConfigService(_persistentDir, legacyPath);
            var cfg = svc.Load();

            Assert.That(cfg.SoundFontPath, Is.EqualTo("/legacy/font.sf2"));
            Assert.That(cfg.ThemeName, Is.EqualTo("LegacyTheme"));
            Assert.That(cfg.ExportFps, Is.EqualTo(24));
            Assert.That(File.Exists(svc.ConfigPath),
                "Migration must have copied the legacy file to the new persistentDataPath.");
        }

        [Test]
        public void Load_DoesNotOverwriteExistingConfig_WithLegacy()
        {
            // Existing config at the new path
            var existingSvc = new UnityConfigService(_persistentDir, legacyConfigPath: null);
            existingSvc.Save(new AppConfig { ThemeName = "ExistingWinner" });

            // Legacy file also present
            string legacyPath = Path.Combine(_legacyDir, "config.json");
            File.WriteAllText(legacyPath, "{\"ThemeName\":\"LegacyLoser\"}");

            var migratingSvc = new UnityConfigService(_persistentDir, legacyPath);
            var cfg = migratingSvc.Load();

            Assert.That(cfg.ThemeName, Is.EqualTo("ExistingWinner"),
                "When both files exist, the new path must win and legacy must not overwrite.");
        }

        [Test]
        public void Load_LegacyPathDoesNotExist_FallsBackToFreshDefaults()
        {
            string nonExistentLegacy = Path.Combine(_legacyDir, "does-not-exist.json");

            var svc = new UnityConfigService(_persistentDir, nonExistentLegacy);
            var cfg = svc.Load();

            Assert.That(cfg, Is.Not.Null);
            Assert.That(File.Exists(svc.ConfigPath), "Default config must still be written when legacy is absent.");
            Assert.That(cfg.ThemeName, Is.EqualTo("Dark"));
        }

        [Test]
        public void Save_IgnoresIOErrors_AndDoesNotThrow()
        {
            // Point ConfigPath at a deliberately invalid location (a file path that contains a
            // directory whose parent is a regular file) and confirm Save catches the IOException.
            string invalidDir = Path.Combine(_persistentDir, "is-a-file.json", "nested");
            File.WriteAllText(Path.Combine(_persistentDir, "is-a-file.json"), "blocker");

            var svc = new UnityConfigService(invalidDir, legacyConfigPath: null);

            // The service contract is to log the IO error and continue. The logged error is
            // expected; the test framework would otherwise fail the test on it.
            LogAssert.Expect(LogType.Error, new Regex(@"\[UnityConfigService\] Failed to write"));

            // Save must not throw.
            Assert.DoesNotThrow(() => svc.Save(new AppConfig()));
        }
    }
}
