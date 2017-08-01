using System;
using System.Threading.Tasks;
using Collectively.Common.Mongo;
using Collectively.Services.Remarks.Domain;
using Collectively.Common.Extensions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Collectively.Services.Remarks.Repositories.Queries
{
    public static class GroupQueries
    {
        public static IMongoCollection<Group> Groups(this IMongoDatabase database)
            => database.GetCollection<Group>();

        public static async Task<Group> GetAsync(this IMongoCollection<Group> groups, Guid id)
        {
            if (id.IsEmpty())
                return null;

            return await groups.AsQueryable().FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}