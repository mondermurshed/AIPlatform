// Services/ModelManager.cs
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AIPlatform2.Shared.Services
{
    /// <summary>
    /// ModelManager handles downloading/extracting model zip archives into the model root configured
    /// by SettingsService. This class DOES NOT create or pick a default model root; it expects the
    /// SettingsService to already contain a valid path. If the configured path does not exist, methods
    /// will throw InvalidOperationException (fail fast).
    /// </summary>
    public class ModelManager : IDisposable
    {
        private readonly SettingsService _settings;
        private readonly HttpClient _http;
        private bool _disposed;

        public ModelManager(SettingsService settings, HttpClient httpClient)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _http = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// The model root path as returned from SettingsService (no defaults).
        /// </summary>
        public string ModelRoot => _settings.GetModelRootPath();

        /// <summary>
        /// Returns the absolute folder path for the given model key inside the configured model root.
        /// Example: modelKey = "stable-diffusion-v1-5-onnx" -> "%MODELROOT%/stable-diffusion-v1-5-onnx"
        /// </summary>
        public string GetModelFolder(string modelKey)
        {
            if (string.IsNullOrWhiteSpace(modelKey)) throw new ArgumentNullException(nameof(modelKey));
            EnsureModelRootExists();
            return Path.Combine(ModelRoot, modelKey);
        }

        /// <summary>
        /// Returns true if the model folder exists and contains files.
        /// Throws if model root is not configured or does not exist.
        /// </summary>
        public bool IsModelInstalled(string modelKey)
        {
            if (string.IsNullOrWhiteSpace(modelKey)) return false;
            EnsureModelRootExists();
            var folder = GetModelFolder(modelKey);
            return Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any();
        }

        /// <summary>
        /// Download a zip archive from zipUrl and extract to ModelRoot/modelKey.
        /// Progress = 0..100. Supports CancellationToken.
        /// Throws InvalidOperationException if the SettingsService model path does not exist.
        /// </summary>
        public async Task DownloadAndExtractZipAsync(string zipUrl, string modelKey, IProgress<int>? progress = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(zipUrl)) throw new ArgumentNullException(nameof(zipUrl));
            if (string.IsNullOrWhiteSpace(modelKey)) throw new ArgumentNullException(nameof(modelKey));

            // Very important: require that the USER set a real path in SettingsService.
            EnsureModelRootExists();

            var tmpFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");

            try
            {
                // Download with streaming to avoid loading entire file into memory
                using (var resp = await _http.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
                {
                    resp.EnsureSuccessStatusCode();

                    var total = resp.Content.Headers.ContentLength ?? -1L;
                    using var fs = new FileStream(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

                    var buffer = new byte[81920];
                    long read = 0;
                    int bytes;
                    while ((bytes = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false)) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, bytes), ct).ConfigureAwait(false);
                        read += bytes;

                        if (total > 0 && progress != null)
                        {
                            var pct = (int)((read * 100L) / total);
                            progress.Report(Math.Min(100, Math.Max(0, pct)));
                        }
                    }
                }

                // Extract to destination inside configured model root
                var dest = GetModelFolder(modelKey);

                // Remove previous install if present (best-effort)
                if (Directory.Exists(dest))
                {
                    try { Directory.Delete(dest, true); }
                    catch { /* ignore; extraction may still succeed */ }
                }

                // Extract archive (ZipFile throws if archive invalid)
                ZipFile.ExtractToDirectory(tmpFile, dest);

                // Sanity check: ensure extraction produced files
                if (!Directory.EnumerateFileSystemEntries(dest).Any())
                {
                    throw new InvalidOperationException("Extraction produced an empty folder. The downloaded archive may be invalid.");
                }
            }
            finally
            {
                // Always attempt to delete temp file (best-effort)
                try { if (File.Exists(tmpFile)) File.Delete(tmpFile); } catch { }
            }
        }

        /// <summary>
        /// Lists top-level model folder names in the configured model root.
        /// Throws if model root does not exist.
        /// </summary>
        public string[] ListInstalledModels()
        {
            EnsureModelRootExists();
            try
            {
                return Directory.GetDirectories(ModelRoot).Select(Path.GetFileName).ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Ensures that a model root path was set in SettingsService and that the directory exists.
        /// This method purposefully does NOT create directories or default paths — it throws instead.
        /// </summary>
        private void EnsureModelRootExists()
        {
            var path = _settings.GetModelRootPath();
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                throw new InvalidOperationException("Model root path is not configured or does not exist. Please set the model folder path in settings before using model operations.");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _http?.Dispose();
            _disposed = true;
        }
    }
}
