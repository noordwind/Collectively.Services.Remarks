using System;
using Collectively.Common.Files;
using Collectively.Common.Services;
using Collectively.Messages.Commands;
using RawRabbit;
using Moq;
using Collectively.Services.Remarks.Services;
using Collectively.Services.Remarks.Domain;
using Collectively.Messages.Commands.Remarks;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public abstract class ChangeRemarkStateBase_specs<TCommand,THandler> 
        where TCommand : ChangeRemarkStateBase, new()
        where THandler : ICommandHandler<TCommand>
    {
        protected static THandler CommandHandler;
        protected static IHandler Handler;
        protected static Mock<IBusClient> BusClientMock;
        protected static Mock<IRemarkService> RemarkServiceMock;
        protected static Mock<IRemarkStateService> RemarkStateServiceMock;
        protected static Mock<IFileResolver> FileResolverMock;
        protected static Mock<IFileValidator> FileValidatorMock;
        protected static Mock<IExceptionHandler> ExceptionHandlerMock;
        protected static Mock<IResourceFactory> ResourceFactoryMock;
        protected static TCommand Command;
        protected static string UserId = "UserId";
        protected static Guid RemarkId = Guid.NewGuid();
        protected static File File;
        protected static Remark Remark;
        protected static Location Location;
        protected static User User;
        protected static Category Category;
        protected static string Description;
        protected static Exception Exception;

        protected static void InitializeBase()
        {
            ExceptionHandlerMock = new Mock<IExceptionHandler>();
            Handler = new Handler(ExceptionHandlerMock.Object);
            BusClientMock = new Mock<IBusClient>();
            RemarkServiceMock = new Mock<IRemarkService>();
            RemarkStateServiceMock = new Mock<IRemarkStateService>();
            FileResolverMock = new Mock<IFileResolver>();
            FileValidatorMock = new Mock<IFileValidator>();
            ResourceFactoryMock = new Mock<IResourceFactory>();
            Description = "test";
            Command = new TCommand()
            {
                Request = new Request
                {
                    Name = typeof(TCommand).Name,
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.Now,
                    Origin = "test",
                    Resource = ""
                },
                Description = Description,
                RemarkId = RemarkId,
                UserId = UserId,
                Longitude = 1,
                Latitude = 1,
            };
            User = new User(UserId, "user", "user");
            Category = new Category("test");
            Location = Location.Create(Command.Latitude, Command.Longitude, "address");
            Remark = new Remark(RemarkId, User, Category, Location, Description);
            RemarkServiceMock.Setup(x => x.GetAsync(Moq.It.IsAny<Guid>())).ReturnsAsync(Remark);
        }
    }
}