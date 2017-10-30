using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collectively.Common.Mongo;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories.Queries;
using MongoDB.Driver;

namespace Collectively.Services.Remarks.Repositories
{
    public class GroupRepository : IGroupRepository
    {
        private readonly IMongoDatabase _database;

        public GroupRepository(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task<Maybe<Group>> GetAsync(Guid id)
        => await _database.Groups().GetAsync(id);

        public async Task AddAsync(Group group)
        => await _database.Groups().InsertOneAsync(group);

        public async Task UpdateAsync(Group group)
        => await _database.Groups().ReplaceOneAsync(x => x.Id == group.Id, group);

        public async Task DeleteAsync(Guid id)
        => await _database.Groups().DeleteOneAsync(x => x.Id == id);
    }
}