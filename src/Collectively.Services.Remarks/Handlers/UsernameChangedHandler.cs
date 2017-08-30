using System;
using System.Threading.Tasks;
using Collectively.Messages.Events;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Services;
using Collectively.Messages.Events.Users;
using Serilog;

namespace Collectively.Services.Remarks.Handlers
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