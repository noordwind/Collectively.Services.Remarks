using System;
using System.Threading.Tasks;
using Coolector.Common.Events;
using Coolector.Common.Services;
using Coolector.Services.Remarks.Services;
using Coolector.Services.Users.Shared.Events;
using NLog;

namespace Coolector.Services.Remarks.Handlers
{
    public class UsernameChangedHandler : IEventHandler<UsernameChanged>
    {
        private readonly IHandler _handler;
        private readonly IUserService _userService;
        private readonly IRemarkService _remarkService;

        public UsernameChangedHandler(IHandler handler, IUserService userService, IRemarkService remarkService)
        {
            _handler = handler;
            _userService = userService;
            _remarkService = remarkService;
        }

        public async Task HandleAsync(UsernameChanged @event)
        {
            await _handler
                .Run(async () => 
                {
                    await _userService.UpdateNameAsync(@event.UserId, @event.NewName);
                    await _remarkService.UpdateUserNamesAsync(@event.UserId, @event.NewName);
                })
                .ExecuteAsync();
        }
    }
}