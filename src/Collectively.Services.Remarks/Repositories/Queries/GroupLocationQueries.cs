using System;
using System.Threading.Tasks;
using Collectively.Common.Mongo;
using Collectively.Services.Remarks.Domain;
using Collectively.Common.Extensions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Collectively.Services.Remarks.Repositories.Queries
{
    public static class GroupLocationQueries
    {
        public static IMongoCollection<GroupLocation> GroupLocations(this IMongoDatabase database)
            => database.GetCollection<GroupLocation>();

        public static async Task<GroupLocation> GetAsync(this IMongoCollection<GroupLocation> groupLocations, 
            Guid groupId)
        {
            if (groupId.IsEmpty())
            {
                return null;
            }

            return await groupLocations.AsQueryable().FirstOrDefaultAsync(x => x.GroupId == groupId);
        }

        public static async Task<IEnumerable<GroupLocation>> GetAllWithLocationsAsync(this IMongoCollection<GroupLocation> groupLocations, 
            IEnumerable<string> locations)
        {
            if (locations == null || !locations.Any())
            {
                return Enumerable.Empty<GroupLocation>();
            }

            return await groupLocations
                .AsQueryable()
                .Where(x => locations.Any(l => x.Locations.Contains(l)))
                .ToListAsync();
        }
    }
}