using System.Threading.Tasks;
using Collectively.Common.Services;
using Collectively.Messages.Commands;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Policies;
using Collectively.Services.Remarks.Services;
using RawRabbit;

namespace Collectively.Services.Remarks.Handlers
{
    public class AddCommentToRemarkHandler : ICommandHandler<AddCommentToRemark>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkCommentService _remarkCommentService;
        private readonly IAddCommentPolicy _policy;

        public AddCommentToRemarkHandler(IHandler handler, IBusClient bus, 
            IRemarkCommentService remarkCommentService, IAddCommentPolicy policy)
        {
            _handler = handler;
            _bus = bus;
            _remarkCommentService = remarkCommentService;
            _policy = policy;
        }

        public async Task HandleAsync(AddCommentToRemark command)
        {
            Comment comment = null;
            await _handler
                .Validate(async () =>  await _policy.ValidateAsync(command.RemarkId, command.UserId))           
                .Run(async () => 
                {
                    await _remarkCommentService.AddAsync(command.RemarkId, command.CommentId, command.UserId, command.Text);
                    var commentValue = await _remarkCommentService.GetAsync(command.RemarkId, command.CommentId);
                    comment = commentValue.Value;
                })
                .OnSuccess(async () => await _bus.PublishAsync(new CommentAddedToRemark(command.Request.Id, 
                    command.UserId, comment.User.Name, command.RemarkId, comment.Id, comment.Text, comment.CreatedAt)))
                .OnCustomError(ex => _bus.PublishAsync(new AddCommentToRemarkRejected(command.Request.Id,
                    command.RemarkId, command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, $"Error occured while adding a comment to the remark: '{command.RemarkId}' by user: '{command.UserId}'.");
                    await _bus.PublishAsync(new AddCommentToRemarkRejected(command.Request.Id,
                        command.RemarkId, command.UserId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}