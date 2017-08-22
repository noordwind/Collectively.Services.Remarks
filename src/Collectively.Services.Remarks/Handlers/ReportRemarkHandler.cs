using System.Threading.Tasks;
using Collectively.Common.Services;
using Collectively.Messages.Events.Remarks;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories;
using Collectively.Common.ServiceClients;
using Collectively.Services.Remarks.Dto;
using Collectively.Services.Remarks.Services;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Commands;
using RawRabbit;

namespace Collectively.Services.Remarks.Handlers
{
    public class ReportRemarkHandler : ICommandHandler<ReportRemark>
    {
        private readonly IBusClient _bus;
        private readonly IHandler _handler;
        private readonly IReportService _reportService;

        public ReportRemarkHandler(IBusClient bus, 
            IHandler handler, 
            IReportService reportService)
        {
            _bus = bus;
            _handler = handler;
            _reportService = reportService;
        }

        public async Task HandleAsync(ReportRemark command)
        {
            await _handler
                .Run(async () => await _reportService.AddAsync(command.RemarkId, 
                    command.ResourceId, command.Type, command.UserId))
                .OnSuccess(async () => await _bus.PublishAsync(new RemarkReported(command.Request.Id, 
                        command.UserId, command.RemarkId, command.ResourceId, command.Type))
                )
                .OnCustomError(async ex => await _bus.PublishAsync(new ReportRemarkRejected(command.Request.Id,
                    command.UserId, command.RemarkId, command.ResourceId, command.Type, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while reporting a remark.");
                    await _bus.PublishAsync(new ReportRemarkRejected(command.Request.Id,
                        command.UserId, command.RemarkId, command.ResourceId, command.Type, 
                        OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}