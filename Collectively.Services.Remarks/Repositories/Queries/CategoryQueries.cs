using System;
using System.Threading.Tasks;
using  Collectively.Common.Extensions;
using  Collectively.Common.Mongo;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Queries;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Collectively.Services.Remarks.Repositories.Queries
{
    public static class CategoryQueries
    {
        public static IMongoCollection<Category> Categories(this IMongoDatabase database)
            => database.GetCollection<Category>();

        public static async Task<Category> GetByIdAsync(this IMongoCollection<Category> categories, Guid id)
        {
            if (id == Guid.Empty)
                return null;

            return await categories.AsQueryable().FirstOrDefaultAsync(x => x.Id == id);
        }

        public static async Task<Category> GetByNameAsync(this IMongoCollection<Category> categories, string name)
        {
            if (name.Empty())
                return null;

            return await categories.AsQueryable().FirstOrDefaultAsync(x => x.Name == name);
        }

        public static IMongoQueryable<Category> Query(this IMongoCollection<Category> categories,
            BrowseCategories query)
        {
            var values = categories.AsQueryable();

            return values;
        }
    }
}