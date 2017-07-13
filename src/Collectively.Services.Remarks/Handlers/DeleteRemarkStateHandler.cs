using System.Threading.Tasks;
using Collectively.Common.Services;
using Collectively.Messages.Commands;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Collectively.Services.Remarks.Services;
using RawRabbit;

namespace Collectively.Services.Remarks.Handlers
{
    public class DeleteRemarkStateHandler : ICommandHandler<DeleteRemarkState>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _busClient;
        private readonly IRemarkStateService _remarkStateService;

        public DeleteRemarkStateHandler(IHandler handler,
            IBusClient busClient,
            IRemarkStateService remarkStateService)
        {
            _handler = handler;
            _busClient = busClient;
            _remarkStateService = remarkStateService;
        }

        public async Task HandleAsync(DeleteRemarkState command)
        {
            await _handler
                .Validate(async () => await _remarkStateService
                    .ValidateRemoveStateAccessOrFailAsync(command.RemarkId, command.StateId, command.UserId))
                .Run(async () => await  _remarkStateService
                    .DeleteStateAsync(command.RemarkId, command.UserId, command.StateId))
                .OnCustomError(async ex => await _busClient.PublishAsync(new DeleteRemarkStateRejected(
                    command.Request.Id, command.RemarkId, command.StateId,
                    command.UserId, ex.Message, ex.Code)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while deleting remark state." +
                        $" remarkId: {command.RemarkId}, stateId: {command.StateId}");
                    await _busClient.PublishAsync(new DeleteRemarkStateRejected(
                        command.Request.Id, command.RemarkId, command.StateId,
                        command.UserId, ex.Message, OperationCodes.Error));
                })
                .OnSuccess(async () => await _busClient
                    .PublishAsync(new RemarkStateDeleted(command.Request.Id,
                        command.UserId, command.RemarkId, command.StateId)))
                .ExecuteAsync();
        }
    }
}