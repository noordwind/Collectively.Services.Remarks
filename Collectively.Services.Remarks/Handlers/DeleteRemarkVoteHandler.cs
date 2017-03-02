using System.Threading.Tasks;
using Collectively.Messages.Commands;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Services;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using RawRabbit;

namespace Collectively.Services.Remarks.Handlers
{
    public class DeleteRemarkVoteHandler : ICommandHandler<DeleteRemarkVote>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;

        public DeleteRemarkVoteHandler(IHandler handler, IBusClient bus, IRemarkService remarkService)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
        }

        public async Task HandleAsync(DeleteRemarkVote command)
        {
            await _handler
                .Run(async () => await _remarkService.DeleteVoteAsync(command.RemarkId, command.UserId))
                .OnSuccess(async () => await _bus.PublishAsync(new RemarkVoteDeleted(command.Request.Id, 
                    command.UserId, command.RemarkId)))
                .OnCustomError(ex => _bus.PublishAsync(new DeleteRemarkVoteRejected(command.Request.Id,
                    command.UserId, command.RemarkId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while deleting a remark vote.");
                    await _bus.PublishAsync(new DeleteRemarkVoteRejected(command.Request.Id,
                        command.UserId, command.RemarkId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}