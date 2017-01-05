using System.Threading.Tasks;
using Coolector.Common.Commands;
using Coolector.Common.Services;
using Coolector.Services.Remarks.Services;
using Coolector.Services.Remarks.Shared;
using Coolector.Services.Remarks.Shared.Commands;
using Coolector.Services.Remarks.Shared.Events;
using RawRabbit;

namespace Coolector.Services.Remarks.Handlers
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
                    command.UserId, command.Positive))
                .OnSuccess(async () => await _bus.PublishAsync(new RemarkVoteSubmitted(command.Request.Id, 
                        command.UserId, command.RemarkId, command.Positive)))
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