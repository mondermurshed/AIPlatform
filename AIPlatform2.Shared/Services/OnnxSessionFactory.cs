using Microsoft.ML.OnnxRuntime;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace AIPlatform.Shared.Services
{
    public static class OnnxSessionFactory
    {
        // Attempt providers in this priority order.
        // You can tweak ordering: CUDA -> TensorRT -> DirectML -> CoreML -> CPU
        private static readonly string[] ProviderPriority = new[]
        {
        "CUDA",
        "DirectML",
        "CoreML",
        "CPU"
    };

        // Create a session for the provided onnx model path, automatically selecting the best provider available.
        public static InferenceSession CreateSession(string modelPath, SessionOptions baseOptions = null)
        {
            if (string.IsNullOrEmpty(modelPath))
                throw new ArgumentNullException(nameof(modelPath));

            // Create session options or use provided one
            var options = baseOptions ?? new SessionOptions();

            // We'll try to append providers in priority order. If append fails, we log and continue.
            var triedProviders = new List<string>();
            Exception lastError = null;
            InferenceSession session = null;

            foreach (var p in ProviderPriority)
            {
                try
                {
                    // Each provider append uses a specific API on SessionOptions.
                    // We put them in try/catch to handle missing provider libs gracefully.

                    if (p == "CUDA")
                    {
                        // This call is available only if GPU provider native libs are present.
                        // If Microsoft.ML.OnnxRuntime.Gpu is not installed, this will throw at runtime.
                        options.AppendExecutionProvider_CUDA(); // provided by the GPU package
                        triedProviders.Add("CUDA");
                    }
                    else if (p == "DirectML")
                    {
                        // Use DirectML on Windows. Requires Microsoft.ML.OnnxRuntime.DirectML package and DirectX drivers.
                        options.AppendExecutionProvider_DML(); // method name in some builds, or AppendExecutionProvider_DirectML
                        triedProviders.Add("DirectML");
                    }
                    else if (p == "CoreML")
                    {
                        // CoreML EP (macOS / iOS). Requires CoreML EP package / build of ORT that includes CoreML.
                        options.AppendExecutionProvider_CoreML();
                        triedProviders.Add("CoreML");
                    }
                    else if (p == "CPU")
                    {
                        options.AppendExecutionProvider_CPU();
                        // Always available - CPU is default provider so we can just rely on it.
                        triedProviders.Add("CPU");
                    }

                    // Try creating the session with the current options.
                    // If the append call didn't throw but session creation fails, we'll handle it below.
                    session = new InferenceSession(modelPath, options);
                    // Success — record which provider was appended (if any) and return.
                    Console.WriteLine($"ONNX Runtime session created using providers: {string.Join(',', triedProviders)}");
                    return session;
                }
                catch (Exception ex)
                {
                    // Append or session creation failed for this provider — remember the error and move on.
                    lastError = ex;
                    Console.WriteLine($"Provider '{p}' not available or failed: {ex.Message}");
                    // Reset options for next try — create new SessionOptions instance to avoid duplicates
                    options = baseOptions ?? new SessionOptions();
                    triedProviders.Clear();
                }
            }

            // If we get here, none of the preferred providers worked; throw the last error or create CPU session as last resort.
            try
            {
                // Final fallback: CPU-only session
                Console.WriteLine("Falling back to CPUExecutionProvider.");
                session = new InferenceSession(modelPath); // default CPU provider
                return session;
            }
            catch (Exception finalEx)
            {
                // If CPU session creation also fails, throw with context
                throw new Exception("Failed to create any ONNX session (providers tried). See inner exception(s).", finalEx);
            }
        }

        // Small helpers to detect OS (if you want to adjust priority by OS).
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    }
}
