// AIPlatform.Shared/Services/IFolderPickerService.cs
using System.Threading.Tasks;

namespace AIPlatform2.Shared.Services
{
    public interface IFolderPickerService
    {
        /// <summary>
        /// Opens a native folder picker and returns the chosen folder path, or null if cancelled.
        /// Implementations are platform-specific.
        /// </summary>
        Task<string?> PickFolderAsync();
    }
}
