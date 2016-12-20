using System.Threading.Tasks;
using Coolector.Common;
using Coolector.Common.Commands;
using Coolector.Common.Domain;
using Coolector.Common.Services;
using Coolector.Services.Remarks.Services;
using Coolector.Services.Remarks.Shared;
using Coolector.Services.Remarks.Shared.Commands;
using Coolector.Services.Remarks.Shared.Events;
using NLog;
using RawRabbit;

namespace Coolector.Services.Remarks.Handlers
{
    public class DeleteRemarkHandler : ICommandHandler<DeleteRemark>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;

        public DeleteRemarkHandler(IHandler handler, IBusClient bus, IRemarkService remarkService)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
        }

        public async Task HandleAsync(DeleteRemark command)
        {
            await _handler
                .Run(async () =>
                {
                    Logger.Debug($"Handle {nameof(DeleteRemark)} command, remarkId:{command.RemarkId}, " +
                                 $"userId:{command.UserId}");
                    await _remarkService.DeleteAsync(command.RemarkId, command.UserId);
                })
                .OnSuccess(async () 
                    => await _bus.PublishAsync(new RemarkDeleted(command.Request.Id, command.RemarkId, command.UserId)))
                .OnCustomError(ex => _bus.PublishAsync(new DeleteRemarkRejected(command.Request.Id,
                    command.RemarkId, command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while deleting a remark");
                    await _bus.PublishAsync(new DeleteRemarkRejected(command.Request.Id,
                        command.RemarkId, command.UserId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}