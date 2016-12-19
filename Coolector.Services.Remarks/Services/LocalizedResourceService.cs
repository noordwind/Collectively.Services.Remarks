using System.Threading.Tasks;
using Coolector.Common.Types;
using Coolector.Services.Remarks.Repositories;
using NLog;

namespace Coolector.Services.Remarks.Services
{
    public class LocalizedResourceService : ILocalizedResourceService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private ILocalizedResourceRepository _localizedResourceRepository;

        public LocalizedResourceService(ILocalizedResourceRepository localizedResourceRepository)
        {
            _localizedResourceRepository = localizedResourceRepository;
        }

        public async Task<Maybe<string>> TranslateAsync(string name, string culture, params object[] args)
        {
            var resource = await _localizedResourceRepository.GetAsync(name.ToLowerInvariant(), culture.ToLowerInvariant());
            if (resource.HasNoValue)
            {
                Logger.Warn($"Localized resource for name: '{name}' and culture: '{culture}' was not found.");
                
                return new Maybe<string>();
            }

            return resource.Value.GetTranslatedText(args);
        }
    }
}