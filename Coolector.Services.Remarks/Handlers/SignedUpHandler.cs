using System.Threading.Tasks;
using Coolector.Common.Events;
using Coolector.Services.Remarks.Services;
using Coolector.Services.Users.Shared.Events;
using NLog;

namespace Coolector.Services.Remarks.Handlers
{
    public class SignedUpHandler : IEventHandler<SignedUp>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IUserService _userService;

        public SignedUpHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task HandleAsync(SignedUp @event)
        {
            Logger.Debug($"Handle {nameof(SignedUp)} command, userId:{@event.UserId}, userName:{@event.Name}");
            await _userService.CreateIfNotFoundAsync(@event.UserId, @event.Name, @event.Role);
        }
    }
}