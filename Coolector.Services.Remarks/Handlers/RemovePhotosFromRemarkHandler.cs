using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coolector.Common.Commands;
using Coolector.Common.Extensions;
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
            var removedPhotos = new string[]{};
            await _handler
                .Run(async () =>
                {   
                    var groupIds = command.Photos?
                                .Where(x => x.GroupId != Guid.Empty)
                                .Select(x => x.GroupId)
                                .ToArray() ?? new Guid[]{};

                    var names = command.Photos?
                                .Where(x => x.Name.NotEmpty())
                                .Select(x => x.Name)
                                .ToArray() ?? new string[]{};

                    var namesForGroups =  await _remarkService.GetPhotosForGroupsAsync(command.RemarkId, groupIds);
                    removedPhotos = names
                                    .Union(namesForGroups.HasValue ? namesForGroups.Value : Enumerable.Empty<string>())
                                    .Distinct()
                                    .ToArray();

                    await _remarkService.RemovePhotosAsync(command.RemarkId, removedPhotos);
                })
                .OnSuccess(async () => await _bus.PublishAsync(new PhotosFromRemarkRemoved(command.Request.Id, 
                    command.RemarkId, command.UserId, removedPhotos)))
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