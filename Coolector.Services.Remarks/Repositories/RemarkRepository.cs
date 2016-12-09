using System;
using System.Threading.Tasks;
using Coolector.Common.Types;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Queries;
using Coolector.Services.Remarks.Repositories.Queries;
using MongoDB.Driver;

namespace Coolector.Services.Remarks.Repositories
{
    public class RemarkRepository : IRemarkRepository
    {
        private readonly IMongoDatabase _database;

        public RemarkRepository(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task<Maybe<Remark>> GetByIdAsync(Guid id)
            => await _database.Remarks().GetByIdAsync(id);

        public async Task<Maybe<PagedResult<Remark>>> BrowseAsync(BrowseRemarks query)
            => await _database.Remarks().QueryAsync(query);

        public async Task AddAsync(Remark remark)
            => await _database.Remarks().InsertOneAsync(remark);

        public async Task UpdateAsync(Remark remark)
            => await _database.Remarks().ReplaceOneAsync(x => x.Id == remark.Id, remark);

        public async Task UpdateUserNamesAsync(string userId, string name)
        {
            var updateAuthor = Builders<Remark>.Update.Set("author.name", name);
            await _database.Remarks().UpdateManyAsync(x => x.Author.UserId == userId, updateAuthor);

            var updateResolver = Builders<Remark>.Update.Set("resolver.name", name);
            await _database.Remarks().UpdateManyAsync(x => x.Resolver.UserId == userId, updateResolver);
        }

        public async Task DeleteAsync(Remark remark)
            => await _database.Remarks().DeleteOneAsync(x => x.Id == remark.Id);
    }
}