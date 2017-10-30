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
    public class AssignRemarkToGroupHandler : ICommandHandler<AssignRemarkToGroup>
    {
        private static readonly ILogger Logger = Log.Logger;
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;
        private readonly IGroupService _groupService;
        private readonly IRemarkStateService _remarkStateService;
        private readonly IResourceFactory _resourceFactory;

        public AssignRemarkToGroupHandler(IHandler handler,
            IBusClient bus,
            IRemarkService remarkService,
            IGroupService groupService,
            IRemarkStateService remarkStateService,
            IResourceFactory resourceFactory)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
            _groupService = groupService;
            _remarkStateService = remarkStateService;
            _resourceFactory = resourceFactory;
        }

        public async Task HandleAsync(AssignRemarkToGroup command)
            => await _handler
                .Validate(async () => await _groupService.ValidateIfRemarkCanBeAssignedOrFailAsync(command.GroupId,
                        command.UserId, command.Latitude, command.Longitude))
                .Run(async () => await _remarkStateService.AssignToGroupAsync(command.RemarkId, 
                        command.UserId, command.GroupId, command.Description))
                .OnSuccess(async () =>
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    var state = remark.Value.GetLatestStateOf(RemarkState.Names.Renewed).Value;
                    var resource = _resourceFactory.Resolve<RemarkAssignedToGroup>(command.RemarkId);
                    await _bus.PublishAsync(new RemarkAssignedToGroup(command.Request.Id, resource, 
                        command.UserId, command.RemarkId, command.GroupId));
                })
                .OnCustomError(async ex => await _bus.PublishAsync(new AssignRemarkToGroupRejected(command.Request.Id,
                    command.UserId, ex.Code, ex.Message, command.RemarkId, command.GroupId)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while assigning a remark to group.");
                    await _bus.PublishAsync(new AssignRemarkToGroupRejected(command.Request.Id,
                        command.UserId, OperationCodes.Error, ex.Message, command.RemarkId, command.GroupId));
                })
                .ExecuteAsync();
    }
}