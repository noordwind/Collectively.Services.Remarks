using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories.Queries;
using MongoDB.Driver;

namespace Collectively.Services.Remarks.Repositories
{
    public class GroupLocationRepository : IGroupLocationRepository
    {
        private readonly IMongoDatabase _database;

        public GroupLocationRepository(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task<Maybe<GroupLocation>> GetAsync(Guid groupId)
            => await _database.GroupLocations().GetAsync(groupId);

        public async Task AddAsync(GroupLocation groupLocality)
            => await _database.GroupLocations().InsertOneAsync(groupLocality);

        public async Task<IEnumerable<GroupLocation>> GetAllWithLocationsAsync(IEnumerable<string> locations)
            => await _database.GroupLocations().GetAllWithLocationsAsync(locations);
    }
}