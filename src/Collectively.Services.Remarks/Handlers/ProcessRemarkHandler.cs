using System.Threading.Tasks;
using Collectively.Messages.Commands;
using Collectively.Common.Files;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Policies;
using Collectively.Services.Remarks.Services;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Serilog;
using RawRabbit;
using RemarkState = Collectively.Services.Remarks.Domain.RemarkState;

namespace Collectively.Services.Remarks.Handlers
{
    public class ProcessRemarkHandler : ICommandHandler<ProcessRemark>
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
        private readonly IProcessRemarkPolicy _policy;
        private readonly IRemarkActionService _remarkActionService;

        public ProcessRemarkHandler(IHandler handler,
            IBusClient bus,
            IRemarkService remarkService,
            IGroupService groupService,
            IRemarkStateService remarkStateService,
            IFileResolver fileResolver,
            IFileValidator fileValidator,
            IResourceFactory resourceFactory,
            IProcessRemarkPolicy policy,
            IRemarkActionService remarkActionService)
        {
            _handler = handler;
            _bus = bus;
            _remarkService = remarkService;
            _groupService = groupService;
            _remarkStateService = remarkStateService;
            _fileResolver = fileResolver;
            _fileValidator = fileValidator;
            _resourceFactory = resourceFactory;
            _policy = policy;
            _remarkActionService = remarkActionService;
        }

        public async Task HandleAsync(ProcessRemark command)
        {
            var remarkProcessed = false;
            await _handler
                .Validate(async () => 
                {
                    await _policy.ValidateAsync(command.RemarkId, command.UserId);
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    if(remark.Value.Group == null)
                    {
                        return;
                    }
                    await _groupService.ValidateIfRemarkCanBeProcessedOrFailAsync(remark.Value.Group.Id, command.UserId);
                }) 
                .Run(async () =>
                {
                    Location location = null;
                    if (command.Latitude != 0 && command.Longitude != 0)
                    {
                        location = Location.Create(command.Latitude, command.Longitude, command.Address);
                    }
                    await _remarkStateService.ProcessAsync(command.RemarkId, command.UserId, command.Description, location);
                })
                .OnSuccess(async () =>
                {
                    remarkProcessed = true;
                    var remark = await _remarkService.GetAsync(command.RemarkId);
                    var state = remark.Value.GetLatestStateOf(RemarkState.Names.Processing).Value;
                    var resource = _resourceFactory.Resolve<RemarkProcessed>(command.RemarkId);
                    await _bus.PublishAsync(new RemarkProcessed(command.Request.Id, resource, 
                        command.UserId, command.RemarkId));
                })
                .OnCustomError(async ex => await _bus.PublishAsync(new ProcessRemarkRejected(command.Request.Id,
                    command.UserId, command.RemarkId, ex.Code, ex.Message)))
                .OnError(async (ex, logger) =>
                {
                    logger.Error(ex, "Error occured while processing a remark.");
                    await _bus.PublishAsync(new ProcessRemarkRejected(command.Request.Id,
                        command.UserId, command.RemarkId, OperationCodes.Error, ex.Message));
                })
                .Next()
                .Run(async () => 
                {
                    if(!remarkProcessed)
                    {
                        return;
                    }

                    var participant = await _remarkActionService.GetParticipantAsync(command.RemarkId, command.UserId);
                    if(participant.HasValue)
                    {
                        return;
                    }
                    var takeRemarkAction = new TakeRemarkAction
                    {
                        Request = Messages.Commands.Request.From<TakeRemarkAction>(command.Request),
                        UserId = command.UserId,
                        RemarkId = command.RemarkId,
                        Description = command.Description
                    };
                    await _bus.PublishAsync(takeRemarkAction);
                })              
                .Next()
                .ExecuteAllAsync();
        }
    }
}