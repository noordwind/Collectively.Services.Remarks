using System.Threading.Tasks;
using  Collectively.Common.Mongo;
using  Collectively.Common.Types;
using Collectively.Services.Remarks.Queries;
using Collectively.Services.Remarks.Repositories.Queries;
using MongoDB.Driver;
using Tag = Collectively.Services.Remarks.Domain.Tag;

namespace Collectively.Services.Remarks.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly IMongoDatabase _database;

        public TagRepository(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task<Maybe<Tag>> GetAsync(string name)
            => await _database.Tags().GetAsync(name.ToLowerInvariant());

        public async Task<Maybe<PagedResult<Tag>>> BrowseAsync(BrowseTags query)
            => await _database.Tags()
                    .Query(query)
                    .PaginateAsync();

        public async Task AddAsync(Tag tag)
            => await _database.Tags().InsertOneAsync(tag);
    }
}