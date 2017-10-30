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
    public class GroupCreatedHandler : IEventHandler<GroupCreated>
    {
        private readonly IHandler _handler;
        private readonly IGroupService _groupService;
        private readonly IServiceClient _serviceClient;

        public GroupCreatedHandler(IHandler handler, 
            IGroupService groupService,
            IServiceClient serviceClient)
        {
            _handler = handler;
            _groupService = groupService;
            _serviceClient = serviceClient;
        }

        public async Task HandleAsync(GroupCreated @event)
        {
            await _handler
                .Run(async () =>
                {
                    var maybeGroup = await _serviceClient.GetAsync<GroupDto>(@event.Resource);
                    var group = maybeGroup.Value;
                    await _groupService.CreateIfNotFoundAsync(group.Id, group.Name,
                        group.IsPublic, group.State, @event.UserId, group.Criteria, 
                        group.Tags, @event.OrganizationId);
                })
                .OnError((ex, logger) =>
                {
                    logger.Error(ex, $"Error occured while handling {@event.GetType().Name} event");
                })
                .ExecuteAsync();
        }
    }
}