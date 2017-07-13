using System.Threading.Tasks;
using  Collectively.Common.Types;

namespace Collectively.Services.Remarks.Services
{
    public interface ILocalizedResourceService
    {
        Task<Maybe<string>> TranslateAsync(string name, string culture, params object[] args);
    }
}