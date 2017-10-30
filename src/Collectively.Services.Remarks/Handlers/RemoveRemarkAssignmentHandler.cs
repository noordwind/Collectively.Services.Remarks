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
    public class RemoveRemarkAssignmentHandler : ICommandHandler<RemoveRemarkAssignment>
    {
        private static readonly ILogger Logger = Log.Logger;
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;
        private readonly IGroupService _groupService;
        private readonly IRemarkStateService _remarkStateService;
        private readonly IResourceFactory _resourceFactory;

        public RemoveRemarkAssignmentHandler(IHandler handler,
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

        public async Task HandleAsync(RemoveRemarkAssignment command)
        {
            var assignee = "";
            await _handler
                .Validate(async () => 
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    assignee = remark.Value.Assignee;
                    if (remark.Value.Group == null)
                    {
                        return;
                    }
                    await _groupService.ValidateIfRemarkAssignmentCanBeRemovedOrFailAsync(remark.Value.Group.Id,
                        command.UserId);
                })
                .Run(async () => await _remarkStateService.RemoveAssignmentAsync(command.RemarkId, 
                        command.UserId, command.Description))
                .OnSuccess(async () =>
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    var state = remark.Value.GetLatestStateOf(RemarkState.Names.Renewed).Value;
                    var resource = _resourceFactory.Resolve<RemarkAssignmentRemoved>(command.RemarkId);
                    await _bus.PublishAsync(new RemarkAssignmentRemoved(command.Request.Id, resource, 
                        command.UserId, command.RemarkId, assignee));
                })
                .OnCustomError(async ex => await _bus.PublishAsync(new RemoveRemarkAssignmentRejected(command.Request.Id,
                    command.UserId, ex.Code, ex.Message, command.RemarkId, assignee)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while removing a remark assignment.");
                    await _bus.PublishAsync(new RemoveRemarkAssignmentRejected(command.Request.Id,
                        command.UserId, OperationCodes.Error, ex.Message, command.RemarkId, assignee));
                })
                .ExecuteAsync();
        }
    }
}