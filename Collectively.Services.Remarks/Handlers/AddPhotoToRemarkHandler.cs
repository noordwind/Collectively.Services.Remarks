using System.Threading.Tasks;
using Collectively.Common.Domain;
using Collectively.Common.Extensions;
using Collectively.Common.Files;
using Collectively.Common.Services;
using Collectively.Messages.Commands;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Collectively.Services.Remarks.Services;
using RawRabbit;

namespace Collectively.Services.Remarks.Handlers
{
    public class AddPhotoToRemarkHandler : ICommandHandler<AddPhotoToRemark>
    {
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IFileResolver _fileResolver;
        private readonly IFileValidator _fileValidator;
        private readonly IRemarkPhotoService _remarkPhotoService;
        private readonly IRemarkService _remarkService;
        private readonly IResourceFactory _resourceFactory;

        public AddPhotoToRemarkHandler(IHandler handler,
            IBusClient bus,
            IFileResolver fileResolver,
            IFileValidator fileValidator,
            IRemarkPhotoService remarkPhotoService,
            IRemarkService remarkService,
            IResourceFactory resourceFactory)
        {
            _handler = handler;
            _bus = bus;
            _fileResolver = fileResolver;
            _fileValidator = fileValidator;
            _remarkPhotoService = remarkPhotoService;
            _remarkService = remarkService;
            _resourceFactory = resourceFactory;
        }

        public async Task HandleAsync(AddPhotoToRemark command)
        {
            await _handler
                .Validate(() =>
                {
                    if (command.FileBase64.Empty())
                    {
                        throw new ServiceException(OperationCodes.NoFiles,
                            $"Photo is missing, remarkId: {command.RemarkId}");
                    }
                })
                .Run(async () =>
                {
                    var resolvedFile = _fileResolver.FromBase64(command.FileBase64,
                        command.Name, command.ContentType ?? "image");
                    if (resolvedFile.HasNoValue)
                        throw new ServiceException(OperationCodes.CannotConvertFile);

                    var photo = resolvedFile.Value;
                    var isImage = _fileValidator.IsImage(photo);
                    if (!isImage)
                        throw new ServiceException(OperationCodes.InvalidFile);

                    await _remarkPhotoService.AddPhotosAsync(command.RemarkId, command.UserId, photo);
                })
                .OnSuccess(async () =>
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    var resource = _resourceFactory.Resolve<PhotosToRemarkAdded>(remark.Value.Id);
                    await _bus.PublishAsync(new PhotosToRemarkAdded(command.Request.Id, resource,
                        command.UserId, command.RemarkId));
                })
                .OnCustomError(async ex => await _bus.PublishAsync(new AddPhotosToRemarkRejected(command.Request.Id,
                    command.RemarkId, command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while adding photo to remark.");
                    await _bus.PublishAsync(new AddPhotosToRemarkRejected(command.Request.Id,
                        command.RemarkId, command.UserId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}