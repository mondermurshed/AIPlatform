using System.Threading.Tasks;
using AIPlatform2.Shared.Services;

namespace AIPlatform2.Services
{
    public class NullFolderPickerService : IFolderPickerService
    {
        public Task<string?> PickFolderAsync() => Task.FromResult<string?>(null);
    }
}
