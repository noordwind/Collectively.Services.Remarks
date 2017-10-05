using System.Threading.Tasks;
using Collectively.Common.Files;
using Collectively.Messages.Commands;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Services;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Serilog;
using RawRabbit;
using Collectively.Common.Domain;
using Collectively.Common.Locations;
using System.Linq;

namespace Collectively.Services.Remarks.Handlers
{
    public class EditRemarkHandler : ICommandHandler<EditRemark>
    {
        private static readonly ILogger Logger = Log.Logger;
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;
        private readonly ILocationService _locationService;
        private readonly IResourceFactory _resourceFactory;

        public EditRemarkHandler(IHandler handler,
            IBusClient bus,
            IRemarkService remarkService,
            ILocationService locationService,
            IResourceFactory resourceFactory)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
            _locationService = locationService;
            _resourceFactory = resourceFactory;
        }

        public async Task HandleAsync(EditRemark command)
            => await _handler
                .Validate(async () => 
                    await _remarkService.ValidateEditorAccessOrFailAsync(command.RemarkId, command.UserId))            
                .Run(async () =>
                {
                    Location location = null;
                    if (command.Latitude.HasValue && command.Latitude != 0 && 
                        command.Longitude.HasValue && command.Longitude != 0)
                    {
                        var locations = await _locationService.GetAsync(command.Latitude.Value, command.Longitude.Value);
                        if (locations.HasNoValue || locations.Value.Results == null || !locations.Value.Results.Any())
                        {
                            throw new ServiceException(OperationCodes.AddressNotFound, 
                                $"Address was not found for remark with id: '{command.RemarkId}' " +
                                $"latitude: {command.Latitude}, longitude:  {command.Longitude}.");
                        }
                        var address = locations.Value.Results.First().FormattedAddress;
                        location = Domain.Location.Create(command.Latitude.Value, command.Longitude.Value, address);
                    }
                    await _remarkService.EditAsync(command.RemarkId, command.UserId, 
                        command.GroupId, command.Category, command.Description, location);
                })
                .OnSuccess(async () =>
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    var resource = _resourceFactory.Resolve<RemarkEdited>(command.RemarkId);
                    await _bus.PublishAsync(new RemarkEdited(command.Request.Id, resource, 
                        command.UserId, command.RemarkId));
                })
                .OnCustomError(async ex => await _bus.PublishAsync(new EditRemarkRejected(command.Request.Id,
                    command.UserId, command.RemarkId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while editing a remark.");
                    await _bus.PublishAsync(new EditRemarkRejected(command.Request.Id,
                        command.UserId, command.RemarkId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
    }
}