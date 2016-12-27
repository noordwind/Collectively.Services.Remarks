using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coolector.Common.Commands;
using Coolector.Common.Domain;
using Coolector.Common.Services;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Services;
using Coolector.Services.Remarks.Settings;
using Coolector.Services.Remarks.Shared;
using Coolector.Services.Remarks.Shared.Commands;
using Coolector.Services.Remarks.Shared.Events;
using RawRabbit;
using RemarkFile = Coolector.Services.Remarks.Shared.Events.Models.RemarkFile;

namespace Coolector.Services.Remarks.Handlers
{
    public class AddPhotosToRemarkHandler : ICommandHandler<AddPhotosToRemark>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;
        private readonly IFileResolver _fileResolver;
        private readonly IFileValidator _fileValidator;
        private readonly GeneralSettings _generalSettings;

        public AddPhotosToRemarkHandler(IHandler handler,
            IBusClient bus,
            IRemarkService remarkService,
            IFileResolver fileResolver,
            IFileValidator fileValidator,
            GeneralSettings generalSettings)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
            _fileResolver = fileResolver;
            _fileValidator = fileValidator;
            _generalSettings = generalSettings;
        }

        public async Task HandleAsync(AddPhotosToRemark command)
        {
            await _handler
                .Run(async () =>
                {
                    if (command.Photos == null || !command.Photos.Any())
                    {
                        throw new ServiceException(OperationCodes.NoFiles, 
                            $"There are no photos to be added to the remark with id: '{command.RemarkId}'.");
                    }
                    if (command.Photos.Count() > _generalSettings.PhotosLimit) 
                    {
                        throw new ServiceException(OperationCodes.TooManyFiles);
                    }

                    var photos = new List<File>();
                    foreach(var file in command.Photos)
                    {                        
                        var resolvedFile = _fileResolver.FromBase64(file.Base64, file.Name, file.ContentType);
                        if (resolvedFile.HasNoValue)
                        {
                            throw new ServiceException(OperationCodes.CannotConvertFile);
                        }
                        var photo = resolvedFile.Value;
                        var isImage = _fileValidator.IsImage(photo);
                        if (!isImage)
                        {
                            
                            throw new ServiceException(OperationCodes.InvalidFile);
                        }
                        photos.Add(photo);
                    }
                    await _remarkService.AddPhotosAsync(command.RemarkId, photos.ToArray());
                })
                .OnSuccess(async () =>
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    await _bus.PublishAsync(new PhotosToRemarkAdded(command.Request.Id, command.RemarkId, command.UserId, 
                        remark.Value.Photos.Select(x => new RemarkFile(x.GroupId, x.Name, x.Size, x.Url, x.Metadata)).ToArray()));
                })
                .OnCustomError(ex => _bus.PublishAsync(new AddPhotosToRemarkRejected(command.Request.Id,
                    command.RemarkId, command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while adding photos to remark.");
                    await _bus.PublishAsync(new AddPhotosToRemarkRejected(command.Request.Id,
                        command.RemarkId, command.UserId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}