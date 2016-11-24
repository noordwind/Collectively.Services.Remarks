using System;
using System.Threading.Tasks;
using Coolector.Common.Domain;
using Coolector.Common.Events;
using Coolector.Common.Events.Users;
using Coolector.Services.Remarks.Services;
using NLog;

namespace Coolector.Services.Remarks.Handlers
{
    public class UserNameChangedHandler : IEventHandler<UserNameChanged>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IUserService _userService;
        private readonly IRemarkService _remarkService;

        public UserNameChangedHandler(IUserService userService, IRemarkService remarkService)
        {
            _userService = userService;
            _remarkService = remarkService;
        }

        public async Task HandleAsync(UserNameChanged @event)
        {
            Logger.Debug($"Handle {nameof(UserNameChanged)} event, userId: {@event.UserId}, newName: {@event.NewName}");
            try
            {
                await _userService.UpdateNameAsync(@event.UserId, @event.NewName);
                await _remarkService.UpdateUserNamesAsync(@event.UserId, @event.NewName);
            }
            catch(ArgumentException ex)
            {
                Logger.Error(ex);
            }
        }
    }
}