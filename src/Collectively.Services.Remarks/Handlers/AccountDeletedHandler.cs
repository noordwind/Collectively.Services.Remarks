using System.Threading.Tasks;
using Collectively.Messages.Events;
using Collectively.Common.Domain;
using Collectively.Common.Services;
using Collectively.Messages.Events.Users;
using Collectively.Services.Remarks.Services;

namespace Collectively.Services.Remarks.Handlers
{
    public class AccountDeletedHandler: IEventHandler<AccountDeleted>
    {
        private readonly IHandler _handler;
        private readonly IUserService _userService;

        public AccountDeletedHandler(IHandler handler, IUserService userService)
        {
            _handler = handler;
            _userService = userService;
        }

        public async Task HandleAsync(AccountDeleted @event)
        {
            await _handler
                .Run(async () =>await _userService.DeleteAsync(@event.UserId))
                .OnError((ex, logger) =>
                {
                    logger.Error(ex, $"Error occured while handling {@event.GetType().Name} event");
                })
                .ExecuteAsync();
        }
    }
}