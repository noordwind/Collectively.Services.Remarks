using System.Threading.Tasks;
using Coolector.Common.Mongo;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Repositories.Queries;
using MongoDB.Driver;

namespace Coolector.Services.Remarks.Framework
{
    public class DatabaseSeeder : IDatabaseSeeder
    {
        private readonly IMongoDatabase _database;

        public DatabaseSeeder(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task SeedAsync()
        {
            if (await _database.Remarks().AsQueryable().AnyAsync() == false)
            {
                var index = new IndexKeysDefinitionBuilder<Remark>().Geo2DSphere(x => x.Location);
                await _database.Remarks().Indexes.CreateOneAsync(index);
            }

            if (await _database.Categories().AsQueryable().AnyAsync())
                return;

            await _database.Categories().InsertOneAsync(new Category("litter"));
            await _database.Categories().InsertOneAsync(new Category("damages"));
            await _database.Categories().InsertOneAsync(new Category("accidents"));
        }
    }
}