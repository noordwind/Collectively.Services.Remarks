using System;
using System.Threading.Tasks;
using  Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories.Queries;
using MongoDB.Driver;

namespace Collectively.Services.Remarks.Repositories
{
    public class LocalizedResourceRepository : ILocalizedResourceRepository
    {
        private readonly IMongoDatabase _database;

        public LocalizedResourceRepository(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task<Maybe<LocalizedResource>> GetAsync(Guid id)
            => await _database.LocalizedResources().GetAsync(id);

        public async Task<Maybe<LocalizedResource>> GetAsync(string name, string culture)
            => await _database.LocalizedResources().GetAsync(name, culture);

        public async Task AddAsync(LocalizedResource resource)
            => await _database.LocalizedResources().InsertOneAsync(resource);

        public async Task UpdateAsync(LocalizedResource resource)
            => await _database.LocalizedResources().ReplaceOneAsync(x => x.Id == resource.Id, resource);

        public async Task DeleteAsync(LocalizedResource resource)
            => await _database.LocalizedResources().DeleteOneAsync(x => x.Id == resource.Id);
    }
}