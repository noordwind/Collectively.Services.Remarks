using System;
using System.Linq;
using System.Threading.Tasks;
using Coolector.Common.Mongo;
using Coolector.Common.Types;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Queries;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Coolector.Services.Remarks.Repositories.Queries
{
    public static class RemarkQueries
    {
        public static IMongoCollection<Remark> Remarks(this IMongoDatabase database)
            => database.GetCollection<Remark>();

        public static async Task<Remark> GetByIdAsync(this IMongoCollection<Remark> remarks, Guid id)
        {
            if (id == Guid.Empty)
                return null;

            return await remarks.AsQueryable().FirstOrDefaultAsync(x => x.Id == id);
        }

        public static async Task<PagedResult<Remark>> QueryAsync(this IMongoCollection<Remark> remarks,
            BrowseRemarks query)
        {
            if (query.Page <= 0)
                query.Page = 1;
            if (query.Results <= 0)
                query.Results = 10;

            var filter = FilterDefinition<Remark>.Empty;
            var totalCount = await remarks.CountAsync(filter);
            var totalPages = (int)totalCount / query.Results + 1;

            var result =  await remarks.Find(filter)
                .Skip(query.Results * (query.Page - 1))
                .Limit(query.Results)
                .ToListAsync();

            return PagedResult<Remark>.Create(result, query.Page, query.Results, totalPages, totalCount);
        }
    }
}