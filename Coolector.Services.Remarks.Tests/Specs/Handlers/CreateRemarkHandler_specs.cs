using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Handlers;
using Coolector.Services.Remarks.Services;
using Machine.Specifications;
using Moq;
using RawRabbit;
using System;
using Coolector.Common.Commands;
using Coolector.Common.Services;
using Coolector.Services.Remarks.Shared;
using Coolector.Services.Remarks.Shared.Commands;
using Coolector.Services.Remarks.Shared.Commands.Models;
using Coolector.Services.Remarks.Shared.Events;
using It = Machine.Specifications.It;
using RawRabbit.Configuration.Publish;

namespace Coolector.Services.Remarks.Tests.Specs.Handlers
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
        protected static CreateRemark Command;
        protected static Exception Exception;

        protected static void Initialize()
        {
            Handler = new Handler();
            BusClientMock = new Mock<IBusClient>();
            FileResolverMock = new Mock<IFileResolver>();
            FileValidatorMock = new Mock<IFileValidator>();
            RemarkServiceMock = new Mock<IRemarkService>();
            SocialMediaServiceMock = new Mock<ISocialMediaService>();
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
                Photo = new RemarkFile
                {
                    Base64 = "base64",
                    Name = "file.png",
                    ContentType = "image/png"
                }
            };
            CreateRemarkHandler = new CreateRemarkHandler(Handler, BusClientMock.Object, FileResolverMock.Object,
                FileValidatorMock.Object, RemarkServiceMock.Object, SocialMediaServiceMock.Object);
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
            Remark = new Remark(Guid.NewGuid(), new User(Command.UserId, "user"),
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
                Command.Category, Location, Command.Description), Times.Once);
        };

        It should_call_get_async_on_remark_service = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Moq.It.IsAny<Guid>()), Times.Once);
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
            Remark = new Remark(Guid.NewGuid(), new User(Command.UserId, "user"),
                new Category("test"), Location, Command.Description);
            FileResolverMock.Setup(x => x.FromBase64(Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns(File);
            FileValidatorMock.Setup(x => x.IsImage(Moq.It.IsAny<File>())).Returns(true);
            RemarkServiceMock.Setup(x => x.CreateAsync(Moq.It.IsAny<Guid>(), Moq.It.IsAny<string>(),
                    Moq.It.IsAny<string>(), Moq.It.IsAny<Location>(), Moq.It.IsAny<string>()))
                .Throws<Exception>();
        };

        Because of = () => CreateRemarkHandler.HandleAsync(Command).Await();

        It should_call_create_async_on_remark_service = () =>
        {
            RemarkServiceMock.Verify(x => x.CreateAsync(Moq.It.IsAny<Guid>(), Command.UserId,
                Command.Category, Location, Command.Description), Times.Once);
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