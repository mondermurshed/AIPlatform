// AIPlatform.Shared/Services/InferenceService.cs
// --------------------------------------------
// This service is responsible for producing images from a text prompt.
// It is written to be DI-friendly (can be registered as a singleton or scoped).
// Two modes are supported:
//  - Fallback simple renderer (ImageSharp) — works without any ML model.
//  - Placeholder for ONNX inference (GenerateImageWithOnnxAsync) — implement when you have model files.
// The service exposes a configurable output folder, cancellation support, and safe disposal for future ONNX sessions.

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AIPlatform2.Shared.Services
{
    public class InferenceService : IDisposable
    {
        // The folder where generated images are saved. Configurable so you can change it in production.
        private readonly string _outputFolder;

        // If you later use ONNX sessions, hold them here and dispose in Dispose().
        // e.g. private InferenceSession _onnxSession;
        // For now left null until you ask to implement ONNX inference.

        public InferenceService(string outputFolder = null)
        {
            // Determine a reasonable default output folder (Pictures/AIGenerated) if none provided.
            var defaultPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            _outputFolder = string.IsNullOrWhiteSpace(outputFolder)
                ? Path.Combine(defaultPictures, "AIGenerated")
                : outputFolder;

            // Ensure folder exists
            if (!Directory.Exists(_outputFolder))
                Directory.CreateDirectory(_outputFolder);
        }

        // Public accessor so UI can know where images land.
        public string OutputFolder => _outputFolder;

        // ----------------------------
        // Public high-level API method
        // ----------------------------
        // Accepts a prompt and a cancellation token. Returns the saved file path on success.
        // By default this calls the simple placeholder renderer (ImageSharp). Later we will call ONNX here.
        public async Task<string> GenerateImageFromPromptAsync(string prompt, CancellationToken cancellationToken = default)
        {
            // Small guard: normalize prompt
            var effectivePrompt = string.IsNullOrWhiteSpace(prompt) ? "(empty prompt)" : prompt.Trim();

            // If you already implemented ONNX inference and want to use it, call GenerateImageWithOnnxAsync here.
            // For now we call the simple renderer so you have an immediate working experience.
            return await GenerateSimplePreviewImageAsync(effectivePrompt, cancellationToken);
        }

        // -------------------------------------------------
        // Simple image generator (placeholder, ImageSharp)
        // -------------------------------------------------
        // This creates a 512x512 image, paints a background, and draws the prompt text
        // on a semi-transparent rectangle near the bottom. Useful as an immediate working demo.
        private async Task<string> GenerateSimplePreviewImageAsync(string prompt, CancellationToken cancellationToken)
        {
            // Assemble a unique filename
            var fileName = $"gen_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var fullPath = Path.Combine(_outputFolder, fileName);

            // Image dimensions (you can make these configurable)
            const int W = 512, H = 512;

            // ImageSharp operations are synchronous for pixel work; we wrap save in async.
            using (var image = new Image<Rgba32>(W, H))
            {
                // Fill background
                image.Mutate(ctx => ctx.Fill(Color.DarkSlateBlue));

                // Choose a font. This block tries to pick a common font, with fallbacks.
                Font font;
                try
                {
                    font = SystemFonts.CreateFont("Arial", 18, FontStyle.Bold);
                }
                catch
                {
                    // Fallback to first available system font (safe)
                    var enumerator = SystemFonts.Families.GetEnumerator();
                    if (enumerator.MoveNext())
                        font = enumerator.Current.CreateFont(18, FontStyle.Bold);
                    else
                        font = SystemFonts.CreateFont("Times New Roman", 18, FontStyle.Bold);
                }

                // Prepare text lines via a very rough wrap (not perfect but quick).
                int approxCharsPerLine = 36;
                var lines = WrapTextRough(prompt, approxCharsPerLine);

                // Compute rectangle area to draw background box for text
                int padding = 12;
                float lineHeight = font.Size + 6;
                float rectHeight = lines.Length * lineHeight + padding * 2;
                float rectY = H - rectHeight - 20; // margin from bottom
                if (rectY < 10) rectY = 10;

                image.Mutate(ctx =>
                {
                    // semi-transparent box behind text
                    ctx.Fill(Color.Black.WithAlpha(0.55f), new RectangleF(12, rectY, W - 24, rectHeight));

                    // draw each line
                    float lineX = 24;
                    float lineY = rectY + padding;
                    foreach (var line in lines)
                    {
                        ctx.DrawText(line, font, Color.White, new PointF(lineX, lineY));
                        lineY += lineHeight;
                    }
                });

                // Save out to PNG asynchronously
                // Note: ImageSharp supports SaveAsPngAsync
                await image.SaveAsPngAsync(fullPath, cancellationToken);
            }

            return fullPath;
        }

        // -------------------------------------------------
        // PLACEHOLDER: ONNX inference implementation
        // -------------------------------------------------
        // When you want real image generation (Stable Diffusion / Flux), implement this method.
        // Comments below describe what is needed at a high level:
        //
        // 1. NuGet packages you'll likely use:
        //    - Microsoft.ML.OnnxRuntime (or Microsoft.ML.OnnxRuntime.Gpu for GPU)
        //    - Tokenizers / HuggingFace tokenizer port (or implement your own tokenizer)
        //    - (Optionally) any helper libraries for schedulers, VAE decoding, etc.
        //
        // 2. Required model files (example for SD-like approach):
        //    - text encoder / tokenizer (to convert prompt -> tokens)
        //    - UNet ONNX model (main denoising model)
        //    - VAE decoder ONNX model (to convert latents -> RGB)
        //    - Scheduler config or code to run inference steps
        //
        // 3. Steps inside this method:
        //    - Tokenize prompt -> embeddings (or call text encoder ONNX)
        //    - Prepare latent initial noise tensor
        //    - Run denoising loop: repeatedly run ONNX UNet + scheduler steps
        //    - After denoising, call VAE decode to get RGB image
        //    - Convert output tensor -> image bytes -> save to fullPath
        //
        // This is non-trivial but I can implement it for you step-by-step once you confirm:
        //    (A) which model repo/collection you want (Stable Diffusion v1.x, v2, or another ONNX export),
        //    (B) whether you want CPU or GPU inference,
        //    (C) where you will place the ONNX files (path).
        //
        // For now this method throws NotImplementedException so the code is explicit.
        private Task<string> GenerateImageWithOnnxAsync(string prompt, CancellationToken cancellationToken)
        {
            // TODO: Implement ONNX inference pipeline here once you provide model files and preference (CPU/GPU).
            throw new NotImplementedException("ONNX inference is not implemented yet. Call GenerateSimplePreviewImageAsync for a quick demo, or ask me to implement ONNX next.");
        }

        // -------------------------------------------------
        // Very simple whitespace-based wrapper (used by ImageSharp placeholder)
        // -------------------------------------------------
        private static string[] WrapTextRough(string text, int approxCharsPerLine)
        {
            if (string.IsNullOrEmpty(text))
                return new[] { "" };

            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var lines = new System.Collections.Generic.List<string>();
            var current = "";

            foreach (var w in words)
            {
                if ((current.Length + 1 + w.Length) <= approxCharsPerLine || current.Length == 0)
                {
                    current = string.IsNullOrEmpty(current) ? w : current + " " + w;
                }
                else
                {
                    lines.Add(current);
                    current = w;
                }
            }

            if (!string.IsNullOrEmpty(current))
                lines.Add(current);

            return lines.ToArray();
        }

        // Dispose pattern: if you later create ONNX sessions, dispose them here
        public void Dispose()
        {
            // e.g. _onnxSession?.Dispose();
        }
    }
}
