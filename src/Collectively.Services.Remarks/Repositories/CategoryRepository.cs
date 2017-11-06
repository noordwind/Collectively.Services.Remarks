using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using  Collectively.Common.Types;
using  Collectively.Common.Mongo;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Queries;
using Collectively.Services.Remarks.Repositories.Queries;
using MongoDB.Driver;

namespace Collectively.Services.Remarks.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly IMongoDatabase _database;

        public CategoryRepository(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task<Maybe<Category>> GetByIdAsync(Guid id)
            => await _database.Categories().GetByIdAsync(id);

        public async Task<Maybe<Category>> GetByNameAsync(string name)
            => await _database.Categories().GetByNameAsync(name);

        public async Task<Maybe<PagedResult<Category>>> BrowseAsync(BrowseCategories query)
            => await _database.Categories()
                    .Query(query)
                    .PaginateAsync(query);

        public async Task AddManyAsync(IEnumerable<Category> remarks)
            => await _database.Categories().InsertManyAsync(remarks);
    }
}