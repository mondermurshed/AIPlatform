#if WINDOWS || MACCATALYST
using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Storage;

namespace AIPlatform2.Shared.Components   // <-- same namespace as above
{
    public partial class PathInputBase
    {
        protected partial async Task<string?> PlatformPickFolderAsync()
        {
            try
            {
                var result = await FolderPicker.Default.PickAsync();

                if (result == null) return null;
                if (result.IsSuccessful) return result.Folder?.Path;
                return null;
            }
            catch (Exception ex)
            {
                // during development you may want to log ex.Message; for production swallow or log via ILogger
                return null;
            }
        }
    }
}
#endif
