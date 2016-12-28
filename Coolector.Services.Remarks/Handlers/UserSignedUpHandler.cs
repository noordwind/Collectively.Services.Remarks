using System.Threading.Tasks;
using Coolector.Common.Events;
using Coolector.Services.Remarks.Services;
using Coolector.Services.Users.Shared.Events;
using NLog;

namespace Coolector.Services.Remarks.Handlers
{
    public class UserSignedUpHandler : IEventHandler<UserSignedUp>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IUserService _userService;

        public UserSignedUpHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task HandleAsync(UserSignedUp @event)
        {
            Logger.Debug($"Handle {nameof(UserSignedUp)} command, userId:{@event.UserId}, userName:{@event.Name}");
            await _userService.CreateIfNotFoundAsync(@event.UserId, @event.Name, @event.Role);
        }
    }
}