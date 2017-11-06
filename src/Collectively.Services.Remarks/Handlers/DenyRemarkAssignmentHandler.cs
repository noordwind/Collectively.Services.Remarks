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
using System;

namespace Collectively.Services.Remarks.Handlers
{
    public class DenyRemarkAssignmentHandler : ICommandHandler<DenyRemarkAssignment>
    {
        private static readonly ILogger Logger = Log.Logger;
        private readonly IHandler _handler;
        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;
        private readonly IResourceFactory _resourceFactory;

        public DenyRemarkAssignmentHandler(IHandler handler,
            IBusClient bus,
            IRemarkService remarkService,
            IResourceFactory resourceFactory)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
            _resourceFactory = resourceFactory;
        }

        public async Task HandleAsync(DenyRemarkAssignment command)
            => await _handler       
                .Run(async () => 
                {
                    if (command.GroupId.HasValue && command.GroupId != Guid.Empty)
                    {
                        await _remarkService.DenyAssignmentToGroupAsync(command.RemarkId, 
                            command.GroupId.Value, command.UserId);
                    }
                })
                .OnSuccess(async () =>
                {
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    var resource = _resourceFactory.Resolve<RemarkAssignedToGroup>(command.RemarkId);
                    await _bus.PublishAsync(new RemarkAssignmentDenied(command.Request.Id, resource, 
                        command.UserId, command.RemarkId, command.GroupId));
                })
                .OnCustomError(async ex => await _bus.PublishAsync(new DenyRemarkAssignmentRejected(command.Request.Id,
                    command.UserId, ex.Code, ex.Message, command.RemarkId, command.GroupId)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while denying a remark assignment.");
                    await _bus.PublishAsync(new DenyRemarkAssignmentRejected(command.Request.Id,
                        command.UserId, OperationCodes.Error, ex.Message, command.RemarkId, command.GroupId));
                })
                .ExecuteAsync();
    }
}