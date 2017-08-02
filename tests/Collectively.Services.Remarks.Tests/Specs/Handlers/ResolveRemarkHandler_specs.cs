using System;
using Collectively.Common.Files;
using Collectively.Common.Types;
using It = Machine.Specifications.It;
using Moq;
using Collectively.Services.Remarks.Handlers;
using Machine.Specifications;
using RawRabbit.Configuration.Publish;
using Collectively.Services.Remarks.Domain;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public class ResolveRemarkHandler_specs : ChangeRemarkStateBase_specs<ResolveRemark, ResolveRemarkHandler>
    {
        protected static void Initialize()
        {
            InitializeBase();
            CommandHandler = new ResolveRemarkHandler(Handler,
                BusClientMock.Object, 
                RemarkServiceMock.Object,
                GroupServiceMock.Object,
                RemarkStateServiceMock.Object,
                FileResolverMock.Object,
                FileValidatorMock.Object,
                ResourceFactoryMock.Object);
            Command.Photo = new Collectively.Messages.Commands.Models.File
            {
                Base64 = "base64",
                Name = "file.png",
                ContentType = "image/png"
            };
            Command.ValidateLocation = true;
            Command.ValidatePhoto = true;
            Remark.SetResolvedState(User, Description, Location);
            File = File.Create(Command.Photo.Name, Command.Photo.ContentType, new byte[] { 0x1 });
            FileResolverMock.Setup(x => x.FromBase64(Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns(File);
            FileValidatorMock.Setup(x => x.IsImage(Moq.It.IsAny<File>())).Returns(true);
        }
    }

    [Subject("ResolveRemarkHandler HandleAsync")]
    public class when_invoking_resolve_remark_handle_async : ResolveRemarkHandler_specs
    {
        Establish context = () => Initialize();

        Because of = () => CommandHandler.HandleAsync(Command).Await();

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
            RemarkStateServiceMock.Verify(x => x.ResolveAsync(Command.RemarkId, Command.UserId, Description,Location, File, true), Times.Once);
        };

        It should_fetch_resolved_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Exactly(2));
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

        Because of = () => CommandHandler.HandleAsync(Command).Await();

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
            RemarkStateServiceMock.Verify(x => x.ResolveAsync(Command.RemarkId, Command.UserId, Description, Location, File, false), Times.Never);
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

        Because of = () => CommandHandler.HandleAsync(Command).Await();

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
            RemarkStateServiceMock.Verify(x => x.ResolveAsync(Command.RemarkId, Command.UserId, Description, Location, File, false), Times.Never);
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

        Because of = () => CommandHandler.HandleAsync(Command).Await();

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
            RemarkStateServiceMock.Verify(x => x.ResolveAsync(Command.RemarkId, Command.UserId, Description, Location, File, true), Times.Never);
        };

        It should_fetch_resolved_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Once);
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

        Because of = () => CommandHandler.HandleAsync(Command).Await();

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
            RemarkStateServiceMock.Verify(x => x.ResolveAsync(Command.RemarkId, Command.UserId, Description, Location, File, true), Times.Never);
        };

        It should_fetch_resolved_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Once);
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
            RemarkStateServiceMock.Setup(x => x.ResolveAsync(Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<string>(),
                Moq.It.IsAny<Location>(),
                Moq.It.IsAny<File>(),
                Moq.It.IsAny<bool>())).Throws(new ServiceException(ErrorCode));
        };

        Because of = () => CommandHandler.HandleAsync(Command).Await();

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
            RemarkStateServiceMock.Verify(x => x.ResolveAsync(Command.RemarkId, Command.UserId, Description, Location, File, true), Times.Once);
        };

        It should_fetch_resolved_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Once);
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