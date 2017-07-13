using System.Threading.Tasks;
using Collectively.Common.Services;
using Collectively.Messages.Commands;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Collectively.Services.Remarks.Services;
using RawRabbit;

namespace Collectively.Services.Remarks.Handlers
{
    public class CancelRemarkActionHandler : ICommandHandler<CancelRemarkAction>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkActionService _remarkActionService;

        public CancelRemarkActionHandler(IHandler handler, IBusClient bus, IRemarkActionService remarkActionService)
        {
            _handler = handler;
            _bus = bus;
            _remarkActionService = remarkActionService;
        }

        public async Task HandleAsync(CancelRemarkAction command)
        {
            await _handler
                .Run(async () => await _remarkActionService.CancelParticipationAsync(command.RemarkId, command.UserId))
                .OnSuccess(async () => await _bus.PublishAsync(new RemarkActionCanceled(command.Request.Id, 
                        command.UserId, command.RemarkId)))
                .OnCustomError(ex => _bus.PublishAsync(new CancelRemarkActionRejected(command.Request.Id,
                    command.UserId, command.RemarkId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, $"Error occured while canceling action for remark: '{command.RemarkId}' by user: '{command.UserId}'.");
                    await _bus.PublishAsync(new CancelRemarkActionRejected(command.Request.Id,
                        command.UserId, command.RemarkId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}