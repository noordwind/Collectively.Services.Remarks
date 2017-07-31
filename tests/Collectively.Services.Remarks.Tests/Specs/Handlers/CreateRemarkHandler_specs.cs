using Collectively.Common.Files;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Handlers;
using Collectively.Services.Remarks.Policies;
using Collectively.Services.Remarks.Services;
using Machine.Specifications;
using Moq;
using RawRabbit;
using System;
using Collectively.Messages.Commands;
using Collectively.Common.Services;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using It = Machine.Specifications.It;
using RawRabbit.Configuration.Publish;
using System.Collections.Generic;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public abstract class CreateRemarkHandler_specs
    {
        protected static CreateRemarkHandler CreateRemarkHandler;
        protected static IHandler Handler;
        protected static Mock<IBusClient> BusClientMock;
        protected static Mock<IFileResolver> FileResolverMock;
        protected static Mock<IFileValidator> FileValidatorMock;
        protected static Mock<IRemarkService> RemarkServiceMock;
        protected static Mock<ISocialMediaService> SocialMediaServiceMock;
        protected static Mock<IExceptionHandler> ExceptionHandlerMock;
        protected static Mock<IResourceFactory> ResourceFactoryMock;
        protected static Mock<ICreateRemarkPolicy> CreateRemarkPolicyMock;
        protected static CreateRemark Command;
        protected static Exception Exception;

        protected static void Initialize()
        {
            ExceptionHandlerMock = new Mock<IExceptionHandler>();
            Handler = new Handler(ExceptionHandlerMock.Object);
            BusClientMock = new Mock<IBusClient>();
            FileResolverMock = new Mock<IFileResolver>();
            FileValidatorMock = new Mock<IFileValidator>();
            RemarkServiceMock = new Mock<IRemarkService>();
            SocialMediaServiceMock = new Mock<ISocialMediaService>();
            ResourceFactoryMock = new Mock<IResourceFactory>();
            CreateRemarkPolicyMock = new Mock<ICreateRemarkPolicy>();
            Command = new CreateRemark
            {
                RemarkId = Guid.NewGuid(),
                Request = new Request
                {
                    Name = "create_remark",
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.Now,
                    Origin = "test",
                    Resource = ""
                },
                UserId = "userId",
                Category = "litter",
                Longitude = 1,
                Latitude = 1,
                Description = "test",
                Address = "address",
                Photo = new Collectively.Messages.Commands.Models.File
                {
                    Base64 = "base64",
                    Name = "file.png",
                    ContentType = "image/png"
                }
            };
            CreateRemarkHandler = new CreateRemarkHandler(Handler, BusClientMock.Object, FileResolverMock.Object,
                FileValidatorMock.Object, RemarkServiceMock.Object, SocialMediaServiceMock.Object, 
                ResourceFactoryMock.Object, CreateRemarkPolicyMock.Object);
        }
    }

    [Subject("CreateRemarkHandler HandleAsync")]
    public class when_invoking_create_remark_handle_async : CreateRemarkHandler_specs
    {
        static Location Location;
        static File File;
        static Remark Remark;

        Establish context = () =>
        {
            Initialize();
            Location = Location.Create(Command.Latitude, Command.Longitude, Command.Address);
            File = File.Create(Command.Photo.Name, Command.Photo.ContentType, new byte[] { 0x1 });
            Remark = new Remark(Guid.NewGuid(), new User(Command.UserId, "user", "user"),
                new Category("test"), Location, Command.Description);
            FileResolverMock.Setup(x => x.FromBase64(Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns(File);
            FileValidatorMock.Setup(x => x.IsImage(Moq.It.IsAny<File>())).Returns(true);
            RemarkServiceMock.Setup(x => x.GetAsync(Moq.It.IsAny<Guid>())).ReturnsAsync(Remark);
        };

        Because of = () => CreateRemarkHandler.HandleAsync(Command).Await();

        It should_call_create_async_on_remark_service = () =>
        {
            RemarkServiceMock.Verify(x => x.CreateAsync(Moq.It.IsAny<Guid>(), Command.UserId,
                Command.Category, Location, Command.Description, Command.Tags, Command.GroupId), Times.Once);
        };

        It should_publish_remark_created_event = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.IsAny<RemarkCreated>(),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Once);
        };
    }

    [Subject("CreateRemarkHandler HandleAsync")]
    public class when_invoking_create_remark_handle_async_and_create_remark_fails : CreateRemarkHandler_specs
    {
        static Location Location;
        static File File;
        static Remark Remark;

        Establish context = () =>
        {
            Initialize();
            Location = Location.Create(Command.Latitude, Command.Longitude, Command.Address);
            File = File.Create(Command.Photo.Name, Command.Photo.ContentType, new byte[] { 0x1 });
            Remark = new Remark(Guid.NewGuid(), new User(Command.UserId, "user", "user"),
                new Category("test"), Location, Command.Description);
            FileResolverMock.Setup(x => x.FromBase64(Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns(File);
            FileValidatorMock.Setup(x => x.IsImage(Moq.It.IsAny<File>())).Returns(true);
            RemarkServiceMock.Setup(x => x.CreateAsync(Moq.It.IsAny<Guid>(), Moq.It.IsAny<string>(),
                    Moq.It.IsAny<string>(), Moq.It.IsAny<Location>(), Moq.It.IsAny<string>(),
                    Moq.It.IsAny<IEnumerable<string>>(), Moq.It.IsAny<Guid?>()))
                .Throws<Exception>();
        };

        Because of = () => CreateRemarkHandler.HandleAsync(Command).Await();

        It should_call_create_async_on_remark_service = () =>
        {
            RemarkServiceMock.Verify(x => x.CreateAsync(Moq.It.IsAny<Guid>(), Command.UserId,
                Command.Category, Location, Command.Description, Command.Tags, Command.GroupId), Times.Once);
        };

        It should_not_publish_remark_created_event = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.IsAny<RemarkCreated>(),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Never);
        };

        It should_publish_create_remark_rejected_message = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.Is<CreateRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.Error),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Once);
        };
    }
}