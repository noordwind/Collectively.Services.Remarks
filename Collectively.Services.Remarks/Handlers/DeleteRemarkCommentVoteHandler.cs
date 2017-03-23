using System.Threading.Tasks;
using Collectively.Common.Services;
using Collectively.Messages.Commands;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Services;
using RawRabbit;

namespace Collectively.Services.Remarks.Handlers
{
    public class DeleteRemarkCommentVoteHandler : ICommandHandler<DeleteRemarkCommentVote>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkCommentService _remarkCommentService;

        public DeleteRemarkCommentVoteHandler(IHandler handler, IBusClient bus, IRemarkCommentService remarkCommentService)
        {
            _handler = handler;
            _bus = bus;
            _remarkCommentService = remarkCommentService;
        }

        public async Task HandleAsync(DeleteRemarkCommentVote command)
        {
            Comment comment = null;
            await _handler
                .Run(async () => await _remarkCommentService.DoSomethingAsync())
                .OnSuccess(async () => await _bus.PublishAsync(new RemarkCommentVoteDeleted(command.Request.Id, 
                    command.UserId, command.RemarkId, comment.Id)))
                .OnCustomError(ex => _bus.PublishAsync(new DeleteRemarkCommentVoteRejected(command.Request.Id,
                    command.RemarkId, command.CommentId, command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while deleting a vote for a comment in a remark.");
                    await _bus.PublishAsync(new DeleteRemarkCommentVoteRejected(command.Request.Id,
                        command.RemarkId, command.CommentId, command.UserId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}