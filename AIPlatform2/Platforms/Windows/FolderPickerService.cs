#if WINDOWS
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using WinRT;
using AIPlatform2.Shared.Services;
using Microsoft.Maui.ApplicationModel;


namespace AIPlatform2.Platforms.Windows
{
    public class FolderPickerService : IFolderPickerService
    {
        public async Task<string?> PickFolderAsync()
        {
            try
            {
                string? pickedPath = null;

                // Ensure UI thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var picker = new FolderPicker();

                    try
                    {
                        // Try to get MAUI window and its native platform view
                        var mauiWindow = Microsoft.Maui.Controls.Application.Current?.Windows?.FirstOrDefault();
                        var platformView = mauiWindow?.Handler?.PlatformView;

                        if (platformView is Microsoft.UI.Xaml.Window nativeWindow)
                        {
                            // We have the native WinUI window - get HWND and initialize picker
                            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
                            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                        }
                        else
                        {
                            // Attempt a best-effort fallback: try to use WinRT Window.Current if available
                            try
                            {
                                var currentWindow = Microsoft.UI.Xaml.Window.Current;
                                if (currentWindow != null)
                                {
                                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(currentWindow);
                                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                                }
                            }
                            catch
                            {
                                // ignore - picker might still work without explicit handle
                            }
                        }
                    }
                    catch
                    {
                        // ignore init handle issues; continue to show picker
                    }

                    StorageFolder? folder = await picker.PickSingleFolderAsync();
                    pickedPath = folder?.Path;
                });

                return pickedPath;
            }
            catch
            {
                return null;
            }
        }
    }
}
#endif
