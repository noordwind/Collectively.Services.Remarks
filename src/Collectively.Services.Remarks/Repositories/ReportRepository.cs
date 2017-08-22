using System;
using System.Threading.Tasks;
using Collectively.Common.Mongo;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Queries;
using Collectively.Services.Remarks.Repositories.Queries;
using MongoDB.Driver;

namespace Collectively.Services.Remarks.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly IMongoDatabase _database;

        public ReportRepository(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task<bool> ExistsAsync(Guid remarkId, Guid? resourceId, 
            string type, string userId)
            => await _database.Reports()
                    .ExistsAsync(remarkId, resourceId, type, userId);

        public async Task<Maybe<PagedResult<Report>>> BrowseAsync(BrowseReports query)
            => await _database.Reports()
                    .Query(query)
                    .PaginateAsync();

        public async Task AddAsync(Report report)
            => await _database.Reports().InsertOneAsync(report);
    }
}