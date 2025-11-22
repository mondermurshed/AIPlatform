// AIPlatform.Shared/Services/SettingsService.cs
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIPlatform2.Shared.Services
{
    public class AppSettings
    {
        public string ModelRootPath { get; set; } = "";
    }

    public class SettingsService
    {
        private readonly string _settingsFolder;
        private readonly string _settingsFile;
        private AppSettings _settings;

        public SettingsService(string appName = "AIPlatform")
        {
            // Use ApplicationData for cross-platform user-specific storage.
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsFolder = Path.Combine(appData, appName);
            _settingsFile = Path.Combine(_settingsFolder, "settings.json");

            if (!Directory.Exists(_settingsFolder))
                Directory.CreateDirectory(_settingsFolder);

            _settings = LoadSettings();
        }

        private AppSettings LoadSettings()
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
                // ignore parse errors and return defaults
            }
            return new AppSettings();
        }

        private void SaveSettings()
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFile, json);
        }

        // Public API ----------------------------------------------------------------
        public string GetModelRootPath() => _settings.ModelRootPath ?? "";

        public void SetModelRootPath(string path)
        {
            _settings.ModelRootPath = path?.Trim() ?? "";
            SaveSettings();
        }

        public bool ValidateModelRootPath(out string error)
        {
            var p = GetModelRootPath();
            if (string.IsNullOrWhiteSpace(p)) { error = "Model folder not set."; return false; }
            if (!Directory.Exists(p)) { error = "Folder does not exist."; return false; }
            //// optional: check for a couple of required files
            //var hasTextEncoder = File.Exists(Path.Combine(p, "text_encoder", "model.onnx"));
            //var hasUnet = Directory.Exists(Path.Combine(p, "unet")) && Directory.GetFiles(Path.Combine(p, "unet"), "*.onnx").Length > 0;
            //if (!hasTextEncoder || !hasUnet) { error = "Model folder missing required files (text_encoder/unet)."; return false; }
            error = null; return true;
        }
    }
}
