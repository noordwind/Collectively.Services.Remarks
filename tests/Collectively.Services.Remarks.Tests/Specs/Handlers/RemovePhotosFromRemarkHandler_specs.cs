using Collectively.Services.Remarks.Handlers;
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
using System.Collections.Generic;
using Collectively.Common.Domain;
using System.Threading;
using RawRabbit.Pipe;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public abstract class RemovePhotosFromRemarkHandler_specs : SpecsBase
    {
        protected static RemovePhotosFromRemarkHandler RemovePhotosFromRemarkHandler;
        protected static IHandler Handler;
        protected static Mock<IRemarkService> RemarkServiceMock;
        protected static Mock<IRemarkPhotoService> RemarkPhotoServiceMock;
        protected static Mock<IExceptionHandler> ExceptionHandlerMock;
        protected static Mock<IResourceFactory> ResourceFactoryMock;
        protected static RemovePhotosFromRemark Command;
        protected static Exception Exception;

        protected static void Initialize()
        {
            ExceptionHandlerMock = new Mock<IExceptionHandler>();
            Handler = new Handler(ExceptionHandlerMock.Object);
            RemarkServiceMock = new Mock<IRemarkService>();
            RemarkPhotoServiceMock = new Mock<IRemarkPhotoService>();
            ResourceFactoryMock = new Mock<IResourceFactory>();
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
                Photos = new List<Collectively.Messages.Commands.Models.GroupedFile>()
            };
            RemovePhotosFromRemarkHandler = new RemovePhotosFromRemarkHandler(Handler, 
                BusClientMock.Object, RemarkServiceMock.Object, 
                RemarkPhotoServiceMock.Object, ResourceFactoryMock.Object);
        }       
    }

    [Subject("RemovePhotosFromRemarkHandler HandleAsync")]
    public class when_invoking_remove_photos_from_remark_handle_async_without_file : RemovePhotosFromRemarkHandler_specs
    {
        Establish context = () => 
        {
            Initialize();
            RemarkPhotoServiceMock.Setup(x => x.RemovePhotosAsync(Command.RemarkId, Moq.It.IsAny<string[]>()))
                .Throws(new ServiceException(OperationCodes.NoFiles));
        };

        Because of = () => RemovePhotosFromRemarkHandler.HandleAsync(Command).Await();

        It should_publish_remove_photos_from_remark_rejected_message = () =>
        {
            VerifyPublishAsync(Moq.It.Is<RemovePhotosFromRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.NoFiles), Times.Once());
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
            VerifyPublishAsync(Moq.It.Is<RemovePhotosFromRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.UserNotAllowedToModifyRemark), Times.Once());
        };
    }    
}