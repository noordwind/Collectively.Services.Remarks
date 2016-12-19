using System;
using System.Threading.Tasks;
using Coolector.Common.Types;
using Coolector.Services.Remarks.Domain;

namespace Coolector.Services.Remarks.Repositories
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