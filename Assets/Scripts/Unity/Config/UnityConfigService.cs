#nullable enable
using System;
using System.IO;
using Newtonsoft.Json;
using Singularidi.Config;
using UnityEngine;

namespace Singularidi.Unity.Config
{
    // Unity-side IConfigService. Stores AppConfig as JSON under Application.persistentDataPath/config.json.
    //
    // First-run migration: if no config exists at the new path but a legacy
    // %APPDATA%\Singularidi\config.json from the old Avalonia app is present, copy it forward.
    // Window size fields are additive (defaults safe) — legacy files lacking them deserialize fine.
    //
    // Serializer: Newtonsoft.Json (com.unity.nuget.newtonsoft-json). Unity Mono ships without
    // System.Text.Json — the legacy app used System.Text.Json but it's unavailable here. Newtonsoft
    // is the Unity-standard alternative and handles AppConfig's property-based shape correctly
    // (JsonUtility cannot — it serializes fields only).
    public sealed class UnityConfigService : IConfigService
    {
        private const string ConfigFileName = "config.json";

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        private readonly string _configPath;
        private readonly string? _legacyConfigPath;

        public UnityConfigService()
            : this(Application.persistentDataPath, TryGetLegacyAppDataPath())
        {
        }

        // Test-friendly constructor: callers can override both locations.
        public UnityConfigService(string persistentDataDirectory, string? legacyConfigPath)
        {
            _configPath = Path.Combine(persistentDataDirectory, ConfigFileName);
            _legacyConfigPath = legacyConfigPath;
        }

        public string ConfigPath => _configPath;

        public AppConfig Load()
        {
            if (!File.Exists(_configPath))
            {
                if (TryMigrateLegacy())
                {
                    Debug.Log($"[UnityConfigService] Migrated legacy config from '{_legacyConfigPath}' to '{_configPath}'.");
                }
                else
                {
                    var fresh = new AppConfig();
                    Save(fresh);
                    Debug.Log($"[UnityConfigService] No config found; wrote defaults to '{_configPath}'.");
                    return fresh;
                }
            }

            try
            {
                var json = File.ReadAllText(_configPath);
                return JsonConvert.DeserializeObject<AppConfig>(json, JsonSettings) ?? new AppConfig();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityConfigService] Failed to read '{_configPath}': {ex.Message}. Using defaults.");
                return new AppConfig();
            }
        }

        public void Save(AppConfig cfg)
        {
            try
            {
                var dir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(_configPath, JsonConvert.SerializeObject(cfg, JsonSettings));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityConfigService] Failed to write '{_configPath}': {ex.Message}.");
            }
        }

        private bool TryMigrateLegacy()
        {
            if (string.IsNullOrEmpty(_legacyConfigPath) || !File.Exists(_legacyConfigPath))
                return false;

            try
            {
                var dir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.Copy(_legacyConfigPath, _configPath, overwrite: false);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnityConfigService] Legacy migration failed ('{_legacyConfigPath}' → '{_configPath}'): {ex.Message}.");
                return false;
            }
        }

        private static string? TryGetLegacyAppDataPath()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrEmpty(appData))
                    return null;

                return Path.Combine(appData, "Singularidi", "config.json");
            }
            catch
            {
                return null;
            }
        }
    }
}
