using System;
using It = Machine.Specifications.It;
using Moq;
using Collectively.Services.Remarks.Handlers;
using Machine.Specifications;
using RawRabbit;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using RawRabbit.Pipe;
using System.Threading;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public class CancelRemarkHandler_specs : ChangeRemarkStateBase_specs<CancelRemark, CancelRemarkHandler>
    {
        protected static void Initialize()
        {
            InitializeBase();
            CommandHandler = new CancelRemarkHandler(Handler,
                BusClientMock.Object, 
                RemarkServiceMock.Object,
                RemarkStateServiceMock.Object,
                FileResolverMock.Object,
                FileValidatorMock.Object,
                ResourceFactoryMock.Object);
            Remark.SetCanceledState(User, Description);
        }
    }

    [Subject("CancelRemarkHandler HandleAsync")]
    public class when_invoking_cancel_remark_handle_async : CancelRemarkHandler_specs
    {
        Establish context = () => Initialize();

        Because of = () => CommandHandler.HandleAsync(Command).Await();

        It should_cancel_remark = () =>
        {
            RemarkStateServiceMock.Verify(x => x.CancelAsync(Command.RemarkId, Command.UserId, Description, Location), Times.Once());
        };

        It should_fetch_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Once());
        };

        It should_publish_remark_canceled_event = () =>
        {
            VerifyPublishAsync(Moq.It.IsAny<RemarkCanceled>(), Times.Once());
        };
    }

    [Subject("CancelRemarkHandler HandleAsync")]
    public class when_invoking_cancel_remark_handle_async_and_coordinates_are_invalid : CancelRemarkHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
            Command.Latitude = 100;
            Command.Longitude = 200;
        };

        Because of = () => CommandHandler.HandleAsync(Command).Await();

        It should_not_cancel_remark = () =>
        {
            RemarkStateServiceMock.Verify(x => x.CancelAsync(Command.RemarkId, Command.UserId, Description, Location), Times.Never());
        };

        It should_not_fetch_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Never());
        };

        It should_not_publish_remark_canceled_event = () =>
        {
            VerifyPublishAsync(Moq.It.IsAny<RemarkCanceled>(), Times.Never());
        };

        It should_publish_cancel_remark_rejected_message = () =>
        {
            VerifyPublishAsync(Moq.It.Is<CancelRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.Error), Times.Once());
        };
    }
}