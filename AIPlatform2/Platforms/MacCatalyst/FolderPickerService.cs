#if MACCATALYST
using System;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using AIPlatform2.Shared.Services;
using Microsoft.Maui.ApplicationModel;

namespace AIPlatform2.Platforms.MacCatalyst
{
    public class FolderPickerService : IFolderPickerService
    {
        public Task<string?> PickFolderAsync()
        {
            var tcs = new TaskCompletionSource<string?>();

            // Run UI code on main thread
            NSRunLoop.Main.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // UTI for folders
                    var utis = new string[] { "public.folder" };

                    var picker = new UIDocumentPickerViewController(utis, UIDocumentPickerMode.Open)
                    {
                        AllowsMultipleSelection = false,
                    };

                    picker.DidPickDocument += (sender, e) =>
                    {
                        try
                        {
                            NSUrl? nsUrl = null;

                            // Try to read Urls[] first (some SDKs expose this)
                            var urlsProp = e?.GetType().GetProperty("Urls");
                            if (urlsProp != null)
                            {
                                var arr = urlsProp.GetValue(e) as NSUrl[];
                                if (arr != null && arr.Length > 0)
                                    nsUrl = arr[0];
                            }

                            // Fallback to single Url property (some SDKs expose Url)
                            if (nsUrl == null)
                            {
                                var urlProp = e?.GetType().GetProperty("Url");
                                if (urlProp != null)
                                    nsUrl = urlProp.GetValue(e) as NSUrl;
                            }

                            // Final fallback: some event args have "Document" or "PickedUrl" styles - try common names
                            if (nsUrl == null)
                            {
                                var fallback = e?.GetType().GetProperty("Document")?.GetValue(e) as NSUrl
                                           ?? e?.GetType().GetProperty("PickedUrl")?.GetValue(e) as NSUrl;
                                nsUrl = fallback;
                            }

                            if (nsUrl != null)
                            {
                                // Optionally start security-scoped access if needed
                                try
                                {
                                    nsUrl.StartAccessingSecurityScopedResource();
                                }
                                catch { /* ignore if not allowed */ }

                                var path = nsUrl.Path;
                                tcs.TrySetResult(path);
                            }
                            else
                            {
                                tcs.TrySetResult(null);
                            }
                        }
                        catch
                        {
                            tcs.TrySetResult(null);
                        }
                        finally
                        {
                            // dismiss picker: the system usually dismisses itself, but ensure full cleanup
                            try
                            {
                                if (sender is UIDocumentPickerViewController pc)
                                    pc.DismissViewController(true, null);
                            }
                            catch { }
                        }
                    };

                    picker.WasCancelled += (sender, args) =>
                    {
                        tcs.TrySetResult(null);
                    };

                    // Present from topmost view controller
                    var root = UIApplication.SharedApplication.KeyWindow?.RootViewController
                               ?? UIApplication.SharedApplication.Windows?.FirstOrDefault()?.RootViewController;

                    UIViewController? top = root;
                    while (top?.PresentedViewController != null)
                        top = top.PresentedViewController;

                    if (top == null)
                    {
                        tcs.TrySetResult(null);
                        return;
                    }

                    top.PresentViewController(picker, true, null);
                }
                catch
                {
                    tcs.TrySetResult(null);
                }
            });

            return tcs.Task;
        }
    }
}
#endif
