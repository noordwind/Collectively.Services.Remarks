using System;
using Collectively.Common;
using Collectively.Messages.Commands;
using Collectively.Common.Domain;
using Collectively.Common.Services;
using Collectively.Common.Types;
using It = Machine.Specifications.It;
using RawRabbit;
using Moq;
using Collectively.Services.Remarks.Services;
using Collectively.Services.Remarks.Handlers;
using Machine.Specifications;
using RawRabbit.Configuration.Publish;
using Collectively.Services.Remarks.Domain;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Commands.Remarks.Models;
using Collectively.Messages.Events.Remarks;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public class ResolveRemarkHandler_specs
    {
        protected static ResolveRemarkHandler ResolveRemarkHandler;
        protected static IHandler Handler;
        protected static Mock<IBusClient> BusClientMock;
        protected static Mock<IRemarkService> RemarkServiceMock;
        protected static Mock<IFileResolver> FileResolverMock;
        protected static Mock<IFileValidator> FileValidatorMock;
        protected static Mock<IExceptionHandler> ExceptionHandlerMock;
        protected static Mock<IResourceFactory> ResourceFactoryMock;
        protected static ResolveRemark Command;
        protected static string UserId = "UserId";
        protected static Guid RemarkId = Guid.NewGuid();
        protected static File File;
        protected static Remark Remark;
        protected static Location Location;
        protected static User User;
        protected static Category Category;

        protected static Exception Exception;

        protected static void Initialize()
        {
            ExceptionHandlerMock = new Mock<IExceptionHandler>();
            Handler = new Handler(ExceptionHandlerMock.Object);
            BusClientMock = new Mock<IBusClient>();
            RemarkServiceMock = new Mock<IRemarkService>();
            FileResolverMock = new Mock<IFileResolver>();
            FileValidatorMock = new Mock<IFileValidator>();
            ResourceFactoryMock = new Mock<IResourceFactory>();

            ResolveRemarkHandler = new ResolveRemarkHandler(Handler,
                BusClientMock.Object, 
                RemarkServiceMock.Object,
                FileResolverMock.Object,
                FileValidatorMock.Object,
                ResourceFactoryMock.Object);

            Command = new ResolveRemark
            {
                Request = new Request
                {
                    Name = "resolve_remark",
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.Now,
                    Origin = "test",
                    Resource = ""
                },
                RemarkId = RemarkId,
                UserId = UserId,
                Longitude = 1,
                Latitude = 1,
                Photo = new RemarkFile
                {
                    Base64 = "base64",
                    Name = "file.png",
                    ContentType = "image/png"
                },
                ValidatePhoto = true,
                ValidateLocation = true
            };

            File = File.Create(Command.Photo.Name, Command.Photo.ContentType, new byte[] { 0x1 });
            User = new User(UserId, "user", "user");
            Category = new Category("test");
            Location = Location.Create(Command.Latitude, Command.Longitude, "address");
            Remark = new Remark(RemarkId, User, Category, Location, "description");
            Remark.SetResolvedState(User, Location);

            FileResolverMock.Setup(x => x.FromBase64(Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns(File);
            FileValidatorMock.Setup(x => x.IsImage(Moq.It.IsAny<File>())).Returns(true);
            RemarkServiceMock.Setup(x => x.GetAsync(Moq.It.IsAny<Guid>())).ReturnsAsync(Remark);
        }
    }

    [Subject("ResolveRemarkHandler HandleAsync")]
    public class when_invoking_resolve_remark_handle_async : ResolveRemarkHandler_specs
    {
        Establish context = () => Initialize();

        Because of = () => ResolveRemarkHandler.HandleAsync(Command).Await();

        It should_resolve_file_from_base64 = () =>
        {
            FileResolverMock.Verify(x => x.FromBase64(Command.Photo.Base64, Command.Photo.Name, Command.Photo.ContentType), Times.Once);
        };

        It should_validate_image = () =>
        {
            FileValidatorMock.Verify(x => x.IsImage(File), Times.Once);
        };

        It should_resolve_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.ResolveAsync(Command.RemarkId, Command.UserId, File, Location, true), Times.Once);
        };

        It should_fetch_resolved_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Once);
        };

        It should_publish_remark_resolved_event = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.IsAny<RemarkResolved>(), 
                Moq.It.IsAny<Guid>(), 
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Once);
        };
    }

    [Subject("ResolveRemarkHandler HandleAsync")]
    public class when_when_invoking_resolve_remark_handle_async_and_file_cannot_be_resolved : ResolveRemarkHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
            FileResolverMock.Setup(x => x.FromBase64(
                Moq.It.IsAny<string>(), 
                Moq.It.IsAny<string>(), 
                Moq.It.IsAny<string>()))
                .Returns(new Maybe<File>());
        };

        Because of = () => ResolveRemarkHandler.HandleAsync(Command).Await();

        It should_resolve_file_from_base64 = () =>
        {
            FileResolverMock.Verify(x => x.FromBase64(Command.Photo.Base64, Command.Photo.Name, Command.Photo.ContentType), Times.Once);
        };

        It should_not_validate_image = () =>
        {
            FileValidatorMock.Verify(x => x.IsImage(File), Times.Never);
        };

        It should_not_resolve_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.ResolveAsync(Command.RemarkId, Command.UserId, File, Location, false), Times.Never);
        };

        It should_not_fetch_resolved_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Never);
        };

        It should_not_publish_remark_resolved_event = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.IsAny<RemarkResolved>(),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Never);
        };

        It should_publish_resolve_remark_rejected_message = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.Is<ResolveRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.CannotConvertFile),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Once);
        };
    }

    [Subject("ResolveRemarkHandler HandleAsync")]
    public class when_invoking_resolve_remark_handle_async_and_file_is_not_an_image : ResolveRemarkHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
            FileValidatorMock.Setup(x => x.IsImage(Moq.It.IsAny<File>()))
                .Returns(false);
        };

        Because of = () => ResolveRemarkHandler.HandleAsync(Command).Await();

        It should_resolve_file_from_base64 = () =>
        {
            FileResolverMock.Verify(x => x.FromBase64(Command.Photo.Base64, Command.Photo.Name, Command.Photo.ContentType), Times.Once);
        };

        It should_validate_image = () =>
        {
            FileValidatorMock.Verify(x => x.IsImage(File), Times.Once);
        };

        It should_not_resolve_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.ResolveAsync(Command.RemarkId, Command.UserId, File, Location, true), Times.Never);
        };

        It should_not_fetch_resolved_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Never);
        };

        It should_not_publish_remark_resolved_event = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.IsAny<RemarkResolved>(),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Never);
        };

        It should_publish_resolve_remark_rejected_message = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.Is<ResolveRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.InvalidFile),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Once);
        };
    }

    [Subject("ResolveRemarkHandler HandleAsync")]
    public class when_invoking_resolve_remark_handle_async_and_latitude_is_corrupt : ResolveRemarkHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
            Command.Latitude = 100;
        };

        Because of = () => ResolveRemarkHandler.HandleAsync(Command).Await();

        It should_resolve_file_from_base64 = () =>
        {
            FileResolverMock.Verify(x => x.FromBase64(Command.Photo.Base64, Command.Photo.Name, Command.Photo.ContentType), Times.Once);
        };

        It should_validate_image = () =>
        {
            FileValidatorMock.Verify(x => x.IsImage(File), Times.Once);
        };

        It should_not_resolve_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.ResolveAsync(Command.RemarkId, Command.UserId, File, Location, true), Times.Never);
        };

        It should_not_fetch_resolved_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Never);
        };

        It should_not_publish_remark_resolved_event = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.IsAny<RemarkResolved>(),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Never);
        };

        It should_publish_resolve_remark_rejected_message = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.Is<ResolveRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.Error),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Once);
        };
    }

    [Subject("ResolveRemarkHandler HandleAsync")]
    public class when_invoking_resolve_remark_handle_async_and_longitude_is_corrupt : ResolveRemarkHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
            Command.Longitude = 200;
        };

        Because of = () => ResolveRemarkHandler.HandleAsync(Command).Await();

        It should_resolve_file_from_base64 = () =>
        {
            FileResolverMock.Verify(x => x.FromBase64(Command.Photo.Base64, Command.Photo.Name, Command.Photo.ContentType), Times.Once);
        };

        It should_validate_image = () =>
        {
            FileValidatorMock.Verify(x => x.IsImage(File), Times.Once);
        };

        It should_not_resolve_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.ResolveAsync(Command.RemarkId, Command.UserId, File, Location, true), Times.Never);
        };

        It should_not_fetch_resolved_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Never);
        };

        It should_not_publish_remark_resolved_event = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.IsAny<RemarkResolved>(),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Never);
        };

        It should_publish_resolve_remark_rejected_message = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.Is<ResolveRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.Error),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Once);
        };
    }

    [Subject("ResolveRemarkHandler HandleAsync")]
    public class when_invoking_resolve_remark_handle_async_and_resolve_async_fails : ResolveRemarkHandler_specs
    {
        protected static string ErrorCode = "Error"; 

        Establish context = () =>
        {
            Initialize();
            RemarkServiceMock.Setup(x => x.ResolveAsync(Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<File>(),
                Moq.It.IsAny<Location>(),
                Moq.It.IsAny<bool>())).Throws(new ServiceException(ErrorCode));
        };

        Because of = () => ResolveRemarkHandler.HandleAsync(Command).Await();

        It should_resolve_file_from_base64 = () =>
        {
            FileResolverMock.Verify(x => x.FromBase64(Command.Photo.Base64, Command.Photo.Name, Command.Photo.ContentType), Times.Once);
        };

        It should_validate_image = () =>
        {
            FileValidatorMock.Verify(x => x.IsImage(File), Times.Once);
        };

        It should_resolve_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.ResolveAsync(Command.RemarkId, Command.UserId, File, Location, true), Times.Once);
        };

        It should_not_fetch_resolved_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Never);
        };

        It should_not_publish_remark_resolved_event = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.IsAny<RemarkResolved>(),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Never);
        };

        It should_publish_resolve_remark_rejected_message = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.Is<ResolveRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == ErrorCode),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Once);
        };
    }
}