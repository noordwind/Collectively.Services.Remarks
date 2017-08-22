using System;
using System.Threading.Tasks;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;
using Collectively.Common.Types;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Extensions;
using Collectively.Services.Remarks.Queries;
using Collectively.Services.Remarks.Repositories;

namespace Collectively.Services.Remarks.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IRemarkRepository _remarkRepository;

        public ReportService(IReportRepository reportRepository, 
            IRemarkRepository remarkRepository)
        {
            _reportRepository = reportRepository;
            _remarkRepository = remarkRepository;
        }

        public async Task<Maybe<PagedResult<Report>>> BrowseAsync(BrowseReports query)
            => await _reportRepository.BrowseAsync(query);

        public async Task AddAsync(Guid remarkId, Guid? resourceId, string type, string userId)
        {
            if (type.Empty())
            {
                throw new ServiceException(OperationCodes.EmptyReportType, 
                    $"Empty report type sent by user: '{userId}' "
                    + $"for remark: '{remarkId}' and resource: '{resourceId}'.");
            }
            type = type.ToLowerInvariant();
            if (await _reportRepository.ExistsAsync(remarkId, resourceId, type, userId))
            {
                throw new ServiceException(OperationCodes.ReportAlreadySent, 
                    $"Report: '{type}' was already sent by user: '{userId}' " 
                    + $"for remark: '{remarkId}' and resource: '{resourceId}'.");                
            }
            Report report = null;
            switch(type)
            {
                case "activity": report = await ReportActivityAsync(remarkId, resourceId, userId); break;
                case "comment": report = await ReportCommentAsync(remarkId, resourceId, userId); break;
                case "remark": report = await ReportRemarkAsync(remarkId, userId); break;
                default: throw new ServiceException(OperationCodes.InvalidReportType, 
                    $"Invalid report type: '{type}' sent by user: '{userId}' "
                    + $"for remark: '{remarkId}' and resource: '{resourceId}'.");
            }
            await _reportRepository.AddAsync(report);
        }

        private async Task<Report> ReportActivityAsync(Guid remarkId, Guid? activityId, string userId)
        {
            if (activityId == null || activityId == Guid.Empty)
            {
                throw new ServiceException(OperationCodes.EmptyReportResource, 
                    $"Empty report resource for activity type sent by user: '{userId}' "
                    + $"for remark: '{remarkId}'.");   
            }
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var acitivity = remark.GetStateOrFail(activityId.Value);

            return new Report(remarkId, activityId, "activity", userId);
        }

        private async Task<Report> ReportCommentAsync(Guid remarkId, Guid? commentId, string userId)
        {
            if (commentId == null || commentId == Guid.Empty)
            {
                throw new ServiceException(OperationCodes.EmptyReportResource, 
                    $"Empty report resource for comment type sent by user: '{userId}' "
                    + $"for remark: '{remarkId}'.");             
            }
            var remark = await _remarkRepository.GetOrFailAsync(remarkId);
            var acitivity = remark.GetCommentOrFail(commentId.Value);

            return new Report(remarkId, commentId, "comment", userId);
        }

        private async Task<Report> ReportRemarkAsync(Guid remarkId, string userId)
        {
            if(await _remarkRepository.ExistsAsync(remarkId) == false)
            {
                throw new ServiceException(OperationCodes.RemarkNotFound,
                    $"Remark with id: '{remarkId}' does not exist!");
            }

            return new Report(remarkId, null, "remark", userId);
        }
    }
}