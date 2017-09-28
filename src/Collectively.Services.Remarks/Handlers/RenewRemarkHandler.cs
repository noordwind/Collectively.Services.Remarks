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
using RemarkState = Collectively.Services.Remarks.Domain.RemarkState;

namespace Collectively.Services.Remarks.Handlers
{
    public class RenewRemarkHandler : ICommandHandler<RenewRemark>
    {
        private static readonly ILogger Logger = Log.Logger;
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;
        private readonly IGroupService _groupService;
        private readonly IRemarkStateService _remarkStateService;
        private readonly IFileResolver _fileResolver;
        private readonly IFileValidator _fileValidator;
        private readonly IResourceFactory _resourceFactory;

        public RenewRemarkHandler(IHandler handler,
            IBusClient bus,
            IRemarkService remarkService,
            IGroupService groupService,
            IRemarkStateService remarkStateService,
            IFileResolver fileResolver,
            IFileValidator fileValidator,
            IResourceFactory resourceFactory)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
            _groupService = groupService;
            _remarkStateService = remarkStateService;
            _fileResolver = fileResolver;
            _fileValidator = fileValidator;
            _resourceFactory = resourceFactory;
        }

        public async Task HandleAsync(RenewRemark command)
        {
            await _handler
                .Validate(async () => 
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    if(remark.Value.Group == null)
                    {
                        return;
                    }
                    await _groupService.ValidateIfRemarkCanBeRenewedOrFailAsync(remark.Value.Group.Id, command.UserId);
                })
                .Run(async () =>
                {
                    Location location = null;
                    if (command.Latitude != 0 && command.Longitude != 0)
                    {
                        location = Location.Create(command.Latitude, command.Longitude, command.Address);
                    }
                    await _remarkStateService.RenewAsync(command.RemarkId, command.UserId, command.Description, location);
                })
                .OnSuccess(async () =>
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    var state = remark.Value.GetLatestStateOf(RemarkState.Names.Renewed).Value;
                    var resource = _resourceFactory.Resolve<RemarkRenewed>(command.RemarkId);
                    await _bus.PublishAsync(new RemarkRenewed(command.Request.Id, resource, 
                        command.UserId, command.RemarkId));
                })
                .OnCustomError(async ex => await _bus.PublishAsync(new RenewRemarkRejected(command.Request.Id,
                    command.UserId, command.RemarkId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while renewing a remark.");
                    await _bus.PublishAsync(new RenewRemarkRejected(command.Request.Id,
                        command.UserId, command.RemarkId, OperationCodes.Error, ex.Message));
                })
                .ExecuteAsync();
        }
    }
}