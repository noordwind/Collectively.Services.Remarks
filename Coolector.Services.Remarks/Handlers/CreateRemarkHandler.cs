using System;
using System.Linq;
using System.Threading.Tasks;
using Coolector.Common.Commands;
using Coolector.Common.Commands.Remarks;
using Coolector.Common.Events.Remarks;
using Coolector.Common.Events.Remarks.Models;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Services;
using NLog;
using RawRabbit;

namespace Coolector.Services.Remarks.Handlers
{
    public class CreateRemarkHandler : ICommandHandler<CreateRemark>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IBusClient _bus;
        private readonly IFileResolver _fileResolver;
        private readonly IFileValidator _fileValidator;
        private readonly IRemarkService _remarkService;

        public CreateRemarkHandler(IBusClient bus, 
            IFileResolver fileResolver, 
            IFileValidator fileValidator,
            IRemarkService remarkService)
        {
            _bus = bus;
            _fileResolver = fileResolver;
            _fileValidator = fileValidator;
            _remarkService = remarkService;
        }

        public async Task HandleAsync(CreateRemark command)
        {
            Logger.Debug($"Handle {nameof(CreateRemark)} command, userId:{command.UserId}, categoryId:{command.CategoryId}, lat-long:{command.Latitude}-{command.Longitude}");
            var file = _fileResolver.FromBase64(command.Photo.Base64, command.Photo.Name, command.Photo.ContentType);
            if (file.HasNoValue)
            {
                Logger.Warn($"File cannot be resolved from base64, photoName:{command.Photo.Name}, contentType:{command.Photo.ContentType}, userId:{command.UserId}");
                return;
            }
                

            var isImage = _fileValidator.IsImage(file.Value);
            if (!isImage)
            {
                Logger.Warn($"File is not an image! name:{file.Value.Name}, contentType:{file.Value.ContentType}, userId:{command.UserId}");
                return;
            }

            var remarkId = Guid.NewGuid();
            var location = Location.Create(command.Latitude, command.Longitude, command.Address);
            await _remarkService.CreateAsync(remarkId, command.UserId, command.CategoryId,
                file.Value, location, command.Description);
            var remark = await _remarkService.GetAsync(remarkId);
            await _bus.PublishAsync(new RemarkCreated(remarkId, command.UserId,
                new RemarkCreated.RemarkCategory(remark.Value.Category.Id, remark.Value.Category.Name),
                new RemarkCreated.RemarkLocation(remark.Value.Location.Address, command.Latitude, command.Longitude),
                remark.Value.Photos.Select(x => new RemarkFile(x.Name, x.Size, x.Url, x.Metadata)),
                command.Description));
        }
    }
}