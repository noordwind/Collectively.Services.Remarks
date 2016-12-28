using System.Threading.Tasks;
using Coolector.Common.Mongo;
using Coolector.Common.Types;
using Coolector.Services.Remarks.Queries;
using Coolector.Services.Remarks.Repositories.Queries;
using MongoDB.Driver;
using Tag = Coolector.Services.Remarks.Domain.Tag;

namespace Coolector.Services.Remarks.Repositories
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