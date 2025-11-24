using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using AIPlatform2.Shared.Services; // adjust to your real SettingsService namespace

namespace AIPlatform2.Shared.Components   // <-- REPLACE this with your project's namespace
{
    public partial class PathInputBase : ComponentBase
    {
        [Inject] protected SettingsService SettingsService { get; set; } = null!;

        // visible value in the UI
        protected string? Path { get; set; }

        // small validation message shown to user
        protected string? ValidationError { get; set; }

        // Declaration: must include an accessibility modifier for non-void partial methods
        protected partial Task<string?> PlatformPickFolderAsync();

        protected override void OnInitialized()
        {
            Path = SettingsService.GetModelRootPath();
            ValidateAndSetMessage();
        }

        protected async Task OpenFolderDialog()
        {
            var picked = await PlatformPickFolderAsync();

            if (!string.IsNullOrEmpty(picked))
            {
                // Save to your SettingsService (this writes JSON per your implementation)
                SettingsService.SetModelRootPath(picked);

                // update the local Path so UI shows it immediately
                Path = picked;
            }

            ValidateAndSetMessage();
            StateHasChanged();
        }

        protected Task ClearPath()
        {
            SettingsService.SetModelRootPath(""); // save empty in JSON
            Path = "";
            ValidateAndSetMessage();
            StateHasChanged();
            return Task.CompletedTask;
        }

        void ValidateAndSetMessage()
        {
            if (SettingsService.ValidateModelRootPath(out var err))
            {
                ValidationError = null;
            }
            else
            {
                ValidationError = err;
            }
        }
    }
}
