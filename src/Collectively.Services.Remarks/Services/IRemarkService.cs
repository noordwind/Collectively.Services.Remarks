using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Queries;

namespace Collectively.Services.Remarks.Services
{
    public interface IRemarkService
    {
        Task<Maybe<Remark>> GetAsync(Guid id);
        Task<Maybe<PagedResult<Remark>>> BrowseAsync(BrowseRemarks query);
        Task<Maybe<PagedResult<Category>>> BrowseCategoriesAsync(BrowseCategories query);
        Task<Maybe<PagedResult<Tag>>> BrowseTagsAsync(BrowseTags query);
        Task ValidateEditorAccessOrFailAsync(Guid remarkId, string userId);
        Task CreateAsync(Guid id, string userId, string category,
            Location location, string description = null, IEnumerable<string> tags = null,
            Guid? groupId = null);
        Task UpdateUserNamesAsync(string userId, string name);
        Task DeleteAsync(Guid id);
        Task SubmitVoteAsync(Guid remarkId, string userId, bool positive, DateTime createdAt);
        Task DeleteVoteAsync(Guid remarkId, string userId);
        Task AddFavoriteRemarkAsync(Guid remarkId, string userId);
        Task DeleteFavoriteRemarkAsync(Guid remarkId, string userId);
    }
}