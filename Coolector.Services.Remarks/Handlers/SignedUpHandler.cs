using System.Threading.Tasks;
using Coolector.Common.Events;
using Coolector.Common.Services;
using Coolector.Services.Remarks.Services;
using Coolector.Services.Users.Shared.Events;

namespace Coolector.Services.Remarks.Handlers
{
    public class SignedUpHandler : IEventHandler<SignedUp>
    {
        private readonly IHandler _handler;
        private readonly IUserService _userService;

        public SignedUpHandler(IHandler handler, IUserService userService)
        {
            _handler = handler;
            _userService = userService;
        }

        public async Task HandleAsync(SignedUp @event)
        {
            await _handler
                .Run(async () => await _userService.CreateIfNotFoundAsync(@event.UserId, @event.Name, @event.Role))
                .ExecuteAsync();
        }
    }
}