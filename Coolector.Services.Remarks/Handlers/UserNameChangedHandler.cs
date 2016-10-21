using System.Threading.Tasks;
using Coolector.Common.Events;
using Coolector.Common.Events.Users;
using Coolector.Services.Remarks.Services;

namespace Coolector.Services.Remarks.Handlers
{
    public class UserNameChangedHandler : IEventHandler<UserNameChanged>
    {
        private readonly IUserService _userService;
        private readonly IRemarkService _remarkService;

        public UserNameChangedHandler(IUserService userService, IRemarkService remarkService)
        {
            _userService = userService;
            _remarkService = remarkService;
        }

        public async Task HandleAsync(UserNameChanged @event)
        {
            await _userService.UpdateNameAsync(@event.UserId, @event.NewName);
            await _remarkService.UpdateUserNamesAsync(@event.UserId, @event.NewName);
        }
    }
}