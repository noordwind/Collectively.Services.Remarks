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
    public class ResolveRemarkHandler : ICommandHandler<ResolveRemark>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;
        private readonly IFileResolver _fileResolver;
        private readonly IFileValidator _fileValidator;

        public ResolveRemarkHandler(IBusClient bus,
            IRemarkService remarkService,
            IFileResolver fileResolver,
            IFileValidator fileValidator)
        {
            _bus = bus;
            _remarkService = remarkService;
            _fileResolver = fileResolver;
            _fileValidator = fileValidator;
        }

        public async Task HandleAsync(ResolveRemark command)
        {
            Logger.Debug($"Handle {nameof(ResolveRemark)} command, remarkId:{command.RemarkId}, " +
                $"userId:{command.UserId}, lat-long:{command.Latitude}-{command.Longitude}");
            var file = _fileResolver.FromBase64(command.Photo.Base64, command.Photo.Name, command.Photo.ContentType);
            if (file.HasNoValue)
            {
                Logger.Warn($"File cannot be resolved from base64, photoName:{command.Photo.Name}, " +
                    $"contentType:{command.Photo.ContentType}, userId:{command.UserId}");
                return;
            }

            var isImage = _fileValidator.IsImage(file.Value);
            if (isImage == false)
            {
                Logger.Warn($"File is not an image! name:{file.Value.Name}, contentType:{file.Value.ContentType}, " +
                    $"userId:{command.UserId}");
                return;
            }

            var location = Location.Create(command.Latitude, command.Longitude);
            await _remarkService.ResolveAsync(command.RemarkId, command.UserId, file.Value, location);

            var remark = await _remarkService.GetAsync(command.RemarkId);

            await _bus.PublishAsync(new RemarkResolved(command.RemarkId,
                command.UserId,
                remark.Value.Photos.Select(x => new RemarkFile(x.Name, x.Size, x.Url, x.Metadata)).ToArray(),
                remark.Value.ResolvedAt.GetValueOrDefault()));
        }
    }
}