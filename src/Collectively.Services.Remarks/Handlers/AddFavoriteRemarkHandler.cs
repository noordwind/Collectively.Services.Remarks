using System.Threading.Tasks;
using Collectively.Messages.Commands;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Services;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using RawRabbit;

namespace Collectively.Services.Remarks.Handlers
{
    public class AddFavoriteRemarkHandler : ICommandHandler<AddFavoriteRemark>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;

        public AddFavoriteRemarkHandler(IHandler handler, IBusClient bus, IRemarkService remarkService)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
        }

        public async Task HandleAsync(AddFavoriteRemark command)
        {
            await _handler
                .Run(async () => await _remarkService.AddFavoriteRemarkAsync(command.RemarkId, command.UserId))
                .OnSuccess(async () => await _bus.PublishAsync(new FavoriteRemarkAdded(command.Request.Id, 
                        command.UserId, command.RemarkId)))
                .OnCustomError(ex => _bus.PublishAsync(new AddFavoriteRemarkRejected(command.Request.Id,
                    command.RemarkId, command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, $"Error occured while adding a favorite remark: '{command.RemarkId}' by user: '{command.UserId}'.");
                    await _bus.PublishAsync(new AddFavoriteRemarkRejected(command.Request.Id,
                        command.RemarkId, command.UserId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}