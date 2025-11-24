#if !WINDOWS && !MACCATALYST
using System.Threading.Tasks;

namespace AIPlatform2.Shared.Components   // <-- same namespace as above
{
    public partial class PathInputBase
    {
        protected partial Task<string?> PlatformPickFolderAsync()
        {
            // No native picker for this target (or not compiling for desktop) — return null.
            return Task.FromResult<string?>(null);
        }
    }
}
#endif
