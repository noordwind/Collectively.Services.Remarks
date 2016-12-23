using System.Linq;
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
    public class RemovePhotosFromRemarkHandler : ICommandHandler<RemovePhotosFromRemark>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;

        public RemovePhotosFromRemarkHandler(IHandler handler,
            IBusClient bus,
            IRemarkService remarkService)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
        }

        public async Task HandleAsync(RemovePhotosFromRemark command)
        {
            await _handler
                .Run(async () =>
                {
                    await _remarkService.RemovePhotosAsync(command.RemarkId, command.Photos?.ToArray() ?? new string[]{});
                })
                .OnSuccess(async () => await _bus.PublishAsync(new PhotosFromRemarkRemoved(command.Request.Id, 
                    command.RemarkId, command.UserId, command.Photos)))
                .OnCustomError(ex => _bus.PublishAsync(new RemovePhotosFromRemarkRejected(command.Request.Id,
                    command.RemarkId, command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, $"Error occured while removing photos from the remark with id: '{command.RemarkId}'.");
                    await _bus.PublishAsync(new RemovePhotosFromRemarkRejected(command.Request.Id,
                        command.RemarkId, command.UserId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}