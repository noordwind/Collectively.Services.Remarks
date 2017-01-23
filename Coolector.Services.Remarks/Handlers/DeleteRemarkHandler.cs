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
    public class DeleteRemarkHandler : ICommandHandler<DeleteRemark>
    {
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
                .Validate(async () => await _remarkService.ValidateEditorAccessOrFailAsync(command.RemarkId, command.UserId))
                .Run(async () =>
                {
                    await _remarkService.DeleteAsync(command.RemarkId);
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