using System;
using System.Threading.Tasks;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Queries;

namespace Collectively.Services.Remarks.Services
{
    public interface IReportService
    {
        Task<Maybe<PagedResult<Report>>> BrowseAsync(BrowseReports query);
        Task AddAsync(Guid remarkId, Guid? resourceId, string type, string userId);
    }
}