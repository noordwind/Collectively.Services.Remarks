using System;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Queries;

namespace Collectively.Services.Remarks.Repositories
{
    public interface IReportRepository
    {
         Task<bool> ExistsAsync(Guid remarkId, Guid? resourceId, 
            string type, string userId);
         Task<Maybe<PagedResult<Report>>> BrowseAsync(BrowseReports query);
         Task AddAsync(Report report);
    }
}