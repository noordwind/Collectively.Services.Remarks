using System.Linq;
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
    public class EditRemarkCommentHandler : ICommandHandler<EditRemarkComment>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkCommentService _remarkCommentService;

        public EditRemarkCommentHandler(IHandler handler, IBusClient bus, IRemarkCommentService remarkCommentService)
        {
            _handler = handler;
            _bus = bus;
            _remarkCommentService = remarkCommentService;
        }

        public async Task HandleAsync(EditRemarkComment command)
        {
            CommentHistory history = null;
            await _handler
                .Validate(async () => await _remarkCommentService
                    .ValidateEditorAccessOrFailAsync(command.RemarkId, command.CommentId, command.UserId))
                .Run(async () => 
                {
                    await _remarkCommentService.EditAsync(command.RemarkId, command.CommentId, command.Text);
                    var commentValue = await _remarkCommentService.GetAsync(command.RemarkId, command.CommentId);
                    history = commentValue.Value.History.Last();
                })
                .OnSuccess(async () => await _bus.PublishAsync(new CommentEditedInRemark(command.Request.Id, 
                    command.UserId, command.RemarkId, command.CommentId, history.Text, history.CreatedAt)))
                .OnCustomError(ex => _bus.PublishAsync(new EditRemarkCommentRejected(command.Request.Id,
                    command.RemarkId, command.CommentId, command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while editing a comment in a remark.");
                    await _bus.PublishAsync(new EditRemarkCommentRejected(command.Request.Id,
                        command.RemarkId, command.CommentId, command.UserId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}