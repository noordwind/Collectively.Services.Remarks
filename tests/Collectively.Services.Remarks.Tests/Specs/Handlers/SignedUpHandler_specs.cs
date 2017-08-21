using System;
using It = Machine.Specifications.It;
using Moq;
using Collectively.Services.Remarks.Services;
using Collectively.Services.Remarks.Handlers;
using Collectively.Messages.Events.Users;
using Machine.Specifications;
using Collectively.Common.Services;
using Collectively.Common.ServiceClients;
using Collectively.Messages.Events;
using Collectively.Services.Remarks.Dto;

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
        protected static Mock<IServiceClient> ServiceClient;
        protected static UserDto User;
        protected static void Initialize()
        {
            ExceptionHandlerMock = new Mock<IExceptionHandler>();
            Handler = new Handler(ExceptionHandlerMock.Object);
            UserServiceMock = new Mock<IUserService>();
            User = new UserDto
            {
                Name = "user",
                Role = "user"
            };
            ServiceClient = new Mock<IServiceClient>();
            Event = new SignedUp(Guid.NewGuid(), Resource.Create("test", "test"), 
                Guid.NewGuid().ToString("N"), "test", "user", "active");
            ServiceClient.Setup(x => x.GetAsync<UserDto>(Event.Resource))
                .ReturnsAsync(User);
            SignedUpHandler = new SignedUpHandler(Handler, UserServiceMock.Object, ServiceClient.Object);
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
            UserServiceMock.Verify(x => x.CreateIfNotFoundAsync(Event.UserId, 
                User.Name, User.Role), Times.Once);
        };
    }
}