using System;
using It = Machine.Specifications.It;
using Moq;
using Collectively.Services.Remarks.Services;
using Collectively.Services.Remarks.Handlers;
using Collectively.Messages.Events.Users;
using Machine.Specifications;
using Collectively.Common.Services;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public abstract class SignedUpHandler_specs
    {
        protected static IHandler Handler;
        protected static SignedUpHandler SignedUpHandler;
        protected static Mock<IUserService> UserServiceMock;
        protected static Mock<IExceptionHandler> ExceptionHandlerMock;
        protected static SignedUp Event;
        protected static Exception Exception;

        protected static void Initialize()
        {
            ExceptionHandlerMock = new Mock<IExceptionHandler>();
            Handler = new Handler(ExceptionHandlerMock.Object);
            UserServiceMock = new Mock<IUserService>();
            Event = new SignedUp(Guid.NewGuid(), "user", "user@email.com", "name",
                "picture", "user", "active", "collectively", string.Empty, DateTime.UtcNow);
            SignedUpHandler = new SignedUpHandler(Handler, UserServiceMock.Object);
        }
    }

    [Subject("UserSignedUpHandler HandleAsync")]
    public class when_invoking_user_signed_up_handle_async : SignedUpHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
        };

        Because of = () => SignedUpHandler.HandleAsync(Event).Await();

        It should_call_create_if_not_found_async_on_user_service = () =>
        {
            UserServiceMock.Verify(x => x.CreateIfNotFoundAsync(Event.UserId, Event.Name, Event.Role), Times.Once);
        };
    }
}