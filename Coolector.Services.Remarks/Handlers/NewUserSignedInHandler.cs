using System.Threading.Tasks;
using Coolector.Common.Events;
using Coolector.Common.Events.Users;
using Coolector.Services.Remarks.Services;
using NLog;

namespace Coolector.Services.Remarks.Handlers
{
    public class NewUserSignedInHandler : IEventHandler<NewUserSignedIn>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IUserService _userService;

        public NewUserSignedInHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task HandleAsync(NewUserSignedIn @event)
        {
            Logger.Debug($"Handle {nameof(NewUserSignedIn)} command, userId:{@event.UserId}, userName:{@event.Name}");
            await _userService.CreateIfNotFoundAsync(@event.UserId, @event.Name);
        }
    }
}