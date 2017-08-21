using System;
using It = Machine.Specifications.It;
using Moq;
using Collectively.Services.Remarks.Handlers;
using Machine.Specifications;
using Collectively.Messages.Commands.Remarks;
using Collectively.Messages.Events.Remarks;
using System.Threading;
using RawRabbit.Pipe;
using RawRabbit;

namespace Collectively.Services.Remarks.Tests.Specs.Handlers
{
    public class RenewRemarkHandler_specs : ChangeRemarkStateBase_specs<RenewRemark, RenewRemarkHandler>
    {
        protected static void Initialize()
        {
            InitializeBase();
            CommandHandler = new RenewRemarkHandler(Handler,
                BusClientMock.Object, 
                RemarkServiceMock.Object,
                RemarkStateServiceMock.Object,
                FileResolverMock.Object,
                FileValidatorMock.Object,
                ResourceFactoryMock.Object);
            Remark.SetRenewedState(User, Description);
        }
    }

    [Subject("RenewRemarkHandler HandleAsync")]
    public class when_invoking_renew_remark_handle_async : RenewRemarkHandler_specs
    {
        Establish context = () => Initialize();

        Because of = () => CommandHandler.HandleAsync(Command).Await();

        It should_renew_remark = () =>
        {
            RemarkStateServiceMock.Verify(x => x.RenewAsync(Command.RemarkId, Command.UserId, Description, Location), Times.Once);
        };

        It should_fetch_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Once);
        };

        It should_publish_remark_renewed_event = () =>
        {
            VerifyPublishAsync(Moq.It.IsAny<RemarkRenewed>(), Times.Once);
        };
    }

    [Subject("RenewRemarkHandler HandleAsync")]
    public class when_invoking_renew_remark_handle_async_and_coordinates_are_invalid : RenewRemarkHandler_specs
    {
        Establish context = () =>
        {
            Initialize();
            Command.Latitude = 100;
            Command.Longitude = 200;
        };

        Because of = () => CommandHandler.HandleAsync(Command).Await();

        It should_not_renew_remark = () =>
        {
            RemarkStateServiceMock.Verify(x => x.RenewAsync(Command.RemarkId, Command.UserId, Description, Location), Times.Never);
        };

        It should_not_fetch_remark = () =>
        {
            RemarkServiceMock.Verify(x => x.GetAsync(Command.RemarkId), Times.Never);
        };

        It should_not_publish_remark_renewed_event = () =>
        {
            VerifyPublishAsync(Moq.It.IsAny<RemarkRenewed>(), Times.Never);
        };

        It should_publish_renew_remark_rejected_message = () =>
        {
            VerifyPublishAsync(Moq.It.Is<RenewRemarkRejected>(m =>
                    m.RequestId == Command.Request.Id
                    && m.RemarkId == Command.RemarkId
                    && m.UserId == Command.UserId
                    && m.Code == OperationCodes.Error), Times.Once);
        };
    }
}