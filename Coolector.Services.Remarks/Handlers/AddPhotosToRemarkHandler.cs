using System;
using System.Linq;
using System.Threading.Tasks;
using Coolector.Common;
using Coolector.Common.Commands;
using Coolector.Common.Domain;
using Coolector.Common.Services;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Services;
using Coolector.Services.Remarks.Shared.Commands;
using Coolector.Services.Remarks.Shared.Commands.Models;
using Coolector.Services.Remarks.Shared.Events;
using NLog;
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

        public AddPhotosToRemarkHandler(IHandler handler,
            IBusClient bus,
            IRemarkService remarkService,
            IFileResolver fileResolver,
            IFileValidator fileValidator)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
            _fileResolver = fileResolver;
            _fileValidator = fileValidator;
        }

        public async Task HandleAsync(AddPhotosToRemark command)
        {
            await _handler
                .Run(async () =>
                {
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
                    }
                })
                .OnCustomError(ex => _bus.PublishAsync(new CreateRemarkRejected(command.Request.Id,
                    command.RemarkId, command.UserId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while creating remark.");
                    await _bus.PublishAsync(new CreateRemarkRejected(command.Request.Id,
                        command.RemarkId, command.UserId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}