using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collectively.Messages.Commands;
using Collectively.Common.Domain;
using Collectively.Common.Files;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Services;
using Collectively.Services.Remarks.Settings;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using RawRabbit;

namespace Collectively.Services.Remarks.Handlers
{
    public class AddPhotosToRemarkHandler : ICommandHandler<AddPhotosToRemark>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;
        private readonly IRemarkPhotoService _remarkPhotoService;
        private readonly IFileResolver _fileResolver;
        private readonly IFileValidator _fileValidator;
        private readonly GeneralSettings _generalSettings;
        private readonly IResourceFactory _resourceFactory;

        public AddPhotosToRemarkHandler(IHandler handler,
            IBusClient bus,
            IRemarkService remarkService,
            IRemarkPhotoService remarkPhotoService,
            IFileResolver fileResolver,
            IFileValidator fileValidator,
            GeneralSettings generalSettings,
            IResourceFactory resourceFactory)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
            _remarkPhotoService = remarkPhotoService;
            _fileResolver = fileResolver;
            _fileValidator = fileValidator;
            _generalSettings = generalSettings;
            _resourceFactory = resourceFactory;
        }

        public async Task HandleAsync(AddPhotosToRemark command)
        {
            await _handler
                .Validate(async () => 
                {
                    await _remarkService.ValidateEditorAccessOrFailAsync(command.RemarkId, command.UserId);
                    if (command.Photos == null || !command.Photos.Any())
                    {
                        throw new ServiceException(OperationCodes.NoFiles, 
                            $"There are no photos to be added to the remark with id: '{command.RemarkId}'.");
                    }
                    if (command.Photos.Count() > _generalSettings.PhotosLimit) 
                    {
                        throw new ServiceException(OperationCodes.TooManyFiles);
                    }
                })
                .Run(async () =>
                {
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
                    await _remarkPhotoService.AddPhotosAsync(command.RemarkId, photos.ToArray());
                })
                .OnSuccess(async () =>
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    var resource = _resourceFactory.Resolve<PhotosToRemarkAdded>(command.RemarkId);
                    await _bus.PublishAsync(new PhotosToRemarkAdded(command.Request.Id, resource, 
                        command.UserId, command.RemarkId));
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