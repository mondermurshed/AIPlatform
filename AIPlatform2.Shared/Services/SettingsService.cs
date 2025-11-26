// AIPlatform.Shared/Services/SettingsService.cs
using Microsoft.FluentUI.AspNetCore.Components;
using System;
using System.IO;
using System.Runtime.CompilerServices;
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
        private readonly object _lock = new object(); // <-- important

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
                catch { }

                return new AppSettings();
            }
        }

        private void SaveSettings()
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFile, json);
            }
        }

        // Public API -------------------------
        public string GetModelRootPath()
        {
            lock (_lock)
                return _settings.ModelRootPath ?? "";
        }

        public void SetModelRootPath(string path)
        {
            lock (_lock)
            {
                _settings.ModelRootPath = path?.Trim() ?? "";
                SaveSettings();
            }
        }

        public bool ValidateModelRootPath(out string error)
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
