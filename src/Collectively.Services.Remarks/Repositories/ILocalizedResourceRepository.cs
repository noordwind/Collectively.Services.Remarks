using System;
using System.Threading.Tasks;
using  Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;

namespace Collectively.Services.Remarks.Repositories
{
    public interface ILocalizedResourceRepository
    {
        Task<Maybe<LocalizedResource>> GetAsync(Guid id);
        Task<Maybe<LocalizedResource>> GetAsync(string name, string culture);
        Task AddAsync(LocalizedResource resource);
        Task UpdateAsync(LocalizedResource resource);
        Task DeleteAsync(LocalizedResource resource);
    }
}