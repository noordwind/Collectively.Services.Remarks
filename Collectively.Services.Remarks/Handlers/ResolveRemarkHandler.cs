using System.Linq;
using System.Threading.Tasks;
using  Collectively.Common.Commands;
using  Collectively.Common.Domain;
using  Collectively.Common.Services;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Services;

using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Collectively.Messages.Events.Remarks.Models;
using NLog;
using RawRabbit;
using RemarkState = Collectively.Services.Remarks.Domain.RemarkState;

namespace Collectively.Services.Remarks.Handlers
{
    public class ResolveRemarkHandler : ICommandHandler<ResolveRemark>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;
        private readonly IFileResolver _fileResolver;
        private readonly IFileValidator _fileValidator;

        public ResolveRemarkHandler(IHandler handler,
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

        public async Task HandleAsync(ResolveRemark command)
        {
            File file = null;
            
            await _handler
                .Validate(() => 
                {
                    Logger.Debug($"Handle {nameof(ResolveRemark)} command, remarkId:{command.RemarkId}, " +
                        $"userId:{command.UserId}, lat-long:{command.Latitude}-{command.Longitude}");

                    if (command.ValidatePhoto)
                    {
                        var resolvedFile = _fileResolver.FromBase64(command.Photo.Base64, command.Photo.Name, command.Photo.ContentType);
                        if (resolvedFile.HasNoValue)
                        {
                            Logger.Error($"File cannot be resolved from base64, photoName:{command.Photo.Name}, " +
                                $"contentType:{command.Photo.ContentType}, userId:{command.UserId}");
                            throw new ServiceException(OperationCodes.CannotConvertFile);
                        }
                        file = resolvedFile.Value;

                        var isImage = _fileValidator.IsImage(file);
                        if (isImage == false)
                        {
                            Logger.Warn($"File is not an image! name:{file.Name}, contentType:{file.ContentType}, " +
                                $"userId:{command.UserId}");
                            throw new ServiceException(OperationCodes.InvalidFile);
                        }
                    }
                })
                .Run(async () =>
                {
                    Location location = null;
                    if (command.Latitude != 0 && command.Longitude != 0)
                    {
                        location = Location.Create(command.Latitude, command.Longitude, command.Address);
                    }

                    await _remarkService.ResolveAsync(command.RemarkId, command.UserId, file, location, command.ValidateLocation);
                })
                .OnSuccess(async () =>
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    var state = remark.Value.GetLatestStateOf(RemarkState.Names.Resolved).Value;
                    var location =  state.Location == null ? 
                                    null : 
                                    new RemarkLocation(state.Location.Address, state.Location.Latitude, state.Location.Longitude);
                    await _bus.PublishAsync(new RemarkResolved(command.Request.Id, command.RemarkId,
                        command.UserId, state.User.Name, string.Empty, location, state.CreatedAt,
                        remark.Value.Photos.Select(x => new RemarkFile(x.GroupId, x.Name, x.Size, x.Url, x.Metadata)).ToArray()));
                })
                .OnCustomError(async ex => await _bus.PublishAsync(new ResolveRemarkRejected(command.Request.Id,
                    command.UserId, command.RemarkId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while resolving a remark");
                    await _bus.PublishAsync(new ResolveRemarkRejected(command.Request.Id,
                        command.UserId, command.RemarkId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}