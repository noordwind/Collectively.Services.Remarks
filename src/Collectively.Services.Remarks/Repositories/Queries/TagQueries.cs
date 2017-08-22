using System.Threading.Tasks;
using Collectively.Common.Extensions;
using Collectively.Common.Mongo;
using Collectively.Services.Remarks.Queries;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Tag = Collectively.Services.Remarks.Domain.Tag;

namespace Collectively.Services.Remarks.Repositories.Queries
{
    public static class TagQueries
    {
        public static IMongoCollection<Tag> Tags(this IMongoDatabase database)
            => database.GetCollection<Tag>();

        public static async Task<Tag> GetAsync(this IMongoCollection<Tag> tags, string name)
        {
            if (name.Empty())
                return null;

            return await tags.AsQueryable().FirstOrDefaultAsync(x => x.Name == name);
        }

        public static IMongoQueryable<Tag> Query(this IMongoCollection<Tag> tags,
            BrowseTags query)
        {
            var values = tags.AsQueryable();

            return values;
        }
    }
}