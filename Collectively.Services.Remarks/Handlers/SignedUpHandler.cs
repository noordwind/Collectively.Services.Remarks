using System.Threading.Tasks;
using Collectively.Messages.Events;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Services;
using Collectively.Messages.Events.Users;
using Collectively.Common.ServiceClients.Users;
using Newtonsoft.Json;
using System;
using Collectively.Services.Remarks.Dto;

namespace Collectively.Services.Remarks.Handlers
{
    public class SignedUpHandler : IEventHandler<SignedUp>
    {
        private readonly IHandler _handler;
        private readonly IUserService _userService;
        private readonly IUserServiceClient _userServiceClient;

        public SignedUpHandler(IHandler handler, IUserService userService, 
            IUserServiceClient userServiceClient)
        {
            _handler = handler;
            _userService = userService;
            _userServiceClient = userServiceClient;
        }

        public async Task HandleAsync(SignedUp @event)
        {
            var user = await _userServiceClient.GetAsync<UserDto>(@event.UserId);
            await _handler
                .Run(async () => await _userService.CreateIfNotFoundAsync(@event.UserId, user.Value.Name, user.Value.Role))
                .ExecuteAsync();
        }
    }
}