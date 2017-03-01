using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Handlers;
using Collectively.Services.Remarks.Services;
using Machine.Specifications;
using Moq;
using RawRabbit;
using System;
using Collectively.Common.Commands;
using Collectively.Common.Services;
using Collectively.Services.Remarks.Settings;

using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Commands.Remarks.Models;
using Collectively.Messages.Events.Remarks;
using It = Machine.Specifications.It;
using RawRabbit.Configuration.Publish;
using System.Collections.Generic;
using Collectively.Common.Types;
using Collectively.Common.Domain;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public abstract class RemovePhotosFromRemarkHandler_specs
    {
        protected static RemovePhotosFromRemarkHandler RemovePhotosFromRemarkHandler;
        protected static IHandler Handler;
        protected static Mock<IBusClient> BusClientMock;
        protected static Mock<IRemarkService> RemarkServiceMock;
        protected static Mock<IExceptionHandler> ExceptionHandlerMock;
        protected static RemovePhotosFromRemark Command;
        protected static Exception Exception;

        protected static void Initialize()
        {
            ExceptionHandlerMock = new Mock<IExceptionHandler>();
            Handler = new Handler(ExceptionHandlerMock.Object);
            BusClientMock = new Mock<IBusClient>();
            RemarkServiceMock = new Mock<IRemarkService>();
            Command = new RemovePhotosFromRemark
            {
                RemarkId = Guid.NewGuid(),
                Request = new Request
                {
                    Name = "remove_photos_from_remark",
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.Now,
                    Origin = "test",
                    Resource = ""
                },
                UserId = "userId",
                Photos = new List<GroupedFile>()
            };
            RemovePhotosFromRemarkHandler = new RemovePhotosFromRemarkHandler(Handler, 
                BusClientMock.Object, RemarkServiceMock.Object);
        }       
    }

    [Subject("RemovePhotosFromRemarkHandler HandleAsync")]
    public class when_invoking_remove_photos_from_remark_handle_async_without_file : RemovePhotosFromRemarkHandler_specs
    {
        Establish context = () => 
        {
            Initialize();
            RemarkServiceMock.Setup(x => x.RemovePhotosAsync(Command.RemarkId, Moq.It.IsAny<string[]>()))
                .Throws(new ServiceException(OperationCodes.NoFiles));
        };

        Because of = () => RemovePhotosFromRemarkHandler.HandleAsync(Command).Await();

        It should_publish_remove_photos_from_remark_rejected_message = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.Is<RemovePhotosFromRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.NoFiles),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Once);
        };
    }

    [Subject("RemovePhotosFromRemarkHandler HandleAsync")]
    public class when_invoking_remove_photos_from_remark_handle_async_without_user_access : RemovePhotosFromRemarkHandler_specs
    {
        Establish context = () => 
        {
            Initialize();
            RemarkServiceMock.Setup(x => x.ValidateEditorAccessOrFailAsync(Command.RemarkId, Command.UserId))
                .Throws(new ServiceException(OperationCodes.UserNotAllowedToModifyRemark));
        };

        Because of = () => RemovePhotosFromRemarkHandler.HandleAsync(Command).Await();

        It should_publish_remove_photos_from_remark_rejected_message = () =>
        {
            BusClientMock.Verify(x => x.PublishAsync(Moq.It.Is<RemovePhotosFromRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.UserNotAllowedToModifyRemark),
                Moq.It.IsAny<Guid>(),
                Moq.It.IsAny<Action<IPublishConfigurationBuilder>>()), Times.Once);
        };
    }    
}