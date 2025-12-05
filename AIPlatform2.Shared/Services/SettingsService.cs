// AIPlatform.Shared/Services/SettingsService.cs
using Microsoft.FluentUI.AspNetCore.Components;
using System;
using System.IO;
using System.Text.Json;

namespace AIPlatform2.Shared.Services
{
    public class AppSettings
    {
        public string ModelRootPath { get; set; } = "";
        public DesignThemeModes ThemeMode { get; set; }
        public OfficeColor ThemeColor { get; set; }
    }

    public class SettingsService
    {
        private readonly string _settingsFolder;
        private readonly string _settingsFile;
        private AppSettings _settings;
        private readonly object _lock = new object();

        // Event fired when model root changes
        public event EventHandler<string?>? ModelRootPathChanged;

        public SettingsService(string appName = "AIPlatform")
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsFolder = Path.Combine(appData, appName);
            _settingsFile = Path.Combine(_settingsFolder, "settings.json");

            if (!Directory.Exists(_settingsFolder))
                Directory.CreateDirectory(_settingsFolder);

            _settings = LoadSettings();
        }

        private AppSettings LoadSettings()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_settingsFile))
                    {
                        var json = File.ReadAllText(_settingsFile);
                        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    }
                }
                catch
                {
                    // ignore and fallback to defaults
                }

                return new AppSettings();
            }
        }

        private void SaveSettings()
        {
            lock (_lock)
            {
                try
                {
                    // write to temp file then replace atomically to reduce corruption risk
                    var tmp = _settingsFile + ".tmp";
                    var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(tmp, json);
                    File.Move(tmp, _settingsFile, overwrite: true);
                }
                catch
                {
                    // best-effort save; swallow exceptions to avoid crashing UI—log in real app
                }
            }
        }

        // Public API -------------------------
        public string GetModelRootPath()
        {
            lock (_lock)
                return _settings.ModelRootPath ?? "";
        }

        // Try-get pattern (no exceptions)
        public bool TryGetModelRoot(out string path)
        {
            lock (_lock)
            {
                path = _settings.ModelRootPath ?? "";
                return !string.IsNullOrWhiteSpace(path);
            }
        }

        public void SetModelRootPath(string path)
        {
            string normalized = path?.Trim() ?? "";
            lock (_lock)
            {
                if (_settings.ModelRootPath == normalized) return;
                _settings.ModelRootPath = normalized;
                SaveSettings();
            }
            // fire event outside lock
            ModelRootPathChanged?.Invoke(this, normalized);
        }

        /// <summary>
        /// Validate that a model root path exists and points to a directory.
        /// Returns true if valid, and outputs an error string otherwise.
        /// </summary>
        public bool ValidateModelRootPath(out string? error)
        {
            var path = GetModelRootPath();

            if (string.IsNullOrWhiteSpace(path))
            {
                error = "Model folder not set.";
                return false;
            }

            if (!Directory.Exists(path))
            {
                error = "Folder does not exist.";
                return false;
            }

            error = null;
            return true;
        }

        public DesignThemeModes GetThemeMode()
        {
            lock (_lock)
                return _settings.ThemeMode;
        }
        public OfficeColor GetThemeColor()
        {
            lock (_lock)
                return _settings.ThemeColor;
        }

        public void SetThemeMode(DesignThemeModes mode)
        {
            lock (_lock)
            {
                _settings.ThemeMode = mode;
                SaveSettings();
            }
        }
        public void SetThemeColor(OfficeColor color)
        {
            lock (_lock)
            {
                _settings.ThemeColor = color;
                SaveSettings();
            }
        }
    }
}
