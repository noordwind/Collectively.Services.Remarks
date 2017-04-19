using System;
using System.Threading.Tasks;
using  Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Queries;

namespace Collectively.Services.Remarks.Repositories
{
    public interface IRemarkRepository
    {
        Task<Maybe<Remark>> GetByIdAsync(Guid id);
        Task<Maybe<PagedResult<Remark>>> BrowseAsync(BrowseRemarks query);
        Task<Maybe<Remark>> GetLatestUserRemarkAsync(string userId);
        Task AddAsync(Remark remark);
        Task UpdateAsync(Remark remark);
        Task UpdateUserNamesAsync(string userId, string name);
        Task DeleteAsync(Remark remark);
    }
}