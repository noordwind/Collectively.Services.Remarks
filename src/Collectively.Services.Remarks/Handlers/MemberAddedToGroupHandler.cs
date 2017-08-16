using System.Threading.Tasks;
using Collectively.Messages.Events;
using Collectively.Common.Services;
using Collectively.Messages.Events.Groups;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Repositories;
using Collectively.Common.ServiceClients;
using Collectively.Services.Remarks.Dto;
using Collectively.Services.Remarks.Services;

namespace Collectively.Services.Remarks.Handlers
{
    public class MemberAddedToGroupHandler : IEventHandler<MemberAddedToGroup>
    {
        private readonly IHandler _handler;
        private readonly IGroupService _groupService;

        public MemberAddedToGroupHandler(IHandler handler, 
            IGroupService groupService)
        {
            _handler = handler;
            _groupService = groupService;
        }

        public async Task HandleAsync(MemberAddedToGroup @event)
        {
            await _handler
                .Run(async () =>
                    await _groupService.AddMemberAsync(@event.GroupId, 
                        @event.MemberId, @event.Role))
                .OnError((ex, logger) =>
                {
                    logger.Error(ex, $"Error occured while handling {@event.GetType().Name} event");
                })
                .ExecuteAsync();
        }
    }
}