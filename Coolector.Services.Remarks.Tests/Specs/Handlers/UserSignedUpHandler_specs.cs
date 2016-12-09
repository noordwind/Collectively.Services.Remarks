using System;
using It = Machine.Specifications.It;
using Moq;
using Coolector.Services.Remarks.Services;
using Coolector.Services.Remarks.Handlers;
using Coolector.Services.Users.Shared.Events;
using Machine.Specifications;

namespace Coolector.Services.Remarks.Tests.Specs.Handlers
{
    public abstract class UserSignedUpHandler_specs
    {
        protected static UserSignedUpHandler Handler;
        protected static Mock<IUserService> UserServiceMock;
        protected static UserSignedUp Event;
        protected static Exception Exception;

        protected static void Initialize()
        {
            UserServiceMock = new Mock<IUserService>();
            Event = new UserSignedUp(Guid.NewGuid(), "user", "user@email.com", "name",
                "picture", "user", "active", "coolector", string.Empty, DateTime.UtcNow);
            Handler = new UserSignedUpHandler(UserServiceMock.Object);
        }
    }

    [Subject("UserSignedUpHandler HandleAsync")]
    public class when_invoking_user_signed_up_handle_async : UserSignedUpHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
        };

        Because of = () => Handler.HandleAsync(Event).Await();

        It should_call_create_if_not_found_async_on_user_service = () =>
        {
            UserServiceMock.Verify(x => x.CreateIfNotFoundAsync(Event.UserId, Event.Name), Times.Once);
        };
    }
}