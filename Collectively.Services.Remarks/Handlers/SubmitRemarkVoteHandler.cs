using System.Threading.Tasks;
using  Collectively.Common.Commands;
using  Collectively.Common.Services;
using Collectively.Services.Remarks.Services;

using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using RawRabbit;

namespace Collectively.Services.Remarks.Handlers
{
    public class SubmitRemarkVoteHandler : ICommandHandler<SubmitRemarkVote>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;

        public SubmitRemarkVoteHandler(IHandler handler, IBusClient bus, IRemarkService remarkService)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
        }

        public async Task HandleAsync(SubmitRemarkVote command)
        {
            await _handler
                .Run(async () => await _remarkService.SubmitVoteAsync(command.RemarkId, 
                    command.UserId, command.Positive, command.CreatedAt))
                .OnSuccess(async () => await _bus.PublishAsync(new RemarkVoteSubmitted(command.Request.Id, 
                        command.UserId, command.RemarkId, command.Positive, command.CreatedAt)))
                .OnCustomError(ex => _bus.PublishAsync(new SubmitRemarkVoteRejected(command.Request.Id,
                    command.UserId, command.RemarkId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while submitting a remark vote.");
                    await _bus.PublishAsync(new SubmitRemarkVoteRejected(command.Request.Id,
                        command.UserId, command.RemarkId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}