using System.Collections.Generic;
using System.Threading.Tasks;
using Coolector.Common.Mongo;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Repositories.Queries;
using MongoDB.Driver;
using Tag = Coolector.Services.Remarks.Domain.Tag;

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

            await _database.Categories().InsertOneAsync(new Category("defect"));
            await _database.Categories().InsertOneAsync(new Category("issue"));
            await _database.Categories().InsertOneAsync(new Category("suggestion"));
            await _database.Categories().InsertOneAsync(new Category("praise"));

            await _database.LocalizedResources().InsertOneAsync(new LocalizedResource("facebook:new_remark", "en-gb",
                "I've just sent a new remark using Coolector. You can see it here: {0}"));
            await _database.LocalizedResources().InsertOneAsync(new LocalizedResource("facebook:new_remark", "pl-pl",
                "Nowe zgłoszenie zostało przeze mnie dodane za pomocą Coolector. Możesz je zobaczyć tutaj: {0}"));
            
            var tags = new List<Tag>
            {
                new Tag("junk"), new Tag("small"), new Tag("medium"),
                new Tag("big"), new Tag("crash"), new Tag("stink"),
                new Tag("dirty"), new Tag("glass"), new Tag("plastic")
            };
            await _database.Tags().InsertManyAsync(tags);
        }
    }
}