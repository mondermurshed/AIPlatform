using AIPlatform2.Shared.Services;
using AIPlatform2.Services;
using AIPlatform2.Shared.Services;
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
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add device-specific services used by the AIPlatform2.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddFluentUIComponents();
            builder.Services.AddSingleton<InferenceService>();
            builder.Services.AddSingleton<SettingsService>();
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
