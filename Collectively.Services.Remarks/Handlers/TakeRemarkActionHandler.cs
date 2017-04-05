using System.Threading.Tasks;
using Collectively.Common.Services;
using Collectively.Messages.Commands;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Collectively.Services.Remarks.Services;
using RawRabbit;

namespace Collectively.Services.Remarks.Handlers
{
    public class TakeRemarkActionHandler : ICommandHandler<TakeRemarkAction>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkActionService _remarkActionService;

        public TakeRemarkActionHandler(IHandler handler, IBusClient bus, IRemarkActionService remarkActionService)
        {
            _handler = handler;
            _bus = bus;
            _remarkActionService = remarkActionService;
        }

        public async Task HandleAsync(TakeRemarkAction command)
        {
            var username = string.Empty;
            await _handler
                .Run(async () => 
                {
                    await _remarkActionService.ParticipateAsync(command.RemarkId, command.UserId, command.Description);
                    var participant = await _remarkActionService.GetParticipantAsync(command.RemarkId, command.UserId);
                    username = participant.Value.User.Name;
                })
                .OnSuccess(async () => await _bus.PublishAsync(new RemarkActionTaken(command.Request.Id, 
                        command.UserId, username, command.RemarkId, command.Description)))
                .OnCustomError(ex => _bus.PublishAsync(new TakeRemarkActionRejected(command.Request.Id,
                    command.UserId, command.RemarkId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, $"Error occured while taking action for remark: '{command.RemarkId}' by user: '{command.UserId}'.");
                    await _bus.PublishAsync(new TakeRemarkActionRejected(command.Request.Id,
                        command.UserId, command.RemarkId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}