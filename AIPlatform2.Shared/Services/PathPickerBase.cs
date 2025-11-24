#if WINDOWS || MACCATALYST
using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Storage;

public partial class PathInputBase
{
    partial async Task<string?> PlatformPickFolderAsync()
    {
        try
        {
            // show native folder dialog
            var result = await FolderPicker.Default.PickAsync();

            if (result == null) return null; // safety

            if (result.IsSuccessful)
            {
                // return chosen folder full path (what you need)
                return result.Folder?.Path;
            }

            // canceled or failed
            return null;
        }
        catch (Exception)
        {
            // swallow; you can log errors here during debugging
            return null;
        }
    }
}
#endif
