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
    public class DeleteRemarkCommentHandler : ICommandHandler<DeleteRemarkComment>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkCommentService _remarkCommentService;

        public DeleteRemarkCommentHandler(IHandler handler, IBusClient bus, IRemarkCommentService remarkCommentService)
        {
            _handler = handler;
            _bus = bus;
            _remarkCommentService = remarkCommentService;
        }

        public async Task HandleAsync(DeleteRemarkComment command)
        {
            await _handler
                .Validate(async () => await _remarkCommentService
                    .ValidateEditorAccessOrFailAsync(command.RemarkId, command.CommentId, command.UserId))
                .Run(async () => await _remarkCommentService.RemoveAsync(command.RemarkId, command.CommentId))
                .OnSuccess(async () => await _bus.PublishAsync(new CommentDeletedFromRemark(command.Request.Id, 
                    command.UserId, command.RemarkId, command.CommentId)))
                .OnCustomError(ex => _bus.PublishAsync(new DeleteRemarkCommentRejected(command.Request.Id,
                    command.RemarkId, command.CommentId, command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while deleting a comment from a remark.");
                    await _bus.PublishAsync(new DeleteRemarkCommentRejected(command.Request.Id,
                        command.RemarkId, command.CommentId, command.UserId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}