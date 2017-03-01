using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using  Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Queries;

namespace Collectively.Services.Remarks.Repositories
{
    public interface ICategoryRepository
    {
        Task<Maybe<Category>> GetByIdAsync(Guid id);
        Task<Maybe<Category>> GetByNameAsync(string name);
        Task<Maybe<PagedResult<Category>>> BrowseAsync(BrowseCategories query);
        Task AddManyAsync(IEnumerable<Category> remarks);
    }
}