using System.Threading.Tasks;
using Coolector.Common.Types;

namespace Coolector.Services.Remarks.Services
{
    public interface ILocalizedResourceService
    {
        Task<Maybe<string>> TranslateAsync(string name, string culture, params object[] args);
    }
}