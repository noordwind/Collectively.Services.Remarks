using System.Threading.Tasks;
using Coolector.Common;
using Coolector.Common.Commands;
using Coolector.Common.Commands.Remarks;
using Coolector.Common.Domain;
using Coolector.Common.Events.Remarks;
using Coolector.Services.Remarks.Services;
using NLog;
using RawRabbit;

namespace Coolector.Services.Remarks.Handlers
{
    public class DeleteRemarkHandler : ICommandHandler<DeleteRemark>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IBusClient _bus;
        private readonly IRemarkService _remarkService;

        public DeleteRemarkHandler(IBusClient bus, IRemarkService remarkService)
        {
            _bus = bus;
            _remarkService = remarkService;
        }

        public async Task HandleAsync(DeleteRemark command)
        {
            try
            {
                Logger.Debug($"Handle {nameof(DeleteRemark)} command, remarkId:{command.RemarkId}, userId:{command.UserId}");
                await _remarkService.DeleteAsync(command.RemarkId, command.UserId);
                await _bus.PublishAsync(new RemarkDeleted(command.Request.Id, command.RemarkId, command.UserId));
            }
            catch (ServiceException ex)
            {
                Logger.Error(ex);
                await _bus.PublishAsync(new DeleteRemarkRejected(command.Request.Id,
                    command.RemarkId, command.UserId, OperationCodes.Error, ex.Message));
            }
        }
    }
}