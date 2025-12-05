using AIPlatform2.Services;
using AIPlatform2.Shared.Services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AIPlatform2
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddFluentUIComponents();
            
            // App services (singletons)
            builder.Services.AddSingleton<InferenceService>();
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            // Platform folder picker registration
#if WINDOWS
            builder.Services.AddSingleton<IFolderPickerService, AIPlatform2.Platforms.Windows.FolderPickerService>();
#elif MACCATALYST
            builder.Services.AddSingleton<IFolderPickerService, AIPlatform2.Platforms.MacCatalyst.FolderPickerService>();
#else
            builder.Services.AddSingleton<IFolderPickerService, AIPlatform2.Services.NullFolderPickerService>();
#endif

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
